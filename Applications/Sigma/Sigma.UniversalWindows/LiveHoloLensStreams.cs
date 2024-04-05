// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.MediaCapture;
    using Microsoft.Psi.MixedReality.ResearchMode;
    using Microphone = Microsoft.Psi.MixedReality.MediaCapture.Microphone;

    /// <summary>
    /// Implements support for live HoloLens streams.
    /// </summary>
    public static class LiveHoloLensStreams
    {
        /// <summary>
        /// Creates a collection of live HoloLens streams.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the streams to.</param>
        /// <param name="depthCamera">On return, contains the depth camera.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="outputPreviewStream">A value indicating whether to output the preview stream.</param>
        /// <returns>The HoloLens streams.</returns>
        public static HoloLensStreams Create(Pipeline pipeline, out DepthCamera depthCamera, SigmaAppConfiguration configuration, bool outputPreviewStream)
        {
            IProducer<AudioBuffer> audio = new Microphone(pipeline, configuration.MicrophoneConfiguration);

            if (configuration.AudioResampleFormat != null)
            {
                audio = audio.Resample(configuration.AudioResampleFormat, DeliveryPolicy.Unlimited);
            }

            if (configuration.AudioReframeSizeMs > 0)
            {
                audio = audio.Reframe(TimeSpan.FromMilliseconds(configuration.AudioReframeSizeMs), DeliveryPolicy.Unlimited);
            }

            var videoCamera = new PhotoVideoCamera(
                pipeline,
                new PhotoVideoCameraConfiguration
                {
                    // Use the VideoStream for the mixed reality image
                    VideoStreamSettings = new ()
                    {
                        FrameRate = configuration.VideoFrameRate,
                        ImageWidth = configuration.VideoResolution.Width,
                        ImageHeight = configuration.VideoResolution.Height,
                        OutputImage = false,
                        OutputEncodedImage = false,
                        OutputCameraIntrinsics = true,
                        OutputPose = true,
                        OutputImageCameraView = false,
                        OutputEncodedImageCameraView = true,
                    },
                    VideoStreamUsesTriggeredOutputs = configuration.UseTriggeredVideoStream,

                    PreviewStreamSettings = outputPreviewStream ?
                        new ()
                        {
                            FrameRate = configuration.VideoFrameRate,
                            ImageWidth = configuration.VideoResolution.Width,
                            ImageHeight = configuration.VideoResolution.Height,
                            OutputImage = false,
                            OutputEncodedImage = false,
                            OutputCameraIntrinsics = false,
                            OutputPose = false,
                            OutputImageCameraView = false,
                            OutputEncodedImageCameraView = true,
                            MixedRealityCapture = new (),
                        }
                        :
                        null,
                    PreviewStreamUsesTriggeredOutputs = configuration.UseTriggeredPreviewStream,

                    TriggeredOutputFrequency = 5,
                });

            // Initialize the depth camera
            depthCamera = new DepthCamera(pipeline, new DepthCameraConfiguration() { DepthSensorType = HoloLens2ResearchMode.ResearchModeSensorType.DepthLongThrow });
            depthCamera.DepthImage
                .First(DeliveryPolicy.SynchronousOrThrottle)
                .Select(_ => 0, DeliveryPolicy.SynchronousOrThrottle, name: "GetClock")
                .PipeTo(videoCamera.TriggerInput, DeliveryPolicy.SynchronousOrThrottle);

            return new HoloLensStreams(
                audio,
                null,
                videoCamera.VideoEncodedImageCameraView,
                videoCamera.PreviewEncodedImageCameraView,
                depthCamera.DepthImageCameraView,
                null,
                Generators.Repeat(pipeline, MixedReality.WorldSpatialAnchorId, TimeSpan.FromSeconds(1)),
                pipeline.Diagnostics);
        }
    }
}
