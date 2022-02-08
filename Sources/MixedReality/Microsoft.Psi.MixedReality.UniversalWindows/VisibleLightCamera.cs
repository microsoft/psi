// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Diagnostics;
    using HoloLens2ResearchMode;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Windows.Perception;

    /// <summary>
    /// Visible light camera source component.
    /// </summary>
    public class VisibleLightCamera : ResearchModeCamera
    {
        private readonly VisibleLightCameraConfiguration configuration;
        private DateTime previousOriginatingTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisibleLightCamera"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        public VisibleLightCamera(Pipeline pipeline, VisibleLightCameraConfiguration configuration = null)
            : base(
                  pipeline,
                  (configuration ?? new VisibleLightCameraConfiguration()).Mode,
                  (configuration ?? new VisibleLightCameraConfiguration()).OutputCalibrationMap,
                  (configuration ?? new VisibleLightCameraConfiguration()).OutputCalibration)
        {
            this.configuration = configuration ?? new VisibleLightCameraConfiguration();

            if (this.configuration.Mode != ResearchModeSensorType.LeftFront &&
                this.configuration.Mode != ResearchModeSensorType.LeftLeft &&
                this.configuration.Mode != ResearchModeSensorType.RightFront &&
                this.configuration.Mode != ResearchModeSensorType.RightRight)
            {
                throw new ArgumentException($"Initializing the camera in {this.configuration.Mode} mode is not supported.");
            }

            this.Image = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Image));
        }

        /// <summary>
        /// Gets the grayscale image stream.
        /// </summary>
        public Emitter<Shared<Image>> Image { get; }

        /// <inheritdoc/>
        protected override void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame, ResearchModeSensorResolution resolution, ulong frameTicks, DateTime originatingTime)
        {
            // If we're withing the specified min frame interval, return
            if ((originatingTime - this.previousOriginatingTime) <= this.configuration.MinInterframeInterval)
            {
                return;
            }

            if (this.configuration.OutputCalibrationMap &&
                (originatingTime - this.CalibrationPointsMap.LastEnvelope.OriginatingTime) > this.configuration.OutputCalibrationMapInterval)
            {
                // Post the calibration map created at the start
                this.CalibrationPointsMap.Post(this.GetCalibrationPointsMap(), originatingTime);
            }

            if (this.configuration.OutputCalibration)
            {
                // Post the intrinsics computed at the start
                this.CameraIntrinsics.Post(this.GetCameraIntrinsics(), originatingTime);
            }

            if (this.configuration.OutputPose)
            {
                var timestamp = PerceptionTimestampHelper.FromSystemRelativeTargetTime(TimeSpan.FromTicks((long)frameTicks));
                var rigNodeLocation = this.RigNodeLocator.TryLocateAtTimestamp(timestamp, MixedReality.WorldSpatialCoordinateSystem);

                // The rig node may not always be locatable, so we need a null check
                if (rigNodeLocation != null)
                {
                    // Compute the camera pose from the rig node location
                    var cameraWorldPose = this.ToCameraPose(rigNodeLocation);
                    this.Pose.Post(cameraWorldPose, originatingTime);
                }
            }

            if (this.configuration.OutputImage)
            {
                var vlcFrame = sensorFrame as ResearchModeSensorVlcFrame;
                var imageBuffer = vlcFrame.GetBuffer();
                int imageWidth = (int)resolution.Width;
                int imageHeight = (int)resolution.Height;

                using var image = ImagePool.GetOrCreate(imageWidth, imageHeight, PixelFormat.Gray_8bpp);
                Debug.Assert(image.Resource.Size == imageBuffer.Length * sizeof(byte), "Image size does not match raw image buffer size!");
                image.Resource.CopyFrom(imageBuffer);
                this.Image.Post(image, originatingTime);
                this.previousOriginatingTime = originatingTime;
            }
        }
    }
}
