// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using HoloLens2ResearchMode;

    /// <summary>
    /// Configuration for the <see cref="VisibleLightCamera"/> component.
    /// </summary>
    public class VisibleLightCameraConfiguration
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
        /// Gets or sets a value indicating whether the grayscale image stream is emitted.
        /// </summary>
        public bool OutputImage { get; set; } = true;

        /// <summary>
        /// Gets or sets the sensor selection.
        /// </summary>
        /// <remarks>Valid values are: LeftFront, LeftLeft, RightFront, RightRight.</remarks>
        public ResearchModeSensorType Mode { get; set; } = ResearchModeSensorType.LeftFront;

        /// <summary>
        /// Gets or sets the minumum inter-frame interval.
        /// </summary>
        /// <remarks>This value can be user to reduce the emitting framerate of the visible light camera.</remarks>
        public TimeSpan MinInterframeInterval { get; set; } = TimeSpan.Zero;
    }
}
