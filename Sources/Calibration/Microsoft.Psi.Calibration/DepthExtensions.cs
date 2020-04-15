// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Define set of extensions for dealing with depth maps.
    /// </summary>
    public static class DepthExtensions
    {
        /// <summary>
        /// Method for projecting a point in pixel coordinate from the color camera into the depth camera's coordinates by determining the corresponding depth pixel.
        /// </summary>
        /// <param name="depthDeviceCalibrationInfo">Defines the calibration information (extrinsics and intrinsics) for the depth device.</param>
        /// <param name="point2D">Pixel coordinates in the color camera.</param>
        /// <param name="depthImage">Depth map.</param>
        /// <returns>Point in camera coordinates.</returns>
        internal static Point3D? ProjectToCameraSpace(IDepthDeviceCalibrationInfo depthDeviceCalibrationInfo, Point2D point2D, Shared<Image> depthImage)
        {
            var colorExtrinsicsInverse = depthDeviceCalibrationInfo.ColorExtrinsics.Inverse();
            var pointInCameraSpace = depthDeviceCalibrationInfo.ColorIntrinsics.ToCameraSpace(point2D, 1.0, true);
            double x = pointInCameraSpace.X * colorExtrinsicsInverse[0, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[0, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[0, 2] + colorExtrinsicsInverse[0, 3];
            double y = pointInCameraSpace.X * colorExtrinsicsInverse[1, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[1, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[1, 2] + colorExtrinsicsInverse[1, 3];
            double z = pointInCameraSpace.X * colorExtrinsicsInverse[2, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[2, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[2, 2] + colorExtrinsicsInverse[2, 3];
            Point3D pointInWorldSpace = new Point3D(x, y, z);
            Point3D cameraOriginInWorldSpace = new Point3D(colorExtrinsicsInverse[0, 3], colorExtrinsicsInverse[1, 3], colorExtrinsicsInverse[2, 3]);
            Line3D rgbLine = new Line3D(cameraOriginInWorldSpace, pointInWorldSpace);
            return IntersectLineWithDepthMesh(depthDeviceCalibrationInfo, rgbLine, depthImage.Resource, 0.1);
        }

        /// <summary>
        /// Performs a ray/mesh intersection with the depth map.
        /// </summary>
        /// <param name="calibration">Defines the calibration (extrinsics and intrinsics) for the depth camera.</param>
        /// <param name="line">Ray to intersect against depth map.</param>
        /// <param name="depthImage">Depth map to ray cast against.</param>
        /// <param name="skipFactor">Distance to march on each step along ray.</param>
        /// <param name="undistort">Whether undistortion should be applied to the point.</param>
        /// <returns>Returns point of intersection.</returns>
        internal static Point3D? IntersectLineWithDepthMesh(IDepthDeviceCalibrationInfo calibration, Line3D line, Image depthImage, double skipFactor, bool undistort = true)
        {
            // max distance to check for intersection with the scene
            double totalDistance = 5;
            var delta = skipFactor * (line.EndPoint - line.StartPoint).Normalize();

            // size of increment along the ray
            int maxSteps = (int)(totalDistance / delta.Length);
            var hypothesisPoint = line.StartPoint;
            for (int i = 0; i < maxSteps; i++)
            {
                hypothesisPoint += delta;

                // get the mesh distance at the extended point
                float meshDistance = DepthExtensions.GetMeshDepthAtPoint(calibration, depthImage, hypothesisPoint, undistort);

                // if the mesh distance is less than the distance to the point we've hit the mesh
                if (!float.IsNaN(meshDistance) && (meshDistance < hypothesisPoint.Z))
                {
                    return hypothesisPoint;
                }
            }

            return null;
        }

        private static float GetMeshDepthAtPoint(IDepthDeviceCalibrationInfo calibration, Image depthImage, Point3D point, bool undistort)
        {
            Point2D depthSpacePoint = calibration.DepthIntrinsics.ToPixelSpace(point, undistort);

            int x = (int)Math.Round(depthSpacePoint.X);
            int y = (int)Math.Round(depthSpacePoint.Y);
            if ((x < 0) || (x >= depthImage.Width) || (y < 0) || (y >= depthImage.Height))
            {
                return float.NaN;
            }

            int byteOffset = (int)((y * depthImage.Stride) + (x * 2));
            var depth = BitConverter.ToUInt16(depthImage.ReadBytes(2, byteOffset), 0);
            if (depth == 0)
            {
                return float.NaN;
            }

            return (float)depth / 1000;
        }
    }
}
