// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using System.Collections.Generic;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using MathNet.Spatial.Units;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Provides various helper and extension methods for calibrating cameras, dealing with calibration objects, camera intrinsics, rotations, etc.
    /// </summary>
    public static class CalibrationExtensions
    {
        /// <summary>
        /// Estimate a camera's camera matrix and distortion coefficients, given a set of image points and
        /// corresponding 3d camera points. The underlying calibration procedure utilizes Levenberg Marquardt
        /// optimization to produce these estimates.
        /// </summary>
        /// <param name="cameraPoints">3d positions of the points in camera coordinates.
        /// These points are *not* yet in the typically assumed \psi basis (X=Forward, Y=Left, Z=Up).
        /// Instead, we assume that X and Y correspond to directions in the image plane, and Z corresponds to depth in the plane.</param>
        /// <param name="imagePoints">2d positions of the points in the image.</param>
        /// <param name="initialCameraMatrix">Initial estimate of the camera matrix.</param>
        /// <param name="initialDistortionCoefficients">Initial estimate of distortion coefficients.</param>
        /// <param name="cameraMatrix">Estimated output camera matrix.</param>
        /// <param name="distortionCoefficients">Estimated output distortion coefficients.</param>
        /// <param name="silent">If false, print debugging information to the console.</param>
        /// <returns>The RMS (root mean squared) error of this computation.</returns>
        public static double CalibrateCameraIntrinsics(
            List<Point3D> cameraPoints,
            List<Point2D> imagePoints,
            Matrix<double> initialCameraMatrix,
            Vector<double> initialDistortionCoefficients,
            out Matrix<double> cameraMatrix,
            out Vector<double> distortionCoefficients,
            bool silent = true)
        {
            // pack parameters into vector
            // parameters: fx, fy, cx, cy, k1, k2 = 6 parameters
            var initialParameters = Vector<double>.Build.Dense(6);
            int pi = 0;
            initialParameters[pi++] = initialCameraMatrix[0, 0]; // fx
            initialParameters[pi++] = initialCameraMatrix[1, 1]; // fy
            initialParameters[pi++] = initialCameraMatrix[0, 2]; // cx
            initialParameters[pi++] = initialCameraMatrix[1, 2]; // cy
            initialParameters[pi++] = initialDistortionCoefficients[0]; // k1
            initialParameters[pi++] = initialDistortionCoefficients[1]; // k2

            var error = CalibrateCamera(cameraPoints, imagePoints, initialParameters, false, out var computedParameters, silent);

            // unpack parameters into the outputs
            cameraMatrix = Matrix<double>.Build.Dense(3, 3);
            distortionCoefficients = Vector<double>.Build.Dense(2);

            pi = 0;
            cameraMatrix[0, 0] = computedParameters[pi++]; // fx
            cameraMatrix[1, 1] = computedParameters[pi++]; // fy
            cameraMatrix[2, 2] = 1;
            cameraMatrix[0, 2] = computedParameters[pi++]; // cx
            cameraMatrix[1, 2] = computedParameters[pi++]; // cy
            distortionCoefficients[0] = computedParameters[pi++]; // k1
            distortionCoefficients[1] = computedParameters[pi++]; // k2

            return error;
        }

        /// <summary>
        /// Estimate a camera's intrinsics (camera matrix + distortion coefficients) and extrinsics (rotation + translation)
        /// given a set of image points and corresponding 3d world points. The underlying calibration procedure utilizes
        /// Levenberg Marquardt optimization to produce these estimates.
        /// </summary>
        /// <param name="worldPoints">3d positions of the points in world coordinates.
        /// These points are *not* yet in the typically assumed \psi basis (X=Forward, Y=Left, Z=Up).
        /// Instead, we assume that X and Y correspond to directions in the image plane, and Z corresponds to depth in the plane.</param>
        /// <param name="imagePoints">2d positions of the points in the image.</param>
        /// <param name="initialCameraMatrix">Initial estimate of the camera matrix.</param>
        /// <param name="initialDistortionCoefficients">Initial estimate of distortion coefficients.</param>
        /// <param name="cameraMatrix">Estimated output camera matrix.</param>
        /// <param name="distortionCoefficients">Estimated output distortion coefficients.</param>
        /// <param name="rotation">Estimated camera rotation.</param>
        /// <param name="translation">Estimated camera translation.</param>
        /// <param name="silent">If false, print debugging information to the console.</param>
        /// <returns>The RMS (root mean squared) error of this computation.</returns>
        public static double CalibrateCameraIntrinsicsAndExtrinsics(
            List<Point3D> worldPoints,
            List<Point2D> imagePoints,
            Matrix<double> initialCameraMatrix,
            Vector<double> initialDistortionCoefficients,
            out Matrix<double> cameraMatrix,
            out Vector<double> distortionCoefficients,
            out Vector<double> rotation,
            out Vector<double> translation,
            bool silent = true)
        {
            // Compute an initial rotation and translation with DLT algorithm
            DLT(initialCameraMatrix, initialDistortionCoefficients, worldPoints, imagePoints, out var rotationMatrix, out var initialTranslation);
            var initialRotation = MatrixToAxisAngle(rotationMatrix);

            // pack parameters into vector
            // parameters: fx, fy, cx, cy, k1, k2, + 3 for rotation, 3 translation = 12
            var initialParameters = Vector<double>.Build.Dense(12);
            int pi = 0;
            initialParameters[pi++] = initialCameraMatrix[0, 0]; // fx
            initialParameters[pi++] = initialCameraMatrix[1, 1]; // fy
            initialParameters[pi++] = initialCameraMatrix[0, 2]; // cx
            initialParameters[pi++] = initialCameraMatrix[1, 2]; // cy
            initialParameters[pi++] = initialDistortionCoefficients[0]; // k1
            initialParameters[pi++] = initialDistortionCoefficients[1]; // k2
            initialParameters[pi++] = initialRotation[0];
            initialParameters[pi++] = initialRotation[1];
            initialParameters[pi++] = initialRotation[2];
            initialParameters[pi++] = initialTranslation[0];
            initialParameters[pi++] = initialTranslation[1];
            initialParameters[pi++] = initialTranslation[2];

            var error = CalibrateCamera(worldPoints, imagePoints, initialParameters, true, out var computedParameters, silent);

            // unpack parameters into the outputs
            cameraMatrix = Matrix<double>.Build.Dense(3, 3);
            distortionCoefficients = Vector<double>.Build.Dense(2);
            rotation = Vector<double>.Build.Dense(3);
            translation = Vector<double>.Build.Dense(3);

            pi = 0;
            cameraMatrix[0, 0] = computedParameters[pi++]; // fx
            cameraMatrix[1, 1] = computedParameters[pi++]; // fy
            cameraMatrix[2, 2] = 1;
            cameraMatrix[0, 2] = computedParameters[pi++]; // cx
            cameraMatrix[1, 2] = computedParameters[pi++]; // cy
            distortionCoefficients[0] = computedParameters[pi++]; // k1
            distortionCoefficients[1] = computedParameters[pi++]; // k2
            rotation[0] = computedParameters[pi++];
            rotation[1] = computedParameters[pi++];
            rotation[2] = computedParameters[pi++];
            translation[0] = computedParameters[pi++];
            translation[1] = computedParameters[pi++];
            translation[2] = computedParameters[pi++];

            return error;
        }

        /// <summary>
        /// Construct a new <see cref="ICameraIntrinsics"/> object
        /// computed from image width/height and focal length.
        /// </summary>
        /// <param name="imageWidth">The image width.</param>
        /// <param name="imageHeight">The image height.</param>
        /// <param name="focalLengthX">The focal length in the X dimension.</param>
        /// <param name="focalLengthY">The focal length in the Y dimension.</param>
        /// <returns>A newly computed <see cref="ICameraIntrinsics"/>.</returns>
        public static ICameraIntrinsics CreateCameraIntrinsics(
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
        public static ICameraIntrinsics CreateCameraIntrinsics(this ImageBase image, double focalLengthX, double focalLengthY)
        {
            return CreateCameraIntrinsics(image.Width, image.Height, focalLengthX, focalLengthY);
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
            var pointInCameraSpace = depthDeviceCalibrationInfo.ColorIntrinsics.GetCameraSpacePosition(point2D, 1.0, depthImage.Resource.DepthValueSemantics, true);
            double x = pointInCameraSpace.X * colorExtrinsicsInverse[0, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[0, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[0, 2] + colorExtrinsicsInverse[0, 3];
            double y = pointInCameraSpace.X * colorExtrinsicsInverse[1, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[1, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[1, 2] + colorExtrinsicsInverse[1, 3];
            double z = pointInCameraSpace.X * colorExtrinsicsInverse[2, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[2, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[2, 2] + colorExtrinsicsInverse[2, 3];
            var pointInDepthCameraSpace = new Point3D(x, y, z);
            var colorCameraOriginInDepthCameraSpace = new Point3D(colorExtrinsicsInverse[0, 3], colorExtrinsicsInverse[1, 3], colorExtrinsicsInverse[2, 3]);
            var searchRay = new Ray3D(colorCameraOriginInDepthCameraSpace, pointInDepthCameraSpace - colorCameraOriginInDepthCameraSpace);
            return depthImage.Resource.ComputeRayIntersection(depthDeviceCalibrationInfo.DepthIntrinsics, searchRay);
        }

        /// <summary>
        /// Projects set of 2D image points into 3D.
        /// </summary>
        /// <param name="source">Tuple of depth image, list of points to project, and calibration information.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Returns a producer that generates a list of corresponding 3D points in Kinect camera space.</returns>
        public static IProducer<List<Point3D>> ProjectTo3D(
            this IProducer<(Shared<DepthImage>, List<Point2D>, IDepthDeviceCalibrationInfo)> source,
            DeliveryPolicy<(Shared<DepthImage>, List<Point2D>, IDepthDeviceCalibrationInfo)> deliveryPolicy = null,
            string name = nameof(ProjectTo3D))
            => source.PipeTo(new ProjectTo3D(source.Out.Pipeline, name), deliveryPolicy);

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
        /// Convert a rotation matrix to axis-angle representation (a unit vector scaled by the angular distance in radians to rotate).
        /// </summary>
        /// <param name="m">Input rotation matrix.</param>
        /// <param name="epsilon">An optional angle epsilon parameter used to determine when the specified matrix contains a zero-rotation (by default 0.01 degrees).</param>
        /// <returns>Same rotation in axis-angle representation (L2-Norm of the vector represents angular distance in radians).</returns>
        public static Vector<double> MatrixToAxisAngle(Matrix<double> m, Angle? epsilon = null)
        {
            epsilon ??= Angle.FromDegrees(0.01);

            if (m.RowCount != 3 || m.ColumnCount != 3)
            {
                throw new InvalidOperationException("The input must be a valid 3x3 rotation matrix in order to compute its axis-angle representation.");
            }

            // theta = arccos((Trace(m) - 1) / 2)
            double angle = Math.Acos((m.Trace() - 1.0) / 2.0);

            // Create the axis vector.
            var v = Vector<double>.Build.Dense(3, 0);

            if (double.IsNaN(angle) || angle < epsilon.Value.Radians)
            {
                // If the angular distance to rotate is 0, we just return a vector of all zeroes.
                return v;
            }

            // Otherwise, the axis of rotation is extracted from the matrix as follows.
            v[0] = m[2, 1] - m[1, 2];
            v[1] = m[0, 2] - m[2, 0];
            v[2] = m[1, 0] - m[0, 1];

            if (v.L2Norm() < 0.0001)
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

        /// <summary>
        /// Project a 3D point (x, y, z) into a camera space (u, v) given the camera matrix and distortion coefficients.
        /// The 3D point is *not* yet in the typically assumed \psi basis (X=Forward, Y=Left, Z=Up).
        /// Instead, X and Y correspond to the image plane X and Y directions, with Z as depth.
        /// </summary>
        /// <param name="cameraMatrix">The camera matrix.</param>
        /// <param name="distCoeffs">The distortion coefficients of the camera.</param>
        /// <param name="point">Input 3D point (X and Y correspond to image dimensions, with Z as depth).</param>
        /// <param name="projectedPoint">Projected 2D point (output).</param>
        public static void Project(Matrix<double> cameraMatrix, Vector<double> distCoeffs, Point3D point, out Point2D projectedPoint)
        {
            double xp = point.X / point.Z;
            double yp = point.Y / point.Z;

            double fx = cameraMatrix[0, 0];
            double fy = cameraMatrix[1, 1];
            double cx = cameraMatrix[0, 2];
            double cy = cameraMatrix[1, 2];
            double k1 = distCoeffs[0];
            double k2 = distCoeffs[1];

            // compute f(xp, yp)
            double rsquared = xp * xp + yp * yp;
            double g = 1 + k1 * rsquared + k2 * rsquared * rsquared;
            double xpp = xp * g;
            double ypp = yp * g;
            projectedPoint = new Point2D(fx * xpp + cx, fy * ypp + cy);
        }

        /// <summary>
        /// Computes a ray intersection with a depth image mesh.
        /// </summary>
        /// <param name="depthImage">Depth image mesh to ray cast against.</param>
        /// <param name="depthIntrinsics">The intrinsics for the depth camera.</param>
        /// <param name="ray">Ray to intersect against depth image mesh.</param>
        /// <param name="maxDistance">The maximum distance to search for (default is 5 meters).</param>
        /// <param name="skipFactor">Distance to march on each step along ray (default is 5 cm).</param>
        /// <param name="undistort">Whether undistortion should be applied to the point.</param>
        /// <returns>Returns point of intersection, or null if no intersection was found.</returns>
        /// <remarks>
        /// The ray is assumed to be defined relative to the pose of the depth camera,
        /// i.e., (0, 0, 0) is the position of the camera itself.
        /// </remarks>
        public static Point3D? ComputeRayIntersection(this DepthImage depthImage, ICameraIntrinsics depthIntrinsics, Ray3D ray, double maxDistance = 5, double skipFactor = 0.05, bool undistort = true)
        {
            // max distance to check for intersection with the scene
            int maxSteps = (int)(maxDistance / skipFactor);

            // size of increment along the ray
            var delta = skipFactor * ray.Direction;

            var hypothesisPoint = ray.ThroughPoint;
            for (int i = 0; i < maxSteps; i++)
            {
                hypothesisPoint += delta;

                // get the mesh distance at the hypothesis point
                if (depthIntrinsics.TryGetPixelPosition(hypothesisPoint, undistort, out var depthPixel) &&
                    depthImage.TryGetPixel((int)Math.Floor(depthPixel.X), (int)Math.Floor(depthPixel.Y), out var depthValue) &&
                    depthValue != 0)
                {
                    // if the mesh distance is less than the distance to the point we've hit the mesh
                    var meshDistanceMeters = (double)depthValue * depthImage.DepthValueToMetersScaleFactor;
                    if (depthImage.DepthValueSemantics == DepthValueSemantics.DistanceToPlane)
                    {
                        if (meshDistanceMeters < hypothesisPoint.X)
                        {
                            return hypothesisPoint;
                        }
                    }
                    else if (depthImage.DepthValueSemantics == DepthValueSemantics.DistanceToPoint)
                    {
                        if (meshDistanceMeters < hypothesisPoint.ToVector3D().Length)
                        {
                            return hypothesisPoint;
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Unhandled {nameof(DepthValueSemantics)}: {depthImage.DepthValueSemantics}");
                    }
                }
            }

            return null;
        }

        private static double CalibrateCamera(
            List<Point3D> worldPoints,
            List<Point2D> imagePoints,
            Vector<double> initialParameters,
            bool computeExtrinsics,
            out Vector<double> outputParameters,
            bool silent = true)
        {
            int numValues = worldPoints.Count;

            // create a new vector for computing and returning our final parameters
            var parametersCount = initialParameters.Count;
            outputParameters = Vector<double>.Build.Dense(parametersCount);
            initialParameters.CopyTo(outputParameters);

            // This is the function that gets passed to the Levenberg-Marquardt optimizer
            Vector<double> OptimizationFunction(Vector<double> p)
            {
                // initialize the error vector
                var fvec = Vector<double>.Build.Dense(numValues * 2);  // each component (x,y) is a separate entry

                // unpack parameters
                int pi = 0;

                // camera matrix
                var k = Matrix<double>.Build.DenseIdentity(3, 3);
                k[0, 0] = p[pi++]; // fx
                k[1, 1] = p[pi++]; // fy
                k[2, 2] = 1;
                k[0, 2] = p[pi++]; // cx
                k[1, 2] = p[pi++]; // cy

                // distortion coefficients
                var d = Vector<double>.Build.Dense(2, 0);
                d[0] = p[pi++]; // k1
                d[1] = p[pi++]; // k2

                Matrix<double> rotationMatrix = null;
                Vector<double> translationVector = null;

                if (computeExtrinsics)
                {
                    // If we are computing extrinsics, that means the world points are not in local
                    // camera coordinates, so we need to also compute rotation and translation
                    var r = Vector<double>.Build.Dense(3);
                    r[0] = p[pi++];
                    r[1] = p[pi++];
                    r[2] = p[pi++];
                    rotationMatrix = AxisAngleToMatrix(r);

                    translationVector = Vector<double>.Build.Dense(3);
                    translationVector[0] = p[pi++];
                    translationVector[1] = p[pi++];
                    translationVector[2] = p[pi++];
                }

                int fveci = 0;
                for (int i = 0; i < numValues; i++)
                {
                    Point3D cameraPoint;
                    if (computeExtrinsics)
                    {
                        // transform world point to local camera coordinates
                        var x = rotationMatrix * worldPoints[i].ToVector();
                        x += translationVector;
                        cameraPoint = new Point3D(x[0], x[1], x[2]);
                    }
                    else
                    {
                        // world points are already in local camera coordinates
                        cameraPoint = worldPoints[i];
                    }

                    // fvec_i = y_i - f(x_i)
                    Project(k, d, cameraPoint, out Point2D projectedPoint);

                    var imagePoint = imagePoints[i];
                    fvec[fveci++] = imagePoint.X - projectedPoint.X;
                    fvec[fveci++] = imagePoint.Y - projectedPoint.Y;
                }

                return fvec;
            }

            // optimize
            var calibrate = new LevenbergMarquardt(OptimizationFunction);
            while (calibrate.State == LevenbergMarquardt.States.Running)
            {
                var rmsError = calibrate.MinimizeOneStep(outputParameters);
                if (!silent)
                {
                    Console.WriteLine("rms error = " + rmsError);
                }
            }

            if (!silent)
            {
                for (int i = 0; i < parametersCount; i++)
                {
                    Console.WriteLine(outputParameters[i] + "\t");
                }

                Console.WriteLine();
            }

            return calibrate.RMSError;
        }

        // Use DLT to obtain estimate of calibration rig pose.
        // This pose estimate will provide a good initial estimate for subsequent projector calibration.
        // Note for a full PnP solution we should probably refine with Levenberg-Marquardt.
        // DLT is described in Hartley and Zisserman p. 178
        private static void DLT(Matrix<double> cameraMatrix, Vector<double> distCoeffs, List<Point3D> worldPoints, List<Point2D> imagePoints, out Matrix<double> rotationMatrix, out Vector<double> translationVector)
        {
            int n = worldPoints.Count;

            var matrixA = Matrix<double>.Build.Dense(2 * n, 12);

            for (int j = 0; j < n; j++)
            {
                var worldPoint = worldPoints[j];
                var imagePoint = imagePoints[j];

                Undistort(cameraMatrix, distCoeffs, imagePoint, out Point2D undistortedPoint);

                int ii = 2 * j;
                matrixA[ii, 4] = -worldPoint.X;
                matrixA[ii, 5] = -worldPoint.Y;
                matrixA[ii, 6] = -worldPoint.Z;
                matrixA[ii, 7] = -1;

                matrixA[ii, 8] = undistortedPoint.Y * worldPoint.X;
                matrixA[ii, 9] = undistortedPoint.Y * worldPoint.Y;
                matrixA[ii, 10] = undistortedPoint.Y * worldPoint.Z;
                matrixA[ii, 11] = undistortedPoint.Y;

                ii++; // next row
                matrixA[ii, 0] = worldPoint.X;
                matrixA[ii, 1] = worldPoint.Y;
                matrixA[ii, 2] = worldPoint.Z;
                matrixA[ii, 3] = 1;

                matrixA[ii, 8] = -undistortedPoint.X * worldPoint.X;
                matrixA[ii, 9] = -undistortedPoint.X * worldPoint.Y;
                matrixA[ii, 10] = -undistortedPoint.X * worldPoint.Z;
                matrixA[ii, 11] = -undistortedPoint.X;
            }

            // Find the eigenvector of ATA with the smallest eignvalue
            var smallestEigenvector = Vector<double>.Build.Dense(12);
            var matrixATransposeA = matrixA.TransposeThisAndMultiply(matrixA);
            matrixATransposeA.Evd().EigenVectors.Column(0).CopyTo(smallestEigenvector);

            // reshape into 3x4 projection matrix
            var p = Matrix<double>.Build.Dense(3, 4);
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        p[i, j] = smallestEigenvector[i * 4 + j];
                    }
                }
            }

            rotationMatrix = Matrix<double>.Build.Dense(3, 3);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    rotationMatrix[i, j] = p[i, j];
                }
            }

            if (rotationMatrix.Determinant() < 0)
            {
                rotationMatrix *= -1;
                p *= -1;
            }

            // orthogonalize R
            {
                var svd = rotationMatrix.Svd();
                rotationMatrix = svd.U * svd.VT;
            }

            // determine scale factor
            var rp = Matrix<double>.Build.Dense(3, 3);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    rp[i, j] = p[i, j];
                }
            }

            double s = rp.L2Norm() / rotationMatrix.L2Norm();

            translationVector = Vector<double>.Build.Dense(3);
            for (int i = 0; i < 3; i++)
            {
                translationVector[i] = p[i, 3];
            }

            translationVector *= 1.0 / s;
        }

        private static void Undistort(Matrix<double> cameraMatrix, Vector<double> distCoeffs, Point2D pointIn, out Point2D pointOut)
        {
            float fx = (float)cameraMatrix[0, 0];
            float fy = (float)cameraMatrix[1, 1];
            float cx = (float)cameraMatrix[0, 2];
            float cy = (float)cameraMatrix[1, 2];
            float[] kappa = new float[] { (float)distCoeffs[0], (float)distCoeffs[1] };
            Undistort(fx, fy, cx, cy, kappa, pointIn, out pointOut);
        }

        private static void Undistort(float fx, float fy, float cx, float cy, float[] kappa, Point2D pointIn, out Point2D pointOut)
        {
            // maps coords in undistorted image (xin, yin) to coords in distorted image (xout, yout)
            double x = (pointIn.X - cx) / fx;
            double y = (pointIn.Y - cy) / fy; // chances are you will need to flip y before passing in: imageHeight - yin

            // Newton Raphson
            double ru = Math.Sqrt(x * x + y * y);
            double rdest = ru;
            double factor = 1.0;

            bool converged = false;
            for (int j = 0; (j < 100) && !converged; j++)
            {
                double rdest2 = rdest * rdest;
                double denom = 1.0;
                double rk = 1.0;

                factor = 1.0;
                for (int k = 0; k < 2; k++)
                {
                    rk *= rdest2;
                    factor += kappa[k] * rk;
                    denom += (2.0 * k + 3.0) * kappa[k] * rk;
                }

                double num = rdest * factor - ru;
                rdest -= num / denom;

                converged = (num / denom) < 0.0001;
            }

            pointOut = new Point2D(x / factor, y / factor);
        }
    }
}
