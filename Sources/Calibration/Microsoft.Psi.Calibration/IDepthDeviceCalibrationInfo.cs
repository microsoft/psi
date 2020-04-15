// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Defines the calibration information (intrinsics and extrinsics of color and depth cameras) for a depth device.
    /// </summary>
    public interface IDepthDeviceCalibrationInfo
    {
        /// <summary>
        /// Gets the extrinsics defining the color camera's position with respect to the depth camera.
        /// </summary>
        CoordinateSystem ColorExtrinsics { get; }

        /// <summary>
        /// Gets the intrinsics associated with the color camera.
        /// </summary>
        ICameraIntrinsics ColorIntrinsics { get; }

        /// <summary>
        /// Gets the extrinsics defining the depth camera's position in the world.
        /// </summary>
        CoordinateSystem DepthExtrinsics { get; }

        /// <summary>
        /// Gets the intrinsics associated with the depth camera.
        /// </summary>
        ICameraIntrinsics DepthIntrinsics { get; }

        /// <summary>
        /// Converts a 3D point from depth camera coordinates into color image coordinates.
        /// </summary>
        /// <param name="point3D">The 3D point in depth camera coordinates.</param>
        /// <returns>The 2D point in color image space.</returns>
        Point2D ToColorSpace(Point3D point3D);
    }
}
