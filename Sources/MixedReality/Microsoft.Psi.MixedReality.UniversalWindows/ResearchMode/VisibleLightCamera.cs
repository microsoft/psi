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
    /// Visible light camera source component.
    /// </summary>
    public class VisibleLightCamera : ResearchModeCamera
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisibleLightCamera"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="name">An optional name for the component.</param>
        public VisibleLightCamera(Pipeline pipeline, VisibleLightCameraConfiguration configuration = null, string name = nameof(VisibleLightCamera))
            : base(pipeline, configuration ?? new VisibleLightCameraConfiguration(), name)
        {
            this.Image = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Image));
            this.ImageCameraView = pipeline.CreateEmitter<ImageCameraView>(this, nameof(this.ImageCameraView));
        }

        /// <summary>
        /// Gets the grayscale image stream.
        /// </summary>
        public Emitter<Shared<Image>> Image { get; }

        /// <summary>
        /// Gets the grayscale image camera view.
        /// </summary>
        public Emitter<ImageCameraView> ImageCameraView { get; }

        /// <summary>
        /// Gets the visible light camera configuration.
        /// </summary>
        protected new VisibleLightCameraConfiguration Configuration => base.Configuration as VisibleLightCameraConfiguration;

        /// <inheritdoc/>
        protected override void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame, ResearchModeSensorResolution resolution, ulong frameTicks, DateTime originatingTime)
        {
            var shouldOutputImage = this.Configuration.OutputImage &&
                (originatingTime - this.Image.LastEnvelope.OriginatingTime) > this.Configuration.OutputMinInterval;

            var shouldOutputImageCameraView = this.Configuration.OutputImageCameraView &&
                (originatingTime - this.ImageCameraView.LastEnvelope.OriginatingTime) > this.Configuration.OutputMinInterval;

            if (shouldOutputImage || shouldOutputImageCameraView)
            {
                var vlcFrame = sensorFrame as ResearchModeSensorVlcFrame;
                var imageBuffer = vlcFrame.GetBuffer();
                int imageWidth = (int)resolution.Width;
                int imageHeight = (int)resolution.Height;

                using var image = ImagePool.GetOrCreate(imageWidth, imageHeight, PixelFormat.Gray_8bpp);
                Debug.Assert(image.Resource.Size == imageBuffer.Length * sizeof(byte), "Image size does not match raw image buffer size!");
                image.Resource.CopyFrom(imageBuffer);

                if (shouldOutputImage)
                {
                    this.Image.Post(image, originatingTime);
                }

                if (shouldOutputImageCameraView)
                {
                    using var imageCameraView = new ImageCameraView(image, this.GetCameraIntrinsics(), this.GetCameraPose());
                    this.ImageCameraView.Post(imageCameraView, originatingTime);
                }
            }
        }
    }
}
