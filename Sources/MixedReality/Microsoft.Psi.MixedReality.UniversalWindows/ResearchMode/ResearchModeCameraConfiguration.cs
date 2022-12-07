// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.ResearchMode
{
    using System;
    using HoloLens2ResearchMode;

    /// <summary>
    /// Base class for configurations for components derived from <see cref="ResearchModeCamera"/>.
    /// </summary>
    public abstract class ResearchModeCameraConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the camera intrinsics are emitted.
        /// </summary>
        public bool OutputCameraIntrinsics { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the camera pose stream is emitted.
        /// </summary>
        public bool OutputPose { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the original map of points for calibration are emitted.
        /// </summary>
        public bool OutputCalibrationPointsMap { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum interval between posting calibration map messages.
        /// </summary>
        public TimeSpan OutputCalibrationPointsMapMinInterval { get; set; } = TimeSpan.FromSeconds(20);

        /// <summary>
        /// Gets or sets the minumum interval between posting frames.
        /// </summary>
        /// <remarks>This value can be used to reduce the emitting framerate of the camera.
        /// The default value of <see cref="TimeSpan.Zero"/> results in the highest possible framerate.</remarks>
        public TimeSpan OutputMinInterval { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the sensor type.
        /// </summary>
        internal ResearchModeSensorType SensorType { get; set; }

        /// <summary>
        /// Gets a value indicating whether the configuration requires computing the calibration points map.
        /// </summary>
        /// <returns>True if the configuration requires computing the calibration points map.</returns>
        internal abstract bool RequiresCalibrationPointsMap();

        /// <summary>
        /// Gets a value indicating whether the configuration requires computing the camera intrinsics.
        /// </summary>
        /// <returns>True if the configuration requires computing the camera intrinsics.</returns>
        internal abstract bool RequiresCameraIntrinsics();

        /// <summary>
        /// Gets a value indicating whether the configuration requires computing the camera pose.
        /// </summary>
        /// <returns>True if the configuration requires computing the camera pose.</returns>
        internal abstract bool RequiresPose();
    }
}
