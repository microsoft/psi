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
/// Depth camera source component.
/// </summary>
    public class DepthCamera : ResearchModeCamera
    {
        private const byte InvalidMask = 0x80;
        private const ushort InvalidAhatValue = 4090;

        private readonly DepthCameraConfiguration configuration;
        private readonly bool isLongThrow;

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthCamera"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        public DepthCamera(Pipeline pipeline, DepthCameraConfiguration configuration = null)
            : base(
                  pipeline,
                  (configuration ?? new DepthCameraConfiguration()).Mode,
                  (configuration ?? new DepthCameraConfiguration()).OutputCalibrationMap,
                  (configuration ?? new DepthCameraConfiguration()).OutputCalibration)
        {
            this.configuration = configuration ?? new DepthCameraConfiguration();

            if (this.configuration.Mode != ResearchModeSensorType.DepthLongThrow &&
                this.configuration.Mode != ResearchModeSensorType.DepthAhat)
            {
                throw new ArgumentException($"Initializing the depth camera in {this.configuration.Mode} mode is not supported.");
            }

            this.isLongThrow = this.configuration.Mode == ResearchModeSensorType.DepthLongThrow;

            this.DepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, nameof(this.DepthImage));
            this.InfraredImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.InfraredImage));
        }

        /// <summary>
        /// Gets the depth image stream.
        /// </summary>
        public Emitter<Shared<DepthImage>> DepthImage { get; }

        /// <summary>
        /// Gets the infrared image stream.
        /// </summary>
        public Emitter<Shared<Image>> InfraredImage { get; }

        /// <inheritdoc/>
        protected override void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame, ResearchModeSensorResolution resolution, ulong frameTicks, DateTime originatingTime)
        {
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

            var depthFrame = sensorFrame as ResearchModeSensorDepthFrame;
            int depthImageWidth = (int)resolution.Width;
            int depthImageHeight = (int)resolution.Height;

            // Process and post the depth image if requested
            if (this.configuration.OutputDepth)
            {
                byte[] sigmaBuffer = null;
                var depthBuffer = depthFrame.GetBuffer();

                if (this.isLongThrow)
                {
                    sigmaBuffer = depthFrame.GetSigmaBuffer(); // Long-throw only
                    Debug.Assert(depthBuffer.Length == sigmaBuffer.Length, "Depth and sigma buffers should be of equal size!");
                }

                using var depthImage = DepthImagePool.GetOrCreate(depthImageWidth, depthImageHeight);
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

                this.DepthImage.Post(depthImage, originatingTime);
            }

            // Process and post the infrared image if requested
            if (this.configuration.OutputInfrared)
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

                this.InfraredImage.Post(infraredImage, originatingTime);
            }
        }
    }
}
