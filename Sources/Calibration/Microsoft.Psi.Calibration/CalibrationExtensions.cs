// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using System.Collections.Generic;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Provides various helper and extension methods for dealing with calibration objects, camera intrinsics, rotations, etc.
    /// </summary>
    public static class CalibrationExtensions
    {
        /// <summary>
        /// Construct a new <see cref="ICameraIntrinsics"/> object
        /// computed from image width/height and focal length.
        /// </summary>
        /// <param name="imageWidth">The image width.</param>
        /// <param name="imageHeight">The image height.</param>
        /// <param name="focalLengthX">The focal length in the X dimension.</param>
        /// <param name="focalLengthY">The focal length in the Y dimension.</param>
        /// <returns>A newly computed <see cref="ICameraIntrinsics"/>.</returns>
        public static ICameraIntrinsics ComputeCameraIntrinsics(
            int imageWidth,
            int imageHeight,
            double focalLengthX,
            double focalLengthY)
        {
            var transform = Matrix<double>.Build.Dense(3, 3);
            transform[0, 0] = focalLengthX;
            transform[1, 1] = focalLengthY;
            transform[2, 2] = 1;
            transform[0, 2] = imageWidth / 2.0;
            transform[1, 2] = imageHeight / 2.0;
            return new CameraIntrinsics(imageWidth, imageHeight, transform);
        }

        /// <summary>
        /// Construct a new <see cref="ICameraIntrinsics"/> object
        /// computed from an image (width and height) and focal length.
        /// </summary>
        /// <param name="image">The image to construct the intrinsics for.</param>
        /// <param name="focalLengthX">The focal length in the X dimension.</param>
        /// <param name="focalLengthY">The focal length in the Y dimension.</param>
        /// <returns>A newly computed <see cref="ICameraIntrinsics"/>.</returns>
        public static ICameraIntrinsics ComputeCameraIntrinsics(this ImageBase image, double focalLengthX, double focalLengthY)
        {
            return ComputeCameraIntrinsics(image.Width, image.Height, focalLengthX, focalLengthY);
        }

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
                float meshDistance = GetMeshDepthAtPoint(depthIntrinsics, depthImage, hypothesisPoint, undistort);

                // if the mesh distance is less than the distance to the point we've hit the mesh
                if (!float.IsNaN(meshDistance) && (meshDistance < hypothesisPoint.X))
                {
                    return hypothesisPoint;
                }
            }

            return null;
        }

        /// <summary>
        /// Use the Rodrigues formula for transforming a given rotation from axis-angle representation to a 3x3 matrix.
        /// Where 'r' is a rotation vector:
        /// theta = norm(r)
        /// M = skew(r/theta)
        /// R = I + M * sin(theta) + M*M * (1-cos(theta)).
        /// </summary>
        /// <param name="vectorRotation">Rotation in axis-angle vector representation,
        /// where the angle is represented by the length (L2-norm) of the vector.</param>
        /// <returns>Rotation in a 3x3 matrix representation.</returns>
        public static Matrix<double> AxisAngleToMatrix(Vector<double> vectorRotation)
        {
            if (vectorRotation.Count != 3)
            {
                throw new InvalidOperationException("The input must be a valid 3-element vector representing an axis-angle rotation.");
            }

            double theta = vectorRotation.L2Norm();

            var matR = Matrix<double>.Build.DenseIdentity(3, 3);

            // if there is no rotation (theta == 0) return identity rotation
            if (theta == 0)
            {
                return matR;
            }

            // Create a skew-symmetric matrix from the normalized axis vector
            var rn = vectorRotation.Normalize(2);
            var matM = Matrix<double>.Build.Dense(3, 3);
            matM[0, 0] = 0;
            matM[0, 1] = -rn[2];
            matM[0, 2] = rn[1];
            matM[1, 0] = rn[2];
            matM[1, 1] = 0;
            matM[1, 2] = -rn[0];
            matM[2, 0] = -rn[1];
            matM[2, 1] = rn[0];
            matM[2, 2] = 0;

            // I + M * sin(theta) + M*M * (1 - cos(theta))
            var sinThetaM = matM * Math.Sin(theta);
            matR += sinThetaM;
            var matMM = matM * matM;
            var cosThetaMM = matMM * (1 - Math.Cos(theta));
            matR += cosThetaMM;

            return matR;
        }

        /// <summary>
        /// Convert a rotation matrix to axis-angle representation (a unit vector scaled by the angular distance to rotate).
        /// </summary>
        /// <param name="m">Input rotation matrix.</param>
        /// <returns>Same rotation in axis-angle representation (L2-Norm of the vector represents angular distance).</returns>
        public static Vector<double> MatrixToAxisAngle(Matrix<double> m)
        {
            if (m.RowCount != 3 || m.ColumnCount != 3)
            {
                throw new InvalidOperationException("The input must be a valid 3x3 rotation matrix in order to compute its axis-angle representation.");
            }

            double epsilon = 0.01;

            // theta = arccos((Trace(m) - 1) / 2)
            double angle = Math.Acos((m.Trace() - 1.0) / 2.0);

            // Create the axis vector.
            var v = Vector<double>.Build.Dense(3, 0);

            if (angle < epsilon)
            {
                // If the angular distance to rotate is 0, we just return a vector of all zeroes.
                return v;
            }

            // Otherwise, the axis of rotation is extracted from the matrix as follows.
            v[0] = m[2, 1] - m[1, 2];
            v[1] = m[0, 2] - m[2, 0];
            v[2] = m[1, 0] - m[0, 1];

            if (v.L2Norm() < epsilon)
            {
                // if the axis to rotate around has 0 length, we are in a singularity where the angle has to be 180 degrees.
                angle = Math.PI;

                // We can extract the axis of rotation, knowing that v*vT = (m + I) / 2;
                // First compute r = (m + I) / 2
                var r = Matrix<double>.Build.Dense(3, 3);
                m.CopyTo(r);
                r[0, 0] += 1;
                r[1, 1] += 1;
                r[2, 2] += 1;
                r /= 2.0;

                // r = v*vT =
                // | v_x*v_x,  v_x*v_y,  v_x*v_z |
                // | v_x*v_y,  v_y*v_y,  v_y*v_z |
                // | v_x*v_z,  v_y*v_z,  v_z*v_z |
                // Extract x, y, and z as the square roots of the diagonal elements.
                var x = Math.Sqrt(r[0, 0]);
                var y = Math.Sqrt(r[1, 1]);
                var z = Math.Sqrt(r[2, 2]);

                // Now we need to determine the signs of x, y, and z.
                double xsign;
                double ysign;
                double zsign;

                double xy = r[0, 1];
                double xz = r[0, 2];

                if (xy > 0)
                {
                    if (xz > 0)
                    {
                        xsign = 1;
                        ysign = 1;
                        zsign = 1;
                    }
                    else
                    {
                        xsign = 1;
                        ysign = 1;
                        zsign = -1;
                    }
                }
                else
                {
                    if (xz > 0)
                    {
                        xsign = 1;
                        ysign = -1;
                        zsign = 1;
                    }
                    else
                    {
                        xsign = 1;
                        ysign = -1;
                        zsign = -1;
                    }
                }

                v[0] = x * xsign;
                v[1] = y * ysign;
                v[2] = z * zsign;
            }

            return v.Normalize(2) * angle;
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
