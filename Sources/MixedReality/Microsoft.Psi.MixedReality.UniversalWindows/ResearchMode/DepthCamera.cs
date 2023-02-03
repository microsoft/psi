// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.ResearchMode
{
    using System;
    using System.Diagnostics;
    using HoloLens2ResearchMode;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Depth camera source component.
    /// </summary>
    public class DepthCamera : ResearchModeCamera
    {
        private const byte InvalidMask = 0x80;
        private const ushort InvalidAhatValue = 4090;

        private readonly bool isLongThrow;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthCamera"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="name">An optional name for the component.</param>
        public DepthCamera(Pipeline pipeline, DepthCameraConfiguration configuration = null, string name = nameof(DepthCamera))
            : base(pipeline, configuration ?? new DepthCameraConfiguration(), name)
        {
            this.isLongThrow = this.Configuration.SensorType == ResearchModeSensorType.DepthLongThrow;
            this.DepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, nameof(this.DepthImage));
            this.DepthImageCameraView = pipeline.CreateEmitter<DepthImageCameraView>(this, nameof(this.DepthImageCameraView));
            this.InfraredImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.InfraredImage));
            this.InfraredImageCameraView = pipeline.CreateEmitter<ImageCameraView>(this, nameof(this.InfraredImageCameraView));
        }

        /// <summary>
        /// Gets the depth image stream.
        /// </summary>
        public Emitter<Shared<DepthImage>> DepthImage { get; }

        /// <summary>
        /// Gets the depth image camera view stream.
        /// </summary>
        public Emitter<DepthImageCameraView> DepthImageCameraView { get; }

        /// <summary>
        /// Gets the infrared image stream.
        /// </summary>
        public Emitter<Shared<Image>> InfraredImage { get; }

        /// <summary>
        /// Gets the infrared image camera view stream.
        /// </summary>
        public Emitter<ImageCameraView> InfraredImageCameraView { get; }

        /// <summary>
        /// Gets the depth camera configuration.
        /// </summary>
        protected new DepthCameraConfiguration Configuration => base.Configuration as DepthCameraConfiguration;

        /// <inheritdoc/>
        protected override void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame, ResearchModeSensorResolution resolution, ulong frameTicks, DateTime originatingTime)
        {
            var shouldOutputDepthImage = this.Configuration.OutputDepthImage &&
                (originatingTime - this.DepthImage.LastEnvelope.OriginatingTime) > this.Configuration.OutputMinInterval;

            var shouldOutputDepthImageCameraView = this.Configuration.OutputDepthImageCameraView &&
                (originatingTime - this.DepthImageCameraView.LastEnvelope.OriginatingTime) > this.Configuration.OutputMinInterval;

            var shouldOutputInfraredImage = this.Configuration.OutputInfraredImage &&
                (originatingTime - this.InfraredImage.LastEnvelope.OriginatingTime) > this.Configuration.OutputMinInterval;

            var shouldOutputInfraredImageCameraView = this.Configuration.OutputInfraredImageCameraView &&
                (originatingTime - this.InfraredImageCameraView.LastEnvelope.OriginatingTime) > this.Configuration.OutputMinInterval;

            if (shouldOutputDepthImage ||
                shouldOutputDepthImageCameraView ||
                shouldOutputInfraredImage ||
                shouldOutputInfraredImageCameraView)
            {
                var depthFrame = sensorFrame as ResearchModeSensorDepthFrame;
                int depthImageWidth = (int)resolution.Width;
                int depthImageHeight = (int)resolution.Height;

                // Process and post the depth image if need be
                if (shouldOutputDepthImage || shouldOutputDepthImageCameraView)
                {
                    byte[] sigmaBuffer = null;
                    var depthBuffer = depthFrame.GetBuffer();

                    if (this.isLongThrow)
                    {
                        sigmaBuffer = depthFrame.GetSigmaBuffer(); // Long-throw only
                        Debug.Assert(depthBuffer.Length == sigmaBuffer.Length, "Depth and sigma buffers should be of equal size!");
                    }

                    using var depthImage = DepthImagePool.GetOrCreate(
                        depthImageWidth,
                        depthImageHeight,
                        DepthValueSemantics.DistanceToPoint,
                        0.001);
                    Debug.Assert(depthImage.Resource.Size == depthBuffer.Length * sizeof(ushort), "DepthImage size does not match raw depth buffer size!");

                    unsafe
                    {
                        ushort* depthData = (ushort*)depthImage.Resource.ImageData.ToPointer();
                        for (int i = 0; i < depthBuffer.Length; ++i)
                        {
                            bool invalid = this.isLongThrow ?
                                ((sigmaBuffer[i] & InvalidMask) > 0) :
                                (depthBuffer[i] >= InvalidAhatValue);

                            *depthData++ = invalid ? (ushort)0 : depthBuffer[i];
                        }
                    }

                    if (shouldOutputDepthImage)
                    {
                        this.DepthImage.Post(depthImage, originatingTime);
                    }

                    if (shouldOutputDepthImageCameraView)
                    {
                        using var depthImageCameraView = new DepthImageCameraView(depthImage, this.GetCameraIntrinsics(), this.GetCameraPose());
                        this.DepthImageCameraView.Post(depthImageCameraView, originatingTime);
                    }
                }

                // Process and post the infrared image if need be
                if (shouldOutputInfraredImage || shouldOutputInfraredImageCameraView)
                {
                    var infraredBuffer = depthFrame.GetAbDepthBuffer();
                    using var infraredImage = ImagePool.GetOrCreate(depthImageWidth, depthImageHeight, PixelFormat.Gray_16bpp);
                    Debug.Assert(infraredImage.Resource.Size == infraredBuffer.Length * sizeof(ushort), "InfraredImage size does not match raw infrared buffer size!");

                    unsafe
                    {
                        fixed (ushort* p = infraredBuffer)
                        {
                            infraredImage.Resource.CopyFrom((IntPtr)p);
                        }
                    }

                    if (shouldOutputInfraredImage)
                    {
                        this.InfraredImage.Post(infraredImage, originatingTime);
                    }

                    if (shouldOutputInfraredImageCameraView)
                    {
                        using var infraredImageCameraView = new ImageCameraView(infraredImage, this.GetCameraIntrinsics(), this.GetCameraPose());
                        this.InfraredImageCameraView.Post(infraredImageCameraView, originatingTime);
                    }
                }
            }
        }
    }
}
