// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.OpenXR.Visualization
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Common.Interpolators;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Task that projects hands to camera views.
    /// </summary>
    [BatchProcessingTask(
        "Project OpenXR Hands to Cameras",
        Description = "Project hand joints based on all available streams of camera image views. Hands are first interpolated according to camera message times.",
        OutputPartitionName = ProjectHandsToCamerasTaskConfiguration.DefaultPartitionName,
        OutputStoreName = ProjectHandsToCamerasTaskConfiguration.DefaultStoreName)]
    public class ProjectHandsToCamerasTask : BatchProcessingTask<ProjectHandsToCamerasTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, ProjectHandsToCamerasTaskConfiguration configuration)
        {
            // First scan the importer for all hand streams and camera view streams
            var cameraViewStreams = new List<(string streamName, string streamType)>();
            var handStreams = new Dictionary<string, IProducer<Hand>>();
            foreach (var partition in sessionImporter.PartitionImporters.Values)
            {
                foreach (var streamMeta in partition.AvailableStreams)
                {
                    var streamType = TypeSpec.Simplify(streamMeta.TypeName);
                    if (streamType == nameof(ImageCameraView) ||
                        streamType == nameof(EncodedImageCameraView) ||
                        streamType == nameof(DepthImageCameraView) ||
                        streamType == nameof(EncodedDepthImageCameraView))
                    {
                        cameraViewStreams.Add((streamMeta.Name, streamType));
                    }
                    else if (streamType == nameof(Hand))
                    {
                        handStreams.Add(streamMeta.Name, partition.OpenStream<Hand>(streamMeta.Name));
                    }
                    else if (streamType == TypeSpec.Simplify(typeof((Hand, Hand)).FullName))
                    {
                        var bothHands = partition.OpenStream<(Hand Left, Hand Right)>(streamMeta.Name);
                        handStreams.Add($"{streamMeta.Name}.Left", bothHands.Select(tuple => tuple.Left));
                        handStreams.Add($"{streamMeta.Name}.Right", bothHands.Select(tuple => tuple.Right));
                    }
                }
            }

            // Now process each stream (projecting hands to camera views) and persist results
            foreach (var (cameraStreamName, cameraStreamType) in cameraViewStreams)
            {
                if (cameraStreamType == nameof(ImageCameraView))
                {
                    foreach (var handStream in handStreams)
                    {
                        ProjectHandToCamera<ImageCameraView, Shared<Image>>(handStream.Value, cameraStreamName, sessionImporter)
                            .Write($"{cameraStreamName}.{handStream.Key}", exporter);
                    }
                }
                else if (cameraStreamType == nameof(EncodedImageCameraView))
                {
                    foreach (var handStream in handStreams)
                    {
                        ProjectHandToCamera<EncodedImageCameraView, Shared<EncodedImage>>(handStream.Value, cameraStreamName, sessionImporter)
                            .Write($"{cameraStreamName}.{handStream.Key}", exporter);
                    }
                }
                else if (cameraStreamType == nameof(DepthImageCameraView))
                {
                    foreach (var handStream in handStreams)
                    {
                        ProjectHandToCamera<DepthImageCameraView, Shared<DepthImage>>(handStream.Value, cameraStreamName, sessionImporter)
                            .Write($"{cameraStreamName}.{handStream.Key}", exporter);
                    }
                }
                else if (cameraStreamType == nameof(EncodedDepthImageCameraView))
                {
                    foreach (var handStream in handStreams)
                    {
                        ProjectHandToCamera<EncodedDepthImageCameraView, Shared<EncodedDepthImage>>(handStream.Value, cameraStreamName, sessionImporter)
                            .Write($"{cameraStreamName}.{handStream.Key}", exporter);
                    }
                }
            }
        }

        private static IProducer<List<Point2D?>> ProjectHandToCamera<TCamera, T>(
            IProducer<Hand> handStream,
            string cameraStreamName,
            SessionImporter sessionImporter)
            where TCamera : CameraView<T>
        {
            IProducer<TCamera> cameraStream = sessionImporter.OpenStream<TCamera>(cameraStreamName);
            return cameraStream.Join(
                    handStream,
                    new AdjacentValuesInterpolator<Hand>(
                        OpenXR.Operators.InterpolateHands,
                        false,
                        name: nameof(OpenXR.Operators.InterpolateHands)))
                .Select(tuple => tuple.Item2.Joints.Where(j => j is not null).Select(j => tuple.Item1.GetPixelPosition(j.Origin, true)).ToList());
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectHandsToCamerasTaskConfiguration"/> class.
        /// </summary>
        public ProjectHandsToCamerasTaskConfiguration()
            : base()
        {
            this.OutputPartitionName = DefaultPartitionName;
            this.OutputStoreName = DefaultStoreName;
        }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
