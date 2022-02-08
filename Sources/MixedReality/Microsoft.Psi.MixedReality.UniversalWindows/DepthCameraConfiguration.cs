// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using HoloLens2ResearchMode;

    /// <summary>
    /// Configuration for the <see cref="DepthCamera"/> component.
    /// </summary>
    public class DepthCameraConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the calibration settings are emitted.
        /// </summary>
        public bool OutputCalibration { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the original map of points for calibration are emitted.
        /// </summary>
        public bool OutputCalibrationMap { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum interval between posting calibration map messages.
        /// </summary>
        public TimeSpan OutputCalibrationMapInterval { get; set; } = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Gets or sets a value indicating whether the camera pose stream is emitted.
        /// </summary>
        public bool OutputPose { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the depth stream is emitted.
        /// </summary>
        public bool OutputDepth { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted.
        /// </summary>
        public bool OutputInfrared { get; set; } = true;

        /// <summary>
        /// Gets or sets the sensor mode.
        /// </summary>
        /// <remarks>Valid values are: DepthLongThrow or DepthAhat.</remarks>
        public ResearchModeSensorType Mode { get; set; } = ResearchModeSensorType.DepthLongThrow;
    }
}
