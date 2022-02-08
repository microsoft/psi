// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Kinect;
    using Microsoft.Psi.Calibration;
    using static Microsoft.Psi.Calibration.CalibrationExtensions;

    internal class KinectInternalCalibration
    {
        public const int depthImageWidth = 512;
        public const int depthImageHeight = 424;
        public const int colorImageWidth = 1920;
        public const int colorImageHeight = 1080;

        public Matrix<double> colorCameraMatrix;
        public Vector<double> colorLensDistortion;
        public Matrix<double> depthCameraMatrix;
        public Vector<double> depthLensDistortion;
        public Matrix<double> depthToColorTransform;

        [XmlIgnoreAttribute]
        public bool silent = true;

        internal void RecoverCalibrationFromSensor(Microsoft.Kinect.KinectSensor kinectSensor)
        {
            var stopWatch = new System.Diagnostics.Stopwatch();

            if (!this.silent)
            {
                stopWatch.Start();
            }

            var objectPoints1 = new List<Point3D>();
            var colorPoints1 = new List<Point2D>();
            var depthPoints1 = new List<Point2D>();

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
                            objectPoints1.Add(new Point3D(kinectCameraPoint.X, kinectCameraPoint.Y, kinectCameraPoint.Z));

                            var colorPoint = new Point2D(kinectColorPoint.X, kinectColorPoint.Y);
                            colorPoints1.Add(colorPoint);

                            var depthPoint = new Point2D(kinectDepthPoint.X, kinectDepthPoint.Y);
                            depthPoints1.Add(depthPoint);
                        }
                    }

            var initialColorCameraMatrix = Matrix<double>.Build.Dense(3, 3);
            var initialColorDistortion = Vector<double>.Build.Dense(2);
            initialColorCameraMatrix[0, 0] = 1000; //fx
            initialColorCameraMatrix[1, 1] = 1000; //fy
            initialColorCameraMatrix[0, 2] = colorImageWidth / 2; //cx
            initialColorCameraMatrix[1, 2] = colorImageHeight / 2; //cy
            initialColorCameraMatrix[2, 2] = 1;

            var colorError = CalibrateCameraIntrinsicsAndExtrinsics(
                objectPoints1,
                colorPoints1,
                initialColorCameraMatrix,
                initialColorDistortion,
                out this.colorCameraMatrix,
                out this.colorLensDistortion,
                out var rotation,
                out var translation,
                this.silent);
            var rotationMatrix = AxisAngleToMatrix(rotation);

            this.depthToColorTransform = Matrix<double>.Build.DenseIdentity(4, 4);
            for (int i = 0; i < 3; i++)
            {
                this.depthToColorTransform[i, 3] = translation[i];
                for (int j = 0; j < 3; j++)
                    this.depthToColorTransform[i, j] = rotationMatrix[i, j];
            }

            var initialDepthCameraMatrix = Matrix<double>.Build.Dense(3, 3);
            var initialDepthDistortion = Vector<double>.Build.Dense(2);
            initialDepthCameraMatrix[0, 0] = 360; //fx
            initialDepthCameraMatrix[1, 1] = 360; //fy
            initialDepthCameraMatrix[0, 2] = depthImageWidth / 2.0; //cx
            initialDepthCameraMatrix[1, 2] = depthImageHeight / 2.0; //cy
            initialDepthCameraMatrix[2, 2] = 1;

            var depthError = CalibrateCameraIntrinsics(
                objectPoints1,
                depthPoints1,
                initialDepthCameraMatrix,
                initialDepthDistortion,
                out this.depthCameraMatrix,
                out this.depthLensDistortion,
                this.silent);

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
                Project(depthCameraMatrix, depthLensDistortion, testObjectPoint, out Point2D projectedDepthPoint);

                double dx = testDepthPoint.X - projectedDepthPoint.X;
                double dy = testDepthPoint.Y - projectedDepthPoint.Y;
                depthProjectionError += (dx * dx) + (dy * dy);

                // color camera projection
                testObjectPoint4[0] = testObjectPoint.X;
                testObjectPoint4[1] = testObjectPoint.Y;
                testObjectPoint4[2] = testObjectPoint.Z;
                testObjectPoint4[3] = 1;

                var color = depthToColorTransform * testObjectPoint4;
                color *= 1.0 / color[3]; // not necessary for this transform

                Project(colorCameraMatrix, colorLensDistortion, new Point3D(color[0], color[1], color[2]), out Point2D projectedColorPoint);

                dx = testColorPoint.X - projectedColorPoint.X;
                dy = testColorPoint.Y - projectedColorPoint.Y;
                colorProjectionError += (dx * dx) + (dy * dy);
            }
            depthProjectionError /= n;
            colorProjectionError /= n;

            if (!this.silent)
            {
                stopWatch.Stop();
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
    }
}
