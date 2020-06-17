// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using System;
    using Microsoft.Azure.Kinect.Sensor;

    /// <summary>
    /// Represents the Azure Kinect configuration.
    /// </summary>
    public class AzureKinectSensorConfiguration
    {
        /// <summary>
        /// Gets or sets the index of the device to open.
        /// </summary>
        public int DeviceIndex { get; set; } = 0; // K4A_DEVICE_DEFAULT = 0

        /// <summary>
        /// Gets the color image format, i.e. <see cref="ImageFormat.ColorBGRA32"/>.
        /// </summary>
        /// <remarks>This property does not have a setter because currently
        /// the <see cref="AzureKinectSensor"/> component only supports the
        /// <see cref="ImageFormat.ColorBGRA32"/>.</remarks>
        public ImageFormat ColorFormat { get; } = ImageFormat.ColorBGRA32;

        /// <summary>
        /// Gets or sets the resolution of the color camera.
        /// </summary>
        public ColorResolution ColorResolution { get; set; } = ColorResolution.R1080p;

        /// <summary>
        /// Gets or sets the depth camera mode.
        /// </summary>
        public DepthMode DepthMode { get; set; } = DepthMode.NFOV_Unbinned;

        /// <summary>
        /// Gets or sets the desired frame rate.
        /// </summary>
        public FPS CameraFPS { get; set; } = FPS.FPS30;

        /// <summary>
        /// Gets or sets a value indicating whether color and depth captures should be strictly synchronized.
        /// </summary>
        public bool SynchronizedImagesOnly { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the color stream is emitted.
        /// </summary>
        public bool OutputColor { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the depth stream is emitted.
        /// </summary>
        public bool OutputDepth { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted.
        /// </summary>
        public bool OutputInfrared { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use the Azure Kinect's IMU.
        /// </summary>
        public bool OutputImu { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the Azure Kinect outputs its calibration settings.
        /// </summary>
        public bool OutputCalibration { get; set; } = true;

        /// <summary>
        /// Gets or sets the body tracker configuration.
        /// </summary>
        public AzureKinectBodyTrackerConfiguration BodyTrackerConfiguration { get; set; } = null;

        /// <summary>
        /// Gets or sets the timeout used for device capture.
        /// </summary>
        public TimeSpan DeviceCaptureTimeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the frequency at which frame rate is reported on the FrameRate emitter.
        /// </summary>
        public TimeSpan FrameRateReportingFrequency { get; set; } = TimeSpan.FromSeconds(2);
    }
}
