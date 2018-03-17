// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Microsoft.Kinect;

    internal class KinectInternalCalibration
    {
        public const int depthImageWidth = 512;
        public const int depthImageHeight = 424;
        public const int colorImageWidth = 1920;
        public const int colorImageHeight = 1080;

        public Matrix colorCameraMatrix = new Matrix(3, 3);
        public Matrix colorLensDistortion = new Matrix(5, 1);
        public Matrix depthCameraMatrix = new Matrix(3, 3);
        public Matrix depthLensDistortion = new Matrix(5, 1);
        public Matrix depthToColorTransform = new Matrix(4, 4);

        [XmlIgnoreAttribute]
        public bool silent = true;

        /// <summary>
        /// Converts a camera space point (3D) to the corresponding color space point (2D-RGB camera)
        /// </summary>
        /// <param name="cameraSpacePoint">The color space point to convert</param>
        /// <returns>The corresponding camera space point</returns>
        public ColorSpacePoint ToColorSpacePoint(CameraSpacePoint cameraSpacePoint)
        {
            var colorPoint = new Matrix(4, 1);
            var depthPoint = new Matrix(4, 1);
            depthPoint[0] = (double)cameraSpacePoint.X;
            depthPoint[1] = (double)cameraSpacePoint.Y;
            depthPoint[2] = (double)cameraSpacePoint.Z;
            depthPoint[3] = 1;

            colorPoint.Mult(this.depthToColorTransform, depthPoint);

            double colorX, colorY;
            Project(this.colorCameraMatrix, this.colorLensDistortion, colorPoint[0], colorPoint[1], colorPoint[2], out colorX, out colorY);
            ColorSpacePoint colorSpacePoint;
            colorSpacePoint.X = (float)colorX;
            colorSpacePoint.Y = (float)(colorImageHeight - colorY);
            return colorSpacePoint;
        }

        public void ToCameraSpacePoint(ColorSpacePoint colorSpacePoint, out CameraSpacePoint cameraOrigin3D, out CameraSpacePoint point3D)
        {
            Matrix c2d = new Matrix();
            c2d.Inverse(depthToColorTransform);

            var cameraOrigin = new Matrix(4, 1);
            cameraOrigin[0] = 0;
            cameraOrigin[1] = 0;
            cameraOrigin[2] = 0;
            cameraOrigin[3] = 1;
            var cameraOriginIn3DSpace = new Matrix(4, 1);
            cameraOriginIn3DSpace.Mult(c2d, cameraOrigin);
            cameraOrigin3D.X = (float)cameraOriginIn3DSpace[0];
            cameraOrigin3D.Y = (float)cameraOriginIn3DSpace[1];
            cameraOrigin3D.Z = (float)cameraOriginIn3DSpace[2];

            var pointInImage = new Matrix(4, 1);
            double undistx;
            double undisty;
            Undistort(colorCameraMatrix, colorLensDistortion, colorSpacePoint.X, (colorImageHeight - colorSpacePoint.Y), out undistx, out undisty);
            pointInImage[0] = undistx;
            pointInImage[1] = undisty;
            pointInImage[2] = 1;
            pointInImage[3] = 1;

            var pointIn3DSpace = new Matrix(4, 1);
            pointIn3DSpace.Mult(c2d, pointInImage);
            point3D.X = (float)pointIn3DSpace[0];
            point3D.Y = (float)pointIn3DSpace[1];
            point3D.Z = (float)pointIn3DSpace[2];
        }


        /// <summary>
        /// Converts a cameras space point to a depth space point
        /// </summary>
        /// <param name="cameraSpacePoint">The color space point to convert</param>
        /// <returns>The corresponding camera space point</returns>
        public DepthSpacePoint ToDepthSpacePoint(CameraSpacePoint cameraSpacePoint)
        {
            double depthX, depthY;
            Project(this.depthCameraMatrix, this.depthLensDistortion, cameraSpacePoint.X, cameraSpacePoint.Y, cameraSpacePoint.Z, out depthX, out depthY);
            DepthSpacePoint depthSpacePoint;
            depthSpacePoint.X = (float)depthX;
            depthSpacePoint.Y = (float)(depthImageHeight - depthY);

            return depthSpacePoint;
        }

        public void RecoverCalibrationFromSensor(Microsoft.Kinect.KinectSensor kinectSensor)
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var objectPoints1 = new List<Matrix>();
            var colorPoints1 = new List<System.Drawing.PointF>();
            var depthPoints1 = new List<System.Drawing.PointF>();

            int n = 0;
            for (float x = -2f; x < 2f; x += 0.2f)
                for (float y = -2f; y < 2f; y += 0.2f)
                    for (float z = 0.4f; z < 4.5f; z += 0.4f)
                    {
                        var kinectCameraPoint = new CameraSpacePoint();
                        kinectCameraPoint.X = x;
                        kinectCameraPoint.Y = y;
                        kinectCameraPoint.Z = z;

                        // use SDK's projection
                        // adjust Y to make RH cooridnate system that is a projection of Kinect 3D points
                        var kinectColorPoint = kinectSensor.CoordinateMapper.MapCameraPointToColorSpace(kinectCameraPoint);
                        kinectColorPoint.Y = colorImageHeight - kinectColorPoint.Y;
                        var kinectDepthPoint = kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(kinectCameraPoint);
                        kinectDepthPoint.Y = depthImageHeight - kinectDepthPoint.Y;

                        if ((kinectColorPoint.X >= 0) && (kinectColorPoint.X < colorImageWidth) &&
                            (kinectColorPoint.Y >= 0) && (kinectColorPoint.Y < colorImageHeight) &&
                            (kinectDepthPoint.X >= 0) && (kinectDepthPoint.X < depthImageWidth) &&
                            (kinectDepthPoint.Y >= 0) && (kinectDepthPoint.Y < depthImageHeight))
                        {
                            n++;
                            var objectPoint = new Matrix(3, 1);
                            objectPoint[0] = kinectCameraPoint.X;
                            objectPoint[1] = kinectCameraPoint.Y;
                            objectPoint[2] = kinectCameraPoint.Z;
                            objectPoints1.Add(objectPoint);

                            var colorPoint = new System.Drawing.PointF();
                            colorPoint.X = kinectColorPoint.X;
                            colorPoint.Y = kinectColorPoint.Y;
                            colorPoints1.Add(colorPoint);


                            //Console.WriteLine(objectPoint[0] + "\t" + objectPoint[1] + "\t" + colorPoint.X + "\t" + colorPoint.Y);

                            var depthPoint = new System.Drawing.PointF();
                            depthPoint.X = kinectDepthPoint.X;
                            depthPoint.Y = kinectDepthPoint.Y;
                            depthPoints1.Add(depthPoint);
                        }
                    }

            colorCameraMatrix[0, 0] = 1000; //fx
            colorCameraMatrix[1, 1] = 1000; //fy
            colorCameraMatrix[0, 2] = colorImageWidth / 2; //cx
            colorCameraMatrix[1, 2] = colorImageHeight / 2; //cy
            colorCameraMatrix[2, 2] = 1;

            var rotation = new Matrix(3, 1);
            var translation = new Matrix(3, 1);
            var colorError = CalibrateColorCamera(objectPoints1, colorPoints1, colorCameraMatrix, colorLensDistortion, rotation, translation, silent);
            var rotationMatrix = Orientation.Rodrigues(rotation);

            depthToColorTransform = Matrix.Identity(4, 4);
            for (int i = 0; i < 3; i++)
            {
                depthToColorTransform[i, 3] = translation[i];
                for (int j = 0; j < 3; j++)
                    depthToColorTransform[i, j] = rotationMatrix[i, j];
            }


            depthCameraMatrix[0, 0] = 360; //fx
            depthCameraMatrix[1, 1] = 360; //fy
            depthCameraMatrix[0, 2] = depthImageWidth / 2; //cx
            depthCameraMatrix[1, 2] = depthImageHeight / 2; //cy
            depthCameraMatrix[2, 2] = 1;

            var depthError = CalibrateDepthCamera(objectPoints1, depthPoints1, depthCameraMatrix, depthLensDistortion, silent);

            // check projections
            double depthProjectionError = 0;
            double colorProjectionError = 0;
            var color = new Matrix(4, 1);
            var testObjectPoint4 = new Matrix(4, 1);
            for (int i = 0; i < n; i++)
            {
                var testObjectPoint = objectPoints1[i];
                var testDepthPoint = depthPoints1[i];
                var testColorPoint = colorPoints1[i];

                // "camera space" == depth camera space
                // depth camera projection
                double depthU, depthV;
                Project(depthCameraMatrix, depthLensDistortion, testObjectPoint[0], testObjectPoint[1], testObjectPoint[2], out depthU, out depthV);

                double dx = testDepthPoint.X - depthU;
                double dy = testDepthPoint.Y - depthV;
                depthProjectionError += (dx * dx) + (dy * dy);

                // color camera projection
                testObjectPoint4[0] = testObjectPoint[0];
                testObjectPoint4[1] = testObjectPoint[1];
                testObjectPoint4[2] = testObjectPoint[2];
                testObjectPoint4[3] = 1;

                color.Mult(depthToColorTransform, testObjectPoint4);
                color.Scale(1.0 / color[3]); // not necessary for this transform

                double colorU, colorV;
                Project(colorCameraMatrix, colorLensDistortion, color[0], color[1], color[2], out colorU, out colorV);

                dx = testColorPoint.X - colorU;
                dy = testColorPoint.Y - colorV;
                colorProjectionError += (dx * dx) + (dy * dy);
            }
            depthProjectionError /= n;
            colorProjectionError /= n;


            stopWatch.Stop();
            if (!silent)
            {
                Console.WriteLine("FakeCalibration :");
                Console.WriteLine("n = " + n);
                Console.WriteLine("color error = " + colorError);
                Console.WriteLine("depth error = " + depthError);
                Console.WriteLine("depth reprojection error = " + depthProjectionError);
                Console.WriteLine("color reprojection error = " + colorProjectionError);
                Console.WriteLine("depth camera matrix = \n" + depthCameraMatrix);
                Console.WriteLine("depth lens distortion = \n" + depthLensDistortion);
                Console.WriteLine("color camera matrix = \n" + colorCameraMatrix);
                Console.WriteLine("color lens distortion = \n" + colorLensDistortion);

                Console.WriteLine(stopWatch.ElapsedMilliseconds + " ms");
                Console.WriteLine("________________________________________________________");
            }
        }

        public static void Project(Matrix cameraMatrix, Matrix distCoeffs, double x, double y, double z, out double u, out double v)
        {
            double xp = x / z;
            double yp = y / z;

            double fx = cameraMatrix[0, 0];
            double fy = cameraMatrix[1, 1];
            double cx = cameraMatrix[0, 2];
            double cy = cameraMatrix[1, 2];
            double k1 = distCoeffs[0];
            double k2 = distCoeffs[1];

            // compute f(xp, yp)
            double rSquared = xp * xp + yp * yp;
            double xpp = xp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
            double ypp = yp * (1 + k1 * rSquared + k2 * rSquared * rSquared);
            u = fx * xpp + cx;
            v = fy * ypp + cy;
        }

        public static void Undistort(Matrix cameraMatrix, Matrix distCoeffs, double xin, double yin, out double xout, out double yout)
        {
            float fx = (float)cameraMatrix[0, 0];
            float fy = (float)cameraMatrix[1, 1];
            float cx = (float)cameraMatrix[0, 2];
            float cy = (float)cameraMatrix[1, 2];
            float[] kappa = new float[] { (float)distCoeffs[0], (float)distCoeffs[1] };
            Undistort(fx, fy, cx, cy, kappa, xin, yin, out xout, out yout);
        }

        public static void Undistort(float fx, float fy, float cx, float cy, float[] kappa, double xin, double yin, out double xout, out double yout)
        {
            // maps coords in undistorted image (xin, yin) to coords in distorted image (xout, yout)
            double x = (xin - cx) / fx;
            double y = (yin - cy) / fy; // chances are you will need to flip y before passing in: imageHeight - yin

            // Newton Raphson
            double ru = Math.Sqrt(x * x + y * y);
            double rdest = ru;
            double factor = 1.0;

            bool converged = false;
            for (int j = 0; (j < 100) && !converged; j++)
            {
                double rdest2 = rdest * rdest;
                double num = 1.0, denom = 1.0;
                double rk = 1.0;

                factor = 1.0;
                for (int k = 0; k < 2; k++)
                {
                    rk *= rdest2;
                    factor += kappa[k] * rk;
                    denom += (2.0 * k + 3.0) * kappa[k] * rk;
                }
                num = rdest * factor - ru;
                rdest -= (num / denom);

                converged = (num / denom) < 0.0001;
            }
            xout = x / factor;
            yout = y / factor;
        }

        public Microsoft.Kinect.PointF[] ComputeDepthFrameToCameraSpaceTable()
        {
            float fx = (float)depthCameraMatrix[0, 0];
            float fy = (float)depthCameraMatrix[1, 1];
            float cx = (float)depthCameraMatrix[0, 2];
            float cy = (float)depthCameraMatrix[1, 2];
            float[] kappa = new float[] { (float)depthLensDistortion[0], (float)depthLensDistortion[1] };

            var table = new Microsoft.Kinect.PointF[depthImageWidth * depthImageHeight];

            for (int framey = 0; framey < depthImageHeight; framey++)
                for (int framex = 0; framex < depthImageWidth; framex++)
                {
                    double xout, yout;
                    Undistort(fx, fy, cx, cy, kappa, framex, (depthImageHeight - framey), out xout, out yout);

                    var point = new Microsoft.Kinect.PointF();
                    point.X = (float)xout;
                    point.Y = (float)yout;
                    table[depthImageWidth * framey + framex] = point;
                }
            return table;
        }

        static double CalibrateDepthCamera(List<Matrix> worldPoints, List<System.Drawing.PointF> imagePoints, Matrix cameraMatrix, Matrix distCoeffs, bool silent)
        {
            int nPoints = worldPoints.Count;

            // pack parameters into vector
            // parameters: fx, fy, cx, cy, k1, k2 = 6 parameters
            int nParameters = 6;
            var parameters = new Matrix(nParameters, 1);

            {
                int pi = 0;
                parameters[pi++] = cameraMatrix[0, 0]; // fx
                parameters[pi++] = cameraMatrix[1, 1]; // fy
                parameters[pi++] = cameraMatrix[0, 2]; // cx
                parameters[pi++] = cameraMatrix[1, 2]; // cy
                parameters[pi++] = distCoeffs[0]; // k1
                parameters[pi++] = distCoeffs[1]; // k2
            }

            // size of our error vector
            int nValues = nPoints * 2; // each component (x,y) is a separate entry

            LevenbergMarquardt.Function function = delegate (Matrix p)
            {
                var fvec = new Matrix(nValues, 1);

                // unpack parameters
                int pi = 0;
                double fx = p[pi++];
                double fy = p[pi++];
                double cx = p[pi++];
                double cy = p[pi++];
                double k1 = p[pi++];
                double k2 = p[pi++];

                var K = Matrix.Identity(3, 3);
                K[0, 0] = fx;
                K[1, 1] = fy;
                K[0, 2] = cx;
                K[1, 2] = cy;

                var d = Matrix.Zero(5, 1);
                d[0] = k1;
                d[1] = k2;

                int fveci = 0;
                for (int i = 0; i < worldPoints.Count; i++)
                {
                    double u, v;
                    var x = worldPoints[i];
                    KinectInternalCalibration.Project(K, d, x[0], x[1], x[2], out u, out v);

                    var imagePoint = imagePoints[i];
                    fvec[fveci++] = imagePoint.X - u;
                    fvec[fveci++] = imagePoint.Y - v;
                }
                return fvec;
            };

            // optimize
            var calibrate = new LevenbergMarquardt(function);
            while (calibrate.State == LevenbergMarquardt.States.Running)
            {
                var rmsError = calibrate.MinimizeOneStep(parameters);
                if (!silent) Console.WriteLine("rms error = " + rmsError);
            }
            if (!silent)
            {
                for (int i = 0; i < nParameters; i++)
                    Console.WriteLine(parameters[i] + "\t");
                Console.WriteLine();
            }

            // unpack parameters
            {
                int pi = 0;
                double fx = parameters[pi++];
                double fy = parameters[pi++];
                double cx = parameters[pi++];
                double cy = parameters[pi++];
                double k1 = parameters[pi++];
                double k2 = parameters[pi++];
                cameraMatrix[0, 0] = fx;
                cameraMatrix[1, 1] = fy;
                cameraMatrix[0, 2] = cx;
                cameraMatrix[1, 2] = cy;
                distCoeffs[0] = k1;
                distCoeffs[1] = k2;
            }

            return calibrate.RMSError;
        }

        static double CalibrateColorCamera(List<Matrix> worldPoints, List<System.Drawing.PointF> imagePoints, Matrix cameraMatrix, Matrix distCoeffs, Matrix rotation, Matrix translation, bool silent)
        {
            int nPoints = worldPoints.Count;

            {
                Matrix R, t;
                DLT(cameraMatrix, distCoeffs, worldPoints, imagePoints, out R, out t);
                var r = Orientation.RotationVector(R);
                rotation.Copy(r);
                translation.Copy(t);
            }

            // pack parameters into vector
            // parameters: fx, fy, cx, cy, k1, k2, + 3 for rotation, 3 translation = 12
            int nParameters = 12;
            var parameters = new Matrix(nParameters, 1);

            {
                int pi = 0;
                parameters[pi++] = cameraMatrix[0, 0]; // fx
                parameters[pi++] = cameraMatrix[1, 1]; // fy
                parameters[pi++] = cameraMatrix[0, 2]; // cx
                parameters[pi++] = cameraMatrix[1, 2]; // cy
                parameters[pi++] = distCoeffs[0]; // k1
                parameters[pi++] = distCoeffs[1]; // k2
                parameters[pi++] = rotation[0];
                parameters[pi++] = rotation[1];
                parameters[pi++] = rotation[2];
                parameters[pi++] = translation[0];
                parameters[pi++] = translation[1];
                parameters[pi++] = translation[2];

            }

            // size of our error vector
            int nValues = nPoints * 2; // each component (x,y) is a separate entry

            LevenbergMarquardt.Function function = delegate (Matrix p)
            {
                var fvec = new Matrix(nValues, 1);

                // unpack parameters
                int pi = 0;
                double fx = p[pi++];
                double fy = p[pi++];
                double cx = p[pi++];
                double cy = p[pi++];

                double k1 = p[pi++];
                double k2 = p[pi++];

                var K = Matrix.Identity(3, 3);
                K[0, 0] = fx;
                K[1, 1] = fy;
                K[0, 2] = cx;
                K[1, 2] = cy;

                var d = Matrix.Zero(5, 1);
                d[0] = k1;
                d[1] = k2;

                var r = new Matrix(3, 1);
                r[0] = p[pi++];
                r[1] = p[pi++];
                r[2] = p[pi++];

                var t = new Matrix(3, 1);
                t[0] = p[pi++];
                t[1] = p[pi++];
                t[2] = p[pi++];

                var R = Orientation.Rodrigues(r);



                var x = new Matrix(3, 1);

                int fveci = 0;
                for (int i = 0; i < worldPoints.Count; i++)
                {
                    // transform world point to local camera coordinates
                    x.Mult(R, worldPoints[i]);
                    x.Add(t);

                    // fvec_i = y_i - f(x_i)
                    double u, v;
                    KinectInternalCalibration.Project(K, d, x[0], x[1], x[2], out u, out v);

                    var imagePoint = imagePoints[i];
                    fvec[fveci++] = imagePoint.X - u;
                    fvec[fveci++] = imagePoint.Y - v;
                }
                return fvec;
            };

            // optimize
            var calibrate = new LevenbergMarquardt(function);
            while (calibrate.State == LevenbergMarquardt.States.Running)
            {
                var rmsError = calibrate.MinimizeOneStep(parameters);
                if (!silent) Console.WriteLine("rms error = " + rmsError);
            }
            if (!silent)
            {
                for (int i = 0; i < nParameters; i++)
                    Console.WriteLine(parameters[i] + "\t");
                Console.WriteLine();
            }
            // unpack parameters
            {
                int pi = 0;
                double fx = parameters[pi++];
                double fy = parameters[pi++];
                double cx = parameters[pi++];
                double cy = parameters[pi++];
                double k1 = parameters[pi++];
                double k2 = parameters[pi++];
                cameraMatrix[0, 0] = fx;
                cameraMatrix[1, 1] = fy;
                cameraMatrix[0, 2] = cx;
                cameraMatrix[1, 2] = cy;
                distCoeffs[0] = k1;
                distCoeffs[1] = k2;
                rotation[0] = parameters[pi++];
                rotation[1] = parameters[pi++];
                rotation[2] = parameters[pi++];
                translation[0] = parameters[pi++];
                translation[1] = parameters[pi++];
                translation[2] = parameters[pi++];
            }


            return calibrate.RMSError;
        }

        // Use DLT to obtain estimate of calibration rig pose; in our case this is the pose of the Kinect camera.
        // This pose estimate will provide a good initial estimate for subsequent projector calibration.
        // Note for a full PnP solution we should probably refine with Levenberg-Marquardt.
        // DLT is described in Hartley and Zisserman p. 178
        static void DLT(Matrix cameraMatrix, Matrix distCoeffs, List<Matrix> worldPoints, List<System.Drawing.PointF> imagePoints, out Matrix R, out Matrix t)
        {
            int n = worldPoints.Count;

            var A = Matrix.Zero(2 * n, 12);

            for (int j = 0; j < n; j++)
            {
                var X = worldPoints[j];
                var imagePoint = imagePoints[j];

                double x, y;
                Undistort(cameraMatrix, distCoeffs, imagePoint.X, imagePoint.Y, out x, out y);

                int ii = 2 * j;
                A[ii, 4] = -X[0];
                A[ii, 5] = -X[1];
                A[ii, 6] = -X[2];
                A[ii, 7] = -1;

                A[ii, 8] = y * X[0];
                A[ii, 9] = y * X[1];
                A[ii, 10] = y * X[2];
                A[ii, 11] = y;

                ii++; // next row
                A[ii, 0] = X[0];
                A[ii, 1] = X[1];
                A[ii, 2] = X[2];
                A[ii, 3] = 1;

                A[ii, 8] = -x * X[0];
                A[ii, 9] = -x * X[1];
                A[ii, 10] = -x * X[2];
                A[ii, 11] = -x;
            }

            // Pcolumn is the eigenvector of ATA with the smallest eignvalue
            var Pcolumn = new Matrix(12, 1);
            {
                var ATA = new Matrix(12, 12);
                ATA.MultATA(A, A);

                var V = new Matrix(12, 12);
                var ww = new Matrix(12, 1);
                ATA.Eig(V, ww);

                Pcolumn.CopyCol(V, 0);
            }

            // reshape into 3x4 projection matrix
            var P = new Matrix(3, 4);
            P.Reshape(Pcolumn);

            R = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    R[i, j] = P[i, j];

            if (R.Det3x3() < 0)
            {
                R.Scale(-1);
                P.Scale(-1);
            }

            // orthogonalize R
            {
                var U = new Matrix(3, 3);
                var V = new Matrix(3, 3);
                var ww = new Matrix(3, 1);
                R.SVD(U, ww, V);
                R.MultAAT(U, V);
            }

            // determine scale factor
            var RP = new Matrix(3, 3);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    RP[i, j] = P[i, j];
            double s = RP.Norm() / R.Norm();

            t = new Matrix(3, 1);
            for (int i = 0; i < 3; i++)
                t[i] = P[i, 3];
            t.Scale(1.0 / s);
        }

    }
}
