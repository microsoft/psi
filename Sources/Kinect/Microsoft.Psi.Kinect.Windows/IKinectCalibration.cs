// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;

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
        ICameraIntrinsics ColorIntrinsics { get; set; }

        /// <summary>
        /// Gets or sets the extrinsics associated with the Kinect's depth image
        /// </summary>
        CoordinateSystem DepthExtrinsics { get; set; }

        /// <summary>
        /// Gets or sets the intrinsics associated with the Kinect's depth image
        /// </summary>
        ICameraIntrinsics DepthIntrinsics { get; set; }

        /// <summary>
        /// Converts a 3D point from Kinect depth coordinates into color image coordinates.
        /// </summary>
        /// <param name="point3D">The 3D point in Kinect depth coordinates.</param>
        /// <returns>The 2D point in image space.</returns>
        Point2D ToColorSpace(Point3D point3D);
    }
}
