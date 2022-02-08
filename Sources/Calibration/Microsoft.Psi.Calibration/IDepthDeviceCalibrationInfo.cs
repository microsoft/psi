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
        /// Gets the extrinsics associated with the color camera, which describes how to transform points in world coordinates to color camera coordinates (world => camera).
        /// </summary>
        CoordinateSystem ColorExtrinsics { get; }

        /// <summary>
        /// Gets the pose of the color camera in the world, which is obtained by inverting the extrinsics matrix (camera => world).
        /// </summary>
        CoordinateSystem ColorPose { get; }

        /// <summary>
        /// Gets the intrinsics associated with the color camera.
        /// </summary>
        ICameraIntrinsics ColorIntrinsics { get; }

        /// <summary>
        /// Gets the extrinsics associated with the depth camera, which describes how to transform points in world coordinates to depth camera coordinates (world => camera).
        /// </summary>
        CoordinateSystem DepthExtrinsics { get; }

        /// <summary>
        /// Gets the pose of the depth camera in the world, which is obtained by inverting the extrinsics matrix (camera => world).
        /// </summary>
        CoordinateSystem DepthPose { get; }

        /// <summary>
        /// Gets the intrinsics associated with the depth camera.
        /// </summary>
        ICameraIntrinsics DepthIntrinsics { get; }

        /// <summary>
        /// Converts a 3D point from depth camera coordinates into color image coordinates.
        /// </summary>
        /// <param name="point3D">The 3D point in depth camera coordinates.</param>
        /// <param name="nullIfOutsideFieldOfView">Optional flag indicating whether to return null if point is outside the field of view (default true).</param>
        /// <returns>The 2D point in color image space.</returns>
        Point2D? GetPixelPosition(Point3D point3D, bool nullIfOutsideFieldOfView = true);

        /// <summary>
        /// Converts a 3D point from depth camera coordinates into color image coordinates.
        /// </summary>
        /// <param name="point3D">The 3D point in depth camera coordinates.</param>
        /// <param name="pixelPosition">Output point containing the pixel position.</param>
        /// <param name="nullIfOutsideFieldOfView">Optional flag indicating whether to return null if point is outside the field of view (default true).</param>
        /// <returns>True if <paramref name="pixelPosition"/> is within field of view, otherwise false.</returns>
        bool TryGetPixelPosition(Point3D point3D, out Point2D pixelPosition, bool nullIfOutsideFieldOfView = true);
    }
}
