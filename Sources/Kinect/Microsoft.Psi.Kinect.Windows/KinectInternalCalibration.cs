// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using MathNet.Numerics.LinearAlgebra;
    using Microsoft.Kinect;
    using Microsoft.Psi.Calibration;
    using static Microsoft.Psi.Calibration.CalibrationExtensions;

    internal class KinectInternalCalibration
    {
        public const int depthImageWidth = 512;
        public const int depthImageHeight = 424;
        public const int colorImageWidth = 1920;
        public const int colorImageHeight = 1080;

        public Matrix<double> colorCameraMatrix = Matrix<double>.Build.Dense(3, 3);
        public Vector<double> colorLensDistortion = Vector<double>.Build.Dense(5);
        public Matrix<double> depthCameraMatrix = Matrix<double>.Build.Dense(3, 3);
        public Vector<double> depthLensDistortion = Vector<double>.Build.Dense(5);
        public Matrix<double> depthToColorTransform = Matrix<double>.Build.Dense(4, 4);

        [XmlIgnoreAttribute]
        public bool silent = true;

        internal void RecoverCalibrationFromSensor(Microsoft.Kinect.KinectSensor kinectSensor)
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            var objectPoints1 = new List<Vector<double>>();
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
                        // adjust Y to make RH coordinate system that is a projection of Kinect 3D points
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
                            var objectPoint = Vector<double>.Build.Dense(3);
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

            this.colorCameraMatrix[0, 0] = 1000; //fx
            this.colorCameraMatrix[1, 1] = 1000; //fy
            this.colorCameraMatrix[0, 2] = colorImageWidth / 2; //cx
            this.colorCameraMatrix[1, 2] = colorImageHeight / 2; //cy
            this.colorCameraMatrix[2, 2] = 1;

            var rotation = Vector<double>.Build.Dense(3);
            var translation = Vector<double>.Build.Dense(3);
            var colorError = CalibrateColorCamera(objectPoints1, colorPoints1, colorCameraMatrix, colorLensDistortion, rotation, translation, this.silent);
            var rotationMatrix = AxisAngleToMatrix(rotation);

            this.depthToColorTransform = Matrix<double>.Build.DenseIdentity(4, 4);
            for (int i = 0; i < 3; i++)
            {
                this.depthToColorTransform[i, 3] = translation[i];
                for (int j = 0; j < 3; j++)
                    this.depthToColorTransform[i, j] = rotationMatrix[i, j];
            }


            this.depthCameraMatrix[0, 0] = 360; //fx
            this.depthCameraMatrix[1, 1] = 360; //fy
            this.depthCameraMatrix[0, 2] = depthImageWidth / 2.0; //cx
            this.depthCameraMatrix[1, 2] = depthImageHeight / 2.0; //cy
            this.depthCameraMatrix[2, 2] = 1;

            var depthError = CalibrateDepthCamera(objectPoints1, depthPoints1, depthCameraMatrix, depthLensDistortion, silent);

            // check projections
            double depthProjectionError = 0;
            double colorProjectionError = 0;
            var testObjectPoint4 = Vector<double>.Build.Dense(4);
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

                var color = depthToColorTransform * testObjectPoint4;
                color *= (1.0 / color[3]); // not necessary for this transform

                double colorU, colorV;
                Project(colorCameraMatrix, colorLensDistortion, color[0], color[1], color[2], out colorU, out colorV);

                dx = testColorPoint.X - colorU;
                dy = testColorPoint.Y - colorV;
                colorProjectionError += (dx * dx) + (dy * dy);
            }
            depthProjectionError /= n;
            colorProjectionError /= n;


            stopWatch.Stop();
            if (!this.silent)
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

        private static void Project(Matrix<double> cameraMatrix, Vector<double> distCoeffs, double x, double y, double z, out double u, out double v)
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

        private static void Undistort(Matrix<double> cameraMatrix, Vector<double> distCoeffs, double xin, double yin, out double xout, out double yout)
        {
            float fx = (float)cameraMatrix[0, 0];
            float fy = (float)cameraMatrix[1, 1];
            float cx = (float)cameraMatrix[0, 2];
            float cy = (float)cameraMatrix[1, 2];
            float[] kappa = new float[] { (float)distCoeffs[0], (float)distCoeffs[1] };
            Undistort(fx, fy, cx, cy, kappa, xin, yin, out xout, out yout);
        }

        private static void Undistort(float fx, float fy, float cx, float cy, float[] kappa, double xin, double yin, out double xout, out double yout)
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

        private static double CalibrateDepthCamera(List<Vector<double>> worldPoints, List<System.Drawing.PointF> imagePoints, Matrix<double> cameraMatrix, Vector<double> distCoeffs, bool silent = true)
        {
            int nPoints = worldPoints.Count;

            // pack parameters into vector
            // parameters: fx, fy, cx, cy, k1, k2 = 6 parameters
            int nParameters = 6;
            var parameters = Vector<double>.Build.Dense(nParameters);

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

            LevenbergMarquardt.Function function = delegate (Vector<double> p)
            {
                var fvec = Vector<double>.Build.Dense(nValues);

                // unpack parameters
                int pi = 0;
                double fx = p[pi++];
                double fy = p[pi++];
                double cx = p[pi++];
                double cy = p[pi++];
                double k1 = p[pi++];
                double k2 = p[pi++];

                var K = Matrix<double>.Build.DenseIdentity(3, 3);
                K[0, 0] = fx;
                K[1, 1] = fy;
                K[0, 2] = cx;
                K[1, 2] = cy;

                var d = Vector<double>.Build.Dense(5, 0);
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

        private static double CalibrateColorCamera(List<Vector<double>> worldPoints, List<System.Drawing.PointF> imagePoints, Matrix<double> cameraMatrix, Vector<double> distCoeffs, Vector<double> rotation, Vector<double> translation, bool silent = true)
        {
            int nPoints = worldPoints.Count;

            {
                Matrix<double> R;
                Vector<double> t;
                DLT(cameraMatrix, distCoeffs, worldPoints, imagePoints, out R, out t);
                var r = MatrixToAxisAngle(R);
                r.CopyTo(rotation);
                t.CopyTo(translation);
            }

            // pack parameters into vector
            // parameters: fx, fy, cx, cy, k1, k2, + 3 for rotation, 3 translation = 12
            int nParameters = 12;
            var parameters = Vector<double>.Build.Dense(nParameters);
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

            LevenbergMarquardt.Function function = delegate (Vector<double> p)
            {
                var fvec = Vector<double>.Build.Dense(nValues);

                // unpack parameters
                int pi = 0;
                double fx = p[pi++];
                double fy = p[pi++];
                double cx = p[pi++];
                double cy = p[pi++];

                double k1 = p[pi++];
                double k2 = p[pi++];

                var K = Matrix<double>.Build.DenseIdentity(3, 3);
                K[0, 0] = fx;
                K[1, 1] = fy;
                K[0, 2] = cx;
                K[1, 2] = cy;

                var d = Vector<double>.Build.Dense(5, 0);
                d[0] = k1;
                d[1] = k2;

                var r = Vector<double>.Build.Dense(3);
                r[0] = p[pi++];
                r[1] = p[pi++];
                r[2] = p[pi++];

                var t = Vector<double>.Build.Dense(3);
                t[0] = p[pi++];
                t[1] = p[pi++];
                t[2] = p[pi++];

                var R = AxisAngleToMatrix(r);

                int fveci = 0;
                for (int i = 0; i < worldPoints.Count; i++)
                {
                    // transform world point to local camera coordinates
                    var x = R * worldPoints[i];
                    x += t;

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
        private static void DLT(Matrix<double> cameraMatrix, Vector<double>distCoeffs, List<Vector<double>> worldPoints, List<System.Drawing.PointF> imagePoints, out Matrix<double> R, out Vector<double>t)
        {
            int n = worldPoints.Count;

            var A = Matrix<double>.Build.Dense(2 * n, 12);

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
            var Pcolumn = Vector<double>.Build.Dense(12);
            {
                var ATA = A.TransposeThisAndMultiply(A);
                ATA.Evd().EigenVectors.Column(0).CopyTo(Pcolumn);
            }

            // reshape into 3x4 projection matrix
            var P = Matrix<double>.Build.Dense(3, 4);
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        P[i, j] = Pcolumn[i*4 + j];
                    }
                }
            }

            R = Matrix<double>.Build.Dense(3, 3);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    R[i, j] = P[i, j];

            if (R.Determinant() < 0)
            {
                R *= -1;
                P *= -1;
            }

            // orthogonalize R
            {
                var svd = R.Svd();
                R = svd.U * svd.VT;
            }

            // determine scale factor
            var RP = Matrix<double>.Build.Dense(3, 3);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    RP[i, j] = P[i, j];
            double s = RP.L2Norm() / R.L2Norm();

            t = Vector<double>.Build.Dense(3);
            for (int i = 0; i < 3; i++)
                t[i] = P[i, 3];
            t *= (1.0 / s);
        }
    }
}
