// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Batch task that runs the object 2D detection based tracker.
    /// </summary>
    [BatchProcessingTask(
        "Sigma - Run the object 2D detection based tracker",
        Description = "This task generates a new partition with results from object 2D detection based tracking.")]
    public class Object2DDetectionBasedObjectTrackerTask : BatchProcessingTask<Object2DDetectionBasedObjectTrackerTaskConfiguration>
    {
        /// <summary>
        /// Computes the streams for the back-projected object 2D detection results.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the computation to.</param>
        /// <param name="object2DDetectionResults">The 2D object detection results.</param>
        /// <param name="videoEncodedImageCameraView">The video encoded camera view.</param>
        /// <param name="depthImageCameraView">The depth image camera view.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The stream collection with the resuling back-projected object 2D results.</returns>
        public static StreamCollection Compute(
            Pipeline pipeline,
            IProducer<Object2DDetectionResults> object2DDetectionResults,
            IProducer<EncodedImageCameraView> videoEncodedImageCameraView,
            IProducer<DepthImageCameraView> depthImageCameraView,
            Object2DDetectionBasedObjectTrackerTaskConfiguration configuration)
        {
            var streamCollection = new StreamCollection();

            object2DDetectionResults
                .Join(videoEncodedImageCameraView.Select(veicv => (veicv.CameraIntrinsics, veicv.CameraPose)))
                .Join(depthImageCameraView, TimeSpan.FromMilliseconds(200))
                .PipeTo(new Object2DDetectionBasedObjectTracker(
                    pipeline,
                    scoreThreshold: configuration.DetectionScoreThreshold,
                    mergeInstanceThreshold: configuration.MergeInstanceThreshold))
                .Write($"{nameof(Object2DDetectionBasedObjectTracker)}.Results", streamCollection);

            return streamCollection;
        }

        /// <summary>
        /// Computes the streams for the back-projected object 2D detection results.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the computation to.</param>
        /// <param name="object2DDetectionResults">The 2D object detection results.</param>
        /// <param name="videoImageCameraView">The video camera view.</param>
        /// <param name="depthImageCameraView">The depth image camera view.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The stream collection with the resuling back-projected object 2D results.</returns>
        public static StreamCollection Compute(
            Pipeline pipeline,
            IProducer<Object2DDetectionResults> object2DDetectionResults,
            IProducer<ImageCameraView> videoImageCameraView,
            IProducer<DepthImageCameraView> depthImageCameraView,
            Object2DDetectionBasedObjectTrackerTaskConfiguration configuration)
        {
            var streamCollection = new StreamCollection();

            object2DDetectionResults
                .Join(videoImageCameraView.Select(veicv => (veicv.CameraIntrinsics, veicv.CameraPose)))
                .Join(depthImageCameraView, TimeSpan.FromMilliseconds(200))
                .PipeTo(new Object2DDetectionBasedObjectTracker(pipeline, scoreThreshold: configuration.DetectionScoreThreshold))
                .Write($"{nameof(Object2DDetectionBasedObjectTracker)}.Results", streamCollection);

            return streamCollection;
        }

        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, Object2DDetectionBasedObjectTrackerTaskConfiguration configuration)
        {
            var object2DDetectionResults = sessionImporter.OpenStreamOrDefault<Object2DDetectionResults>(configuration.Object2DDetectionResultsStreamName);
            var videoEncodedImageCameraView = sessionImporter.OpenStreamOrDefault<EncodedImageCameraView>(configuration.EncodedImageViewStreamName);
            var depthImageCameraView = sessionImporter.OpenStreamOrDefault<DepthImageCameraView>(configuration.DepthImageViewStreamName);

            Compute(pipeline, object2DDetectionResults, videoEncodedImageCameraView, depthImageCameraView, configuration).Write(exporter);
        }
    }

    /// <summary>
    /// Represents the configuration for the <see cref="Object2DDetectionBasedObjectTrackerTask"/>.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class Object2DDetectionBasedObjectTrackerTaskConfiguration : BatchProcessingTaskConfiguration
#pragma warning restore SA1402 // File may only contain a single type
    {
        private string encodedImageViewStreamName = string.Empty;
        private string depthImageViewStreamName = string.Empty;
        private string object2DDetectionResultsStreamName = string.Empty;
        private float detectionScoreThreshold = 0.5f;
        private double mergeInstanceThreshold = 0.2;

        /// <summary>
        /// Initializes a new instance of the <see cref="Object2DDetectionBasedObjectTrackerTaskConfiguration"/> class.
        /// </summary>
        public Object2DDetectionBasedObjectTrackerTaskConfiguration()
        {
            this.OutputStoreName = nameof(Object2DDetectionBasedObjectTracker);
            this.OutputPartitionName = nameof(Object2DDetectionBasedObjectTracker);
            this.DeliveryPolicySpec = DeliveryPolicySpec.Throttle;
            this.ReplayAllRealTime = false;
        }

        /// <summary>
        /// Gets or sets the name of the encoded image view stream.
        /// </summary>
        [DataMember]
        [DisplayName("Encoded Image View Stream")]
        [Description("The name of the encoded image view stream.")]
        public string EncodedImageViewStreamName
        {
            get => this.encodedImageViewStreamName;
            set { this.Set(nameof(this.EncodedImageViewStreamName), ref this.encodedImageViewStreamName, value); }
        }

        /// <summary>
        /// Gets or sets the name of the depth image view stream.
        /// </summary>
        [DataMember]
        [DisplayName("Depth Image View Stream")]
        [Description("The name of the depth image view stream.")]
        public string DepthImageViewStreamName
        {
            get => this.depthImageViewStreamName;
            set { this.Set(nameof(this.DepthImageViewStreamName), ref this.depthImageViewStreamName, value); }
        }

        /// <summary>
        /// Gets or sets the name of the object 2D results stream.
        /// </summary>
        [DataMember]
        [DisplayName("Object 2D Detection Results Stream")]
        [Description("The name of the object 2D detection results stream.")]
        public string Object2DDetectionResultsStreamName
        {
            get => this.object2DDetectionResultsStreamName;
            set { this.Set(nameof(this.Object2DDetectionResultsStreamName), ref this.object2DDetectionResultsStreamName, value); }
        }

        /// <summary>
        /// Gets or sets the score threshold to use when considering detection results.
        /// </summary>
        [DataMember]
        [DisplayName("Detection Score Threshold")]
        [Description("The score threshold to use when considering detection results.")]
        public float DetectionScoreThreshold
        {
            get => this.detectionScoreThreshold;
            set { this.Set(nameof(this.DetectionScoreThreshold), ref this.detectionScoreThreshold, value); }
        }

        /// <summary>
        /// Gets or sets the distance threshold to use when merging instance results of the same class together.
        /// </summary>
        [DataMember]
        [DisplayName("Merge Instance Threshold")]
        [Description("The distance threshold to use when merging instance results of the same class together.")]
        public double MergeInstanceThreshold
        {
            get => this.mergeInstanceThreshold;
            set { this.Set(nameof(this.MergeInstanceThreshold), ref this.mergeInstanceThreshold, value); }
        }
    }
}