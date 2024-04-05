// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;
    using HoloLensSerializers = HoloLensCaptureInterop.Serialization;

    /// <summary>
    /// Represents a collection of hololens sensor streams.
    /// </summary>
    public class HoloLensStreams : IClientServerCommunicationStreams
    {
        private const int BasePort = 15000;

        /// <summary>
        /// Initializes a new instance of the <see cref="HoloLensStreams"/> class.
        /// </summary>
        /// <param name="audio">The audio stream.</param>
        /// <param name="videoImageCameraView">The video image camera view stream.</param>
        /// <param name="videoEncodedImageCameraView">The video encoded image camera view stream.</param>
        /// <param name="previewEncodedImageCameraView">The preview encoded image camera view stream.</param>
        /// <param name="depthImageCameraView">The depth image camera view stream.</param>
        /// <param name="sceneUnderstanding">The scene understanding stream.</param>
        /// <param name="worldSpatialAnchorId">The world spatial anchor identifier stream.</param>
        /// <param name="pipelineDiagnostics">The pipeline diagnostics stream.</param>
        public HoloLensStreams(
            IProducer<AudioBuffer> audio,
            IProducer<ImageCameraView> videoImageCameraView,
            IProducer<EncodedImageCameraView> videoEncodedImageCameraView,
            IProducer<EncodedImageCameraView> previewEncodedImageCameraView,
            IProducer<DepthImageCameraView> depthImageCameraView,
            IProducer<SceneObjectCollection> sceneUnderstanding,
            IProducer<string> worldSpatialAnchorId,
            IProducer<PipelineDiagnostics> pipelineDiagnostics)
        {
            this.Audio = audio;
            this.VideoImageCameraView = videoImageCameraView;
            this.VideoEncodedImageCameraView = videoEncodedImageCameraView;
            this.PreviewEncodedImageCameraView = previewEncodedImageCameraView;
            this.DepthImageCameraView = depthImageCameraView;
            this.SceneUnderstanding = sceneUnderstanding;
            this.WorldSpatialAnchorId = worldSpatialAnchorId;
            this.PipelineDiagnostics = pipelineDiagnostics;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HoloLensStreams"/> class.
        /// </summary>
        /// <param name="importer">The importer to create the streams from.</param>
        /// <param name="prefix">The prefix under which the streams appear.</param>
        public HoloLensStreams(Importer importer, string prefix)
        {
            if (Resources.ImageFromStreamDecoder == null)
            {
                throw new InvalidOperationException("Image decoder has not been specified.");
            }

            if (Resources.DepthImageFromStreamDecoder == null)
            {
                throw new InvalidOperationException("DepthImage decoder has not been specified.");
            }

            var videoEncodedImageCameraView = importer.OpenStreamOrDefault<EncodedImageCameraView>($"{prefix}.{nameof(this.VideoEncodedImageCameraView)}");
            var videoImageCameraView = videoEncodedImageCameraView?.Decode(Resources.ImageFromStreamDecoder, DeliveryPolicy.LatestMessage);

            this.Audio = importer.OpenStreamOrDefault<AudioBuffer>($"{prefix}.{nameof(this.Audio)}");
            this.VideoImageCameraView = videoImageCameraView;
            this.VideoEncodedImageCameraView = videoEncodedImageCameraView;
            this.PreviewEncodedImageCameraView = importer.OpenStreamOrDefault<EncodedImageCameraView>($"{prefix}.{nameof(this.PreviewEncodedImageCameraView)}");
            this.DepthImageCameraView = importer.OpenStreamOrDefault<DepthImageCameraView>($"{prefix}.{nameof(this.DepthImageCameraView)}");
            this.SceneUnderstanding = importer.OpenStreamOrDefault<SceneObjectCollection>($"{prefix}.{nameof(this.SceneUnderstanding)}");
            this.WorldSpatialAnchorId = importer.OpenStreamOrDefault<string>($"{prefix}.{nameof(this.WorldSpatialAnchorId)}");
            this.PipelineDiagnostics = importer.OpenStreamOrDefault<PipelineDiagnostics>($"{prefix}.{nameof(this.PipelineDiagnostics)}");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HoloLensStreams"/> class.
        /// </summary>
        /// <param name="sessionImporter">The session importer to create the streams from.</param>
        /// <param name="prefix">The prefix under which the streams appear.</param>
        public HoloLensStreams(SessionImporter sessionImporter, string prefix)
        {
            if (Resources.ImageFromStreamDecoder == null)
            {
                throw new InvalidOperationException("Image decoder has not been specified.");
            }

            if (Resources.DepthImageFromStreamDecoder == null)
            {
                throw new InvalidOperationException("DepthImage decoder has not been specified.");
            }

            var videoEncodedImageCameraView = sessionImporter.OpenStreamOrDefault<EncodedImageCameraView>($"{prefix}.{nameof(this.VideoEncodedImageCameraView)}");
            var videoImageCameraView = videoEncodedImageCameraView?.Decode(Resources.ImageFromStreamDecoder, DeliveryPolicy.LatestMessage);

            this.Audio = sessionImporter.OpenStreamOrDefault<AudioBuffer>($"{prefix}.{nameof(this.Audio)}");
            this.VideoImageCameraView = videoImageCameraView;
            this.VideoEncodedImageCameraView = videoEncodedImageCameraView;
            this.PreviewEncodedImageCameraView = sessionImporter.OpenStreamOrDefault<EncodedImageCameraView>($"{prefix}.{nameof(this.PreviewEncodedImageCameraView)}");
            this.DepthImageCameraView = sessionImporter.OpenStreamOrDefault<DepthImageCameraView>($"{prefix}.{nameof(this.DepthImageCameraView)}");
            this.SceneUnderstanding = sessionImporter.OpenStreamOrDefault<SceneObjectCollection>($"{prefix}.{nameof(this.SceneUnderstanding)}");
            this.WorldSpatialAnchorId = sessionImporter.OpenStreamOrDefault<string>($"{prefix}.{nameof(this.WorldSpatialAnchorId)}");
            this.PipelineDiagnostics = sessionImporter.OpenStreamOrDefault<PipelineDiagnostics>($"{prefix}.{nameof(this.PipelineDiagnostics)}");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HoloLensStreams"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to create the streams into.</param>
        /// <param name="rendezvousProcess">The rendezvous process to read the streams from.</param>
        /// <param name="prefix">An optional prefix for the streams.</param>
        public HoloLensStreams(Pipeline pipeline, Rendezvous.Process rendezvousProcess, string prefix = null)
        {
            prefix ??= nameof(HoloLensStreams);

            if (Resources.ImageFromStreamDecoder == null)
            {
                throw new InvalidOperationException("Image decoder has not been specified.");
            }

            foreach (var endpoint in rendezvousProcess.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint)
                {
                    foreach (var stream in tcpEndpoint.Streams)
                    {
                        if (stream.StreamName == $"{prefix}.{nameof(this.Audio)}")
                        {
                            this.Audio = tcpEndpoint.ToTcpSource<AudioBuffer>(pipeline, HoloLensSerializers.AudioBufferFormat(), name: nameof(this.Audio));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.VideoEncodedImageCameraView)}")
                        {
                            // Receive NV12 encoded camera view
                            var nv12Encoded = tcpEndpoint.ToTcpSource<EncodedImageCameraView>(pipeline, HoloLensSerializers.EncodedImageCameraViewFormat(), name: nameof(this.VideoEncodedImageCameraView));

                            // Construct the regular camera view by decoding
                            this.VideoImageCameraView = nv12Encoded.Decode(Resources.ImageFromStreamDecoder, DeliveryPolicy.LatestMessage);

                            // Construct the jpeg encoded image camera view
                            this.VideoEncodedImageCameraView = this.VideoImageCameraView.Encode(Resources.ImageToStreamEncoder, DeliveryPolicy.SynchronousOrThrottle);
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.PreviewEncodedImageCameraView)}")
                        {
                            // Receive NV12 preview camera view
                            this.PreviewEncodedImageCameraView = tcpEndpoint.ToTcpSource<EncodedImageCameraView>(pipeline, HoloLensSerializers.EncodedImageCameraViewFormat(), name: nameof(this.PreviewEncodedImageCameraView));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.DepthImageCameraView)}")
                        {
                            this.DepthImageCameraView = tcpEndpoint.ToTcpSource<DepthImageCameraView>(pipeline, HoloLensSerializers.DepthImageCameraViewFormat(), name: nameof(this.DepthImageCameraView));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.SceneUnderstanding)}")
                        {
                            this.SceneUnderstanding = tcpEndpoint.ToTcpSource<SceneObjectCollection>(pipeline, HoloLensSerializers.SceneObjectCollectionFormat(), name: nameof(this.SceneUnderstanding));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.WorldSpatialAnchorId)}")
                        {
                            this.WorldSpatialAnchorId = tcpEndpoint.ToTcpSource<string>(pipeline, InteropSerialization.StringFormat(), name: nameof(this.WorldSpatialAnchorId));
                        }
                        else if (stream.StreamName == $"{prefix}.{nameof(this.PipelineDiagnostics)}")
                        {
                            this.PipelineDiagnostics = tcpEndpoint.ToTcpSource<PipelineDiagnostics>(pipeline, InteropSerialization.PipelineDiagnosticsFormat(), name: nameof(this.PipelineDiagnostics));
                        }
                    }
                }
                else if (endpoint is not Rendezvous.RemoteClockExporterEndpoint)
                {
                    throw new Exception("Unexpected endpoint type.");
                }
            }
        }

        /// <summary>
        /// Gets the audio stream.
        /// </summary>
        public IProducer<AudioBuffer> Audio { get; private set; }

        /// <summary>
        /// Gets the video encoded image camera view stream.
        /// </summary>
        public IProducer<EncodedImageCameraView> VideoEncodedImageCameraView { get; private set; }

        /// <summary>
        /// Gets the video image camera view stream.
        /// </summary>
        public IProducer<ImageCameraView> VideoImageCameraView { get; private set; }

        /// <summary>
        /// Gets the preview encoded image camera view stream.
        /// </summary>
        public IProducer<EncodedImageCameraView> PreviewEncodedImageCameraView { get; private set; }

        /// <summary>
        /// Gets the depth image camera view stream.
        /// </summary>
        public IProducer<DepthImageCameraView> DepthImageCameraView { get; private set; }

        /// <summary>
        /// Gets the scene understanding stream.
        /// </summary>
        public IProducer<SceneObjectCollection> SceneUnderstanding { get; private set; }

        /// <summary>
        /// Gets the world spatial anchor identifier stream.
        /// </summary>
        public IProducer<string> WorldSpatialAnchorId { get; private set; }

        /// <summary>
        /// Gets the pipeline diagnostics stream.
        /// </summary>
        public IProducer<PipelineDiagnostics> PipelineDiagnostics { get; private set; }

        /// <summary>
        /// Bridges the hololens streams to a target pipeline.
        /// </summary>
        /// <param name="targetPipeline">The target pipeline to bridge the streams to.</param>
        /// <returns>The hololens streams in the target pipeline.</returns>
        public HoloLensStreams BridgeTo(Pipeline targetPipeline)
            => new (
                this.Audio?.BridgeTo(targetPipeline),
                this.VideoImageCameraView?.BridgeTo(targetPipeline),
                this.VideoEncodedImageCameraView?.BridgeTo(targetPipeline),
                this.PreviewEncodedImageCameraView?.BridgeTo(targetPipeline),
                this.DepthImageCameraView?.BridgeTo(targetPipeline),
                this.SceneUnderstanding?.BridgeTo(targetPipeline),
                this.WorldSpatialAnchorId?.BridgeTo(targetPipeline),
                this.PipelineDiagnostics?.BridgeTo(targetPipeline));

        /// <inheritdoc/>
        public void Write(string prefix, Exporter exporter)
        {
            if (Resources.ImageToStreamEncoder == null)
            {
                throw new InvalidOperationException("Image encoder has not been specified.");
            }

            if (Resources.DepthImageToStreamEncoder == null)
            {
                throw new InvalidOperationException("DepthImage encoder has not been specified.");
            }

            this.Audio?.Write($"{prefix}.{nameof(this.Audio)}", exporter);
            this.VideoEncodedImageCameraView?.Write($"{prefix}.{nameof(this.VideoEncodedImageCameraView)}", exporter, largeMessages: true);
            this.PreviewEncodedImageCameraView?.Write($"{prefix}.{nameof(this.PreviewEncodedImageCameraView)}", exporter, largeMessages: true);
            this.DepthImageCameraView?.Write($"{prefix}.{nameof(this.DepthImageCameraView)}", exporter, largeMessages: true);
            this.SceneUnderstanding?.Write($"{prefix}.{nameof(this.SceneUnderstanding)}", exporter, largeMessages: true);
            this.WorldSpatialAnchorId?.Write($"{prefix}.{nameof(this.WorldSpatialAnchorId)}", exporter);
            this.PipelineDiagnostics?.Write($"{prefix}.{nameof(this.PipelineDiagnostics)}", exporter);
        }

        /// <inheritdoc/>
        public void WriteToRendezvousProcess(Rendezvous.Process rendezvousProcess, string address, string prefix = null)
        {
            prefix ??= nameof(HoloLensStreams);

            if (Resources.ImageToStreamEncoder == null)
            {
                throw new InvalidOperationException("Image encoder has not been specified.");
            }

            var port = BasePort;
            this.Audio?.WriteToRendezvousProcess($"{prefix}.{nameof(this.Audio)}", rendezvousProcess, address, port++, HoloLensSerializers.AudioBufferFormat(), DeliveryPolicy.Unlimited);

            this.VideoEncodedImageCameraView?.WriteToRendezvousProcess($"{prefix}.{nameof(this.VideoEncodedImageCameraView)}", rendezvousProcess, address, port++, HoloLensSerializers.EncodedImageCameraViewFormat(), DeliveryPolicy.QueueSizeConstrained(2));
            this.PreviewEncodedImageCameraView?.WriteToRendezvousProcess($"{prefix}.{nameof(this.PreviewEncodedImageCameraView)}", rendezvousProcess, address, port++, HoloLensSerializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
            this.DepthImageCameraView?.WriteToRendezvousProcess($"{prefix}.{nameof(this.DepthImageCameraView)}", rendezvousProcess, address, port++, HoloLensSerializers.DepthImageCameraViewFormat(), DeliveryPolicy.QueueSizeConstrained(2));
            this.SceneUnderstanding?.WriteToRendezvousProcess($"{prefix}.{nameof(this.SceneUnderstanding)}", rendezvousProcess, address, port++, HoloLensSerializers.SceneObjectCollectionFormat(), DeliveryPolicy.LatestMessage);
            this.WorldSpatialAnchorId?.WriteToRendezvousProcess($"{prefix}.{nameof(this.WorldSpatialAnchorId)}", rendezvousProcess, address, port++, InteropSerialization.StringFormat(), DeliveryPolicy.LatestMessage);
            this.PipelineDiagnostics?.WriteToRendezvousProcess($"{prefix}.{nameof(this.PipelineDiagnostics)}", rendezvousProcess, address, port++, InteropSerialization.PipelineDiagnosticsFormat(), DeliveryPolicy.LatestMessage);
        }

        /// <summary>
        /// Decodes and re-encodes the preview stream.
        /// </summary>
        /// <remarks>This method is useful if the desired encoding for the stream is different than the one used to communicate between client and server.</remarks>
        public void ReEncodePreviewStream()
        {
            this.PreviewEncodedImageCameraView = this.PreviewEncodedImageCameraView
                .Decode(Resources.ImageFromStreamDecoder, DeliveryPolicy.LatestMessage)
                .Encode(Resources.PreviewImageToStreamEncoder, DeliveryPolicy.SynchronousOrThrottle);
        }
    }
}
