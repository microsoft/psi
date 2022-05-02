// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.Visualization
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Common.Interpolators;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;
    using static Microsoft.Psi.MixedReality.Operators;

    /// <summary>
    /// Task that projects hands to camera views.
    /// </summary>
    [BatchProcessingTask(
        "Project Hands to Cameras",
        Description = "Project hand joints based on all available streams of camera image views.",
        OutputPartitionName = ProjectHandsToCamerasTaskConfiguration.DefaultPartitionName,
        OutputStoreName = ProjectHandsToCamerasTaskConfiguration.DefaultStoreName)]
    public class ProjectHandsToCamerasTask : BatchProcessingTask<ProjectHandsToCamerasTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, ProjectHandsToCamerasTaskConfiguration configuration)
        {
            // Get the tracked left and right hand streams
            var hands = sessionImporter.OpenStream<(Hand Left, Hand Right)>(configuration.HandsStreamName);
            var leftHand = hands.Select(h => h.Left).Where(h => h is not null && h.IsTracked);
            var rightHand = hands.Select(h => h.Right).Where(h => h is not null && h.IsTracked);

            foreach (var partition in sessionImporter.PartitionImporters.Values)
            {
                foreach (var streamMeta in partition.AvailableStreams)
                {
#pragma warning disable SA1101 // Prefix local calls with this
                    void ProcessCameraStream<TCamera, T>()
                        where TCamera : CameraView<T>
                    {
                        var cameraStream = partition.OpenStream<TCamera>(streamMeta.Name);
                        var resultsLeft = ProjectHandToCamera<TCamera, T>(leftHand, cameraStream, configuration.InterpolateHands);
                        var resultsRight = ProjectHandToCamera<TCamera, T>(rightHand, cameraStream, configuration.InterpolateHands);
                        var interpolated = configuration.InterpolateHands ? "Interpolated." : string.Empty;
                        resultsLeft.Select(tuple => tuple.ProjectedHand).Write($"{streamMeta.Name}.{interpolated}Projected.{configuration.HandsStreamName}.Left", exporter);
                        resultsRight.Select(tuple => tuple.ProjectedHand).Write($"{streamMeta.Name}.{interpolated}Projected.{configuration.HandsStreamName}.Right", exporter);
                        if (configuration.InterpolateHands)
                        {
                            resultsLeft.Select(tuple => tuple.InterpolatedHand).Write($"{streamMeta.Name}.{interpolated}{configuration.HandsStreamName}.Left", exporter);
                            resultsRight.Select(tuple => tuple.InterpolatedHand).Write($"{streamMeta.Name}.{interpolated}{configuration.HandsStreamName}.Right", exporter);
                        }
                    }

                    var streamType = streamMeta.TypeName.Split(',')[0];
                    if (string.Equals(streamType, typeof(ImageCameraView).FullName))
                    {
                        ProcessCameraStream<ImageCameraView, Shared<Image>>();
                    }
                    else if (string.Equals(streamType, typeof(EncodedImageCameraView).FullName))
                    {
                        ProcessCameraStream<EncodedImageCameraView, Shared<EncodedImage>>();
                    }
                    else if (string.Equals(streamType, typeof(DepthImageCameraView).FullName))
                    {
                        ProcessCameraStream<DepthImageCameraView, Shared<DepthImage>>();
                    }
                    else if (string.Equals(streamType, typeof(EncodedDepthImageCameraView).FullName))
                    {
                        ProcessCameraStream<EncodedDepthImageCameraView, Shared<EncodedDepthImage>>();
                    }
#pragma warning restore SA1101 // Prefix local calls with this
                }
            }
        }

        private static IProducer<(List<Point2D?> ProjectedHand, Hand InterpolatedHand)> ProjectHandToCamera<TCamera, T>(
            IProducer<Hand> handStream,
            IProducer<TCamera> cameraStream,
            bool interpolateHand)
                where TCamera : CameraView<T>
        {
            static List<Point2D?> ProjectJoints(Hand hand, TCamera camera)
                => hand.Joints.Where(j => j is not null).Select(j => camera.GetPixelPosition(j.Origin, true)).ToList();

            return interpolateHand ?
                cameraStream.Join(handStream, new AdjacentValuesInterpolator<Hand>(InterpolateHands, false, name: nameof(InterpolateHands)))
                            .Select(tuple => (ProjectJoints(tuple.Item2, tuple.Item1), tuple.Item2)) :
                handStream.Join(cameraStream, RelativeTimeInterval.Infinite)
                          .Select(tuple => (ProjectJoints(tuple.Item1, tuple.Item2), tuple.Item1));
        }
    }

#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// Represents the configuration for the <see cref="ProjectHandsToCamerasTask"/>.
    /// </summary>
    public class ProjectHandsToCamerasTaskConfiguration : BatchProcessingTaskConfiguration
    {
        /// <summary>
        /// Gets the default output partition name.
        /// </summary>
        public const string DefaultPartitionName = "ProjectedHands";

        /// <summary>
        /// Gets the default output store name.
        /// </summary>
        public const string DefaultStoreName = "ProjectedHands";

        private string handsStreamName = "Hands";
        private bool interpolateHands = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectHandsToCamerasTaskConfiguration"/> class.
        /// </summary>
        public ProjectHandsToCamerasTaskConfiguration()
            : base()
        {
            this.OutputPartitionName = DefaultPartitionName;
            this.OutputStoreName = DefaultStoreName;
        }

        /// <summary>
        /// Gets or sets the name of the hands source stream.
        /// </summary>
        [DataMember]
        [DisplayName("Hands Stream Name")]
        [Description("The name of the hands source stream.")]
        public string HandsStreamName
        {
            get => this.handsStreamName;
            set { this.Set(nameof(this.HandsStreamName), ref this.handsStreamName, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to interpolate hands to the timestamps of camera messages.
        /// </summary>
        [DataMember]
        [DisplayName("Interpolate Hands")]
        [Description("Interpolate hands to the camera message times?")]
        public bool InterpolateHands
        {
            get => this.interpolateHands;
            set { this.Set(nameof(this.InterpolateHands), ref this.interpolateHands, value); }
        }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
