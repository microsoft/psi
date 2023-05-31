// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.ResearchMode
{
    using System;
    using HoloLens2ResearchMode;

    /// <summary>
    /// Configuration for the <see cref="VisibleLightCamera"/> component.
    /// </summary>
    public class VisibleLightCameraConfiguration : ResearchModeCameraConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisibleLightCameraConfiguration"/> class.
        /// </summary>
        public VisibleLightCameraConfiguration()
        {
            this.VisibleLightSensorType = ResearchModeSensorType.LeftFront;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the component emits grayscale images.
        /// </summary>
        public bool OutputImage { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the component emits grayscale image camera views.
        /// </summary>
        public bool OutputImageCameraView { get; set; } = true;

        /// <summary>
        /// Gets or sets the visible light sensor type.
        /// </summary>
        public ResearchModeSensorType VisibleLightSensorType
        {
            get => this.SensorType;
            set
            {
                if (value != ResearchModeSensorType.LeftLeft &&
                    value != ResearchModeSensorType.LeftFront &&
                    value != ResearchModeSensorType.RightFront &&
                    value != ResearchModeSensorType.RightRight)
                {
                    throw new ArgumentException($"{value} mode is not valid for {nameof(VisibleLightCameraConfiguration)}.{nameof(this.VisibleLightSensorType)}.");
                }

                this.SensorType = value;
            }
        }

        /// <inheritdoc/>
        internal override bool RequiresCalibrationPointsMap()
            => this.OutputCalibrationPointsMap || this.OutputCameraIntrinsics || this.OutputImageCameraView;

        /// <inheritdoc/>
        internal override bool RequiresCameraIntrinsics()
            => this.OutputCameraIntrinsics || this.OutputImageCameraView;

        /// <inheritdoc/>
        internal override bool RequiresPose()
            => this.OutputPose || this.OutputImageCameraView;
    }
}
