// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Represents the Azure Kinect body tracker configuration.
    /// </summary>
    public class AzureKinectBodyTrackerConfiguration
    {
        /// <summary>
        /// Gets or sets the temporal smoothing to use across frames for the body tracker.
        /// </summary>
        /// <remarks>
        /// Set between 0 (no smoothing) and 1 (full smoothing). Less smoothing will increase
        /// the responsiveness of the detected skeletons but will cause more positional and
        /// orientational jitters.
        /// </remarks>
        public float TemporalSmoothing { get; set; } = 0f;

        /// <summary>
        /// Gets or sets a value indicating whether to use CPU only mode to run the tracker.
        /// Defaults to false (GPU mode).
        /// </summary>
        /// <remarks>
        /// The CPU only mode doesn't require the machine to have a GPU to run the tracker,
        /// but it will be much slower than the GPU mode.</remarks>
        public bool CpuOnlyMode { get; set; } = false;

        /// <summary>
        /// Gets or sets the sensor orientation used by body tracking.
        /// </summary>
        public SensorOrientation SensorOrientation { get; set; } = SensorOrientation.Default;

        /// <summary>
        /// Gets or sets a value indicating whether to use the "lite" model for pose estimation.
        /// Defaults to false (standard model).
        /// </summary>
        /// <remarks>
        /// The lite model trades ~2x performance increase for ~5% accuracy decrease compared to the standard model.
        /// </remarks>
        public bool UseLiteModel { get; set; } = false;
    }
}
