// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.ResearchMode
{
    using System;
    using HoloLens2ResearchMode;

    /// <summary>
    /// Configuration for the <see cref="DepthCamera"/> component.
    /// </summary>
    public class DepthCameraConfiguration : ResearchModeCameraConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthCameraConfiguration"/> class.
        /// </summary>
        public DepthCameraConfiguration()
        {
            this.DepthSensorType = ResearchModeSensorType.DepthLongThrow;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the component emits depth images.
        /// </summary>
        public bool OutputDepthImage { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the component emits depth image camera views.
        /// </summary>
        public bool OutputDepthImageCameraView { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the component emits infrared images.
        /// </summary>
        public bool OutputInfraredImage { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the component emits infrared image camera views.
        /// </summary>
        public bool OutputInfraredImageCameraView { get; set; } = true;

        /// <summary>
        /// Gets or sets the depth sensor type.
        /// </summary>
        public ResearchModeSensorType DepthSensorType
        {
            get => this.SensorType;
            set
            {
                if (value != ResearchModeSensorType.DepthLongThrow && value != ResearchModeSensorType.DepthAhat)
                {
                    throw new ArgumentException($"{value} mode is not valid for {nameof(DepthCameraConfiguration)}.{nameof(this.DepthSensorType)}.");
                }

                this.SensorType = value;
            }
        }

        /// <inheritdoc/>
        internal override bool RequiresCalibrationPointsMap()
            => this.OutputCalibrationPointsMap || this.OutputCameraIntrinsics || this.OutputDepthImageCameraView || this.OutputInfraredImageCameraView;

        /// <inheritdoc/>
        internal override bool RequiresCameraIntrinsics()
            => this.OutputCameraIntrinsics || this.OutputDepthImageCameraView || this.OutputInfraredImageCameraView;

        /// <inheritdoc/>
        internal override bool RequiresPose()
            => this.OutputPose || this.OutputDepthImageCameraView || this.OutputInfraredImageCameraView;
    }
}
