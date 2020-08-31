// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
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
        /// <returns>Point in 3D depth camera coordinates, assuming MathNet basis (Forward=X, Left=Y, Up=Z).</returns>
        public static Point3D? ProjectToCameraSpace(IDepthDeviceCalibrationInfo depthDeviceCalibrationInfo, Point2D point2D, Shared<DepthImage> depthImage)
        {
            var colorExtrinsicsInverse = depthDeviceCalibrationInfo.ColorPose;
            var pointInCameraSpace = depthDeviceCalibrationInfo.ColorIntrinsics.ToCameraSpace(point2D, 1.0, true);
            double x = pointInCameraSpace.X * colorExtrinsicsInverse[0, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[0, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[0, 2] + colorExtrinsicsInverse[0, 3];
            double y = pointInCameraSpace.X * colorExtrinsicsInverse[1, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[1, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[1, 2] + colorExtrinsicsInverse[1, 3];
            double z = pointInCameraSpace.X * colorExtrinsicsInverse[2, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[2, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[2, 2] + colorExtrinsicsInverse[2, 3];
            var pointInDepthCameraSpace = new Point3D(x, y, z);
            var colorCameraOriginInDepthCameraSpace = new Point3D(colorExtrinsicsInverse[0, 3], colorExtrinsicsInverse[1, 3], colorExtrinsicsInverse[2, 3]);
            var searchLine = new Line3D(colorCameraOriginInDepthCameraSpace, pointInDepthCameraSpace);
            return IntersectLineWithDepthMesh(depthDeviceCalibrationInfo.DepthIntrinsics, searchLine, depthImage.Resource);
        }

        /// <summary>
        /// Projects set of 2D image points into 3D.
        /// </summary>
        /// <param name="source">Tuple of depth image, list of points to project, and calibration information.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Returns a producer that generates a list of corresponding 3D points in Kinect camera space.</returns>
        public static IProducer<List<Point3D>> ProjectTo3D(
            this IProducer<(Shared<DepthImage>, List<Point2D>, IDepthDeviceCalibrationInfo)> source, DeliveryPolicy<(Shared<DepthImage>, List<Point2D>, IDepthDeviceCalibrationInfo)> deliveryPolicy = null)
        {
            var projectTo3D = new ProjectTo3D(source.Out.Pipeline);
            source.PipeTo(projectTo3D, deliveryPolicy);
            return projectTo3D;
        }

        /// <summary>
        /// Performs a ray/mesh intersection with the depth map.
        /// </summary>
        /// <param name="depthIntrinsics">The intrinsics for the depth camera.</param>
        /// <param name="line">Ray to intersect against depth map.</param>
        /// <param name="depthImage">Depth map to ray cast against.</param>
        /// <param name="maxDistance">The maximum distance to search for.</param>
        /// <param name="skipFactor">Distance to march on each step along ray.</param>
        /// <param name="undistort">Whether undistortion should be applied to the point.</param>
        /// <returns>Returns point of intersection.</returns>
        public static Point3D? IntersectLineWithDepthMesh(ICameraIntrinsics depthIntrinsics, Line3D line, DepthImage depthImage, double maxDistance = 5, double skipFactor = 0.05, bool undistort = true)
        {
            // max distance to check for intersection with the scene
            var delta = skipFactor * (line.EndPoint - line.StartPoint).Normalize();

            // size of increment along the ray
            int maxSteps = (int)(maxDistance / delta.Length);
            var hypothesisPoint = line.StartPoint;
            for (int i = 0; i < maxSteps; i++)
            {
                hypothesisPoint += delta;

                // get the mesh distance at the extended point
                float meshDistance = DepthExtensions.GetMeshDepthAtPoint(depthIntrinsics, depthImage, hypothesisPoint, undistort);

                // if the mesh distance is less than the distance to the point we've hit the mesh
                if (!float.IsNaN(meshDistance) && (meshDistance < hypothesisPoint.X))
                {
                    return hypothesisPoint;
                }
            }

            return null;
        }

        private static float GetMeshDepthAtPoint(ICameraIntrinsics depthIntrinsics, DepthImage depthImage, Point3D point, bool undistort)
        {
            Point2D depthSpacePoint = depthIntrinsics.ToPixelSpace(point, undistort);

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
