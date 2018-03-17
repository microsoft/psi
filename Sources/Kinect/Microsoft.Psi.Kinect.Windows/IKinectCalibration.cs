// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// IKinectCalibration defines a calibration object that handles both extrinsics and intrinsics for a Kinect camera
    /// </summary>
    public interface IKinectCalibration
    {
        /// <summary>
        /// Gets or sets the extrinsics associated with the Kinect's color image
        /// </summary>
        CoordinateSystem ColorExtrinsics { get; set; }

        /// <summary>
        /// Gets or sets the intrinsics associated with the Kinect's color image
        /// </summary>
        Microsoft.Psi.Calibration.ICameraIntrinsics ColorIntrinsics { get; set; }

        /// <summary>
        /// Gets or sets the extrinsics associated with the Kinect's depth image
        /// </summary>
        CoordinateSystem DepthExtrinsics { get; set; }

        /// <summary>
        /// Gets or sets the intrinsics associated with the Kinect's depth image
        /// </summary>
        Microsoft.Psi.Calibration.ICameraIntrinsics DepthIntrinsics { get; set; }
    }
}
