// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.Onnx;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Composite component that implements 3D object tracking.
    /// </summary>
    public class ObjectTrackingPipeline : Subpipeline
    {
        private readonly Connector<ImageCameraView> videoImageCameraViewInputConnector;
        private readonly Connector<DepthImageCameraView> depthImageCameraViewInputConnector;
        private readonly Connector<UserState> userStateInputConnector;
        private readonly Connector<List<string>> objectClassesInputConnector;
        private readonly Connector<Object2DDetectionResults> object2DDetectionResultsOutputConnector;
        private readonly Connector<List<(string Class, string InstanceId, Point3D Location)>> trackedObjectsLocationsOutputConnector;
        private readonly Connector<Object3DTrackingResults> object3DTrackingResultsOutputConnector;

        private readonly DeticModelRunner deticModelRunner;
        private readonly Object2DDetectionBasedObjectTracker object2DDetectionBasedObjectTracker;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectTrackingPipeline"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="name">An optional name for the component.</param>
        public ObjectTrackingPipeline(Pipeline pipeline, ObjectTrackingPipelineConfiguration configuration, string name = nameof(ObjectTrackingPipeline))
            : base(pipeline, name)
        {
            this.videoImageCameraViewInputConnector = this.CreateInputConnectorFrom<ImageCameraView>(pipeline, nameof(this.VideoImageCameraView));
            this.depthImageCameraViewInputConnector = this.CreateInputConnectorFrom<DepthImageCameraView>(pipeline, nameof(this.DepthImageCameraView));
            this.userStateInputConnector = this.CreateInputConnectorFrom<UserState>(pipeline, nameof(this.UserState));
            this.objectClassesInputConnector = this.CreateInputConnectorFrom<List<string>>(pipeline, nameof(this.ObjectClasses));
            this.object2DDetectionResultsOutputConnector = this.CreateOutputConnectorTo<Object2DDetectionResults>(pipeline, nameof(this.Object2DDetectionResults));
            this.object3DTrackingResultsOutputConnector = this.CreateOutputConnectorTo<Object3DTrackingResults>(pipeline, nameof(this.Object3DTrackingResults));
            this.trackedObjectsLocationsOutputConnector = this.CreateOutputConnectorTo<List<(string Class, string InstanceId, Point3D Location)>>(pipeline, nameof(this.TrackedObjectsLocations));

            // Instantiate a detic model
            IProducer<Object2DDetectionResults> object2DDetectionResults;
            if (configuration.ObjectDetectorType == ObjectDetectorType.Detic)
            {
                this.deticModelRunner = new DeticModelRunner(this, 36000, 36001, Resources.ImageToStreamEncoder, name: "DeticModel");
                object2DDetectionResults = this.deticModelRunner.ToObject2DDetectionResults(DeliveryPolicy.SynchronousOrThrottle);
            }
            else
            {
                throw new NotSupportedException($"Object detector type {configuration.ObjectDetectorType} is not supported.");
            }

            object2DDetectionResults.PipeTo(this.object2DDetectionResultsOutputConnector);

            // Filter images according to head motion, and send to the model together with the classes
            this.videoImageCameraViewInputConnector
                .Join(this.userStateInputConnector, Reproducible.Nearest<UserState>())
                .Where((tuple, _) => this.HeadVelocityIsWithinBounds(tuple.Item2.HeadVelocity3D, 0.25, 0.25), DeliveryPolicy.SynchronousOrThrottle)
                .Select((tuple, _) => tuple.Item1.ViewedObject, DeliveryPolicy.SynchronousOrThrottle)
                .Fuse(this.objectClassesInputConnector, Available.Last<List<string>>())
                .PipeTo(this.deticModelRunner, DeliveryPolicy.LatestMessage);

            // Compute the back projected results and pipe them to the output connector
            this.object2DDetectionBasedObjectTracker = new Object2DDetectionBasedObjectTracker(this, scoreThreshold: 0.5f);
            this.object2DDetectionBasedObjectTracker.PipeTo(this.object3DTrackingResultsOutputConnector);
            object2DDetectionResults
                .Join(this.videoImageCameraViewInputConnector.Select(veicv => (veicv.CameraIntrinsics, veicv.CameraPose)))
                .Join(this.depthImageCameraViewInputConnector, TimeSpan.FromMilliseconds(200))
                .PipeTo(this.object2DDetectionBasedObjectTracker)
                .Select(r => r.Detections.Select(d => (d.Class, d.InstanceId, d.PointCloud.Centroid)).ToList(), DeliveryPolicy.SynchronousOrThrottle)
                .PipeTo(this.trackedObjectsLocationsOutputConnector);
        }

        /// <summary>
        /// Gets the receiver for video image camera views.
        /// </summary>
        public Receiver<ImageCameraView> VideoImageCameraView => this.videoImageCameraViewInputConnector.In;

        /// <summary>
        /// Gets the receiver for depth image camera views.
        /// </summary>
        public Receiver<DepthImageCameraView> DepthImageCameraView => this.depthImageCameraViewInputConnector.In;

        /// <summary>
        /// Gets the receiver for the user state.
        /// </summary>
        public Receiver<UserState> UserState => this.userStateInputConnector.In;

        /// <summary>
        /// Gets the receiver for the set of object classes.
        /// </summary>
        public Receiver<List<string>> ObjectClasses => this.objectClassesInputConnector.In;

        /// <summary>
        /// Gets the emitter for the object 2D detection results.
        /// </summary>
        public Emitter<Object2DDetectionResults> Object2DDetectionResults => this.object2DDetectionResultsOutputConnector.Out;

        /// <summary>
        /// Gets the emitter for the object 3D tracking results.
        /// </summary>
        public Emitter<Object3DTrackingResults> Object3DTrackingResults => this.object3DTrackingResultsOutputConnector.Out;

        /// <summary>
        /// Gets the emitter for the locations of 3D tracked objects.
        /// </summary>
        public Emitter<List<(string Class, string InstanceId, Point3D Location)>> TrackedObjectsLocations => this.trackedObjectsLocationsOutputConnector.Out;

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            this.deticModelRunner?.Dispose();
        }

        /// <summary>
        /// Writes the streams to an exporter under a specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to write the streams under.</param>
        /// <param name="exporter">The exporter to write the streams to.</param>
        public void Write(string prefix, Exporter exporter)
        {
            this.object2DDetectionResultsOutputConnector.Write($"{prefix}.{nameof(this.Object2DDetectionResults)}", exporter);
            this.object2DDetectionBasedObjectTracker.Write($"{prefix}.{nameof(this.Object3DTrackingResults)}", exporter);
            this.trackedObjectsLocationsOutputConnector.Write($"{prefix}.{nameof(this.TrackedObjectsLocations)}", exporter);
        }

        private bool HeadVelocityIsWithinBounds(CoordinateSystemVelocity3D headVelocity3D, double maxHeadAngularSpeed, double maxHeadLinearSpeed)
            => headVelocity3D.Angular.Magnitude.Radians <= maxHeadAngularSpeed && headVelocity3D.Linear.Magnitude <= maxHeadLinearSpeed;
    }
}
