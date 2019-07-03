// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System.Collections.Generic;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Producer that project points into 3D.
    /// </summary>
    public sealed class ProjectTo3D : ConsumerProducer<(Shared<Image>, List<Point2D>, IKinectCalibration), List<Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectTo3D"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        public ProjectTo3D(Pipeline pipeline)
            : base(pipeline)
        {
        }

        /// <summary>
        /// Method for projecting a point in pixel coordinate from the color camera into the world coordinates by determining the corresponding depth pixel.
        /// </summary>
        /// <param name="kinectCalibration">Calibration object for the Kinect camera.</param>
        /// <param name="point2D">Pixel coordinates in the color camera.</param>
        /// <param name="colorExtrinsicsInverse">The inverse of the color extrinsics matrix (kinectCalibration.ColorExtrinsics). Passed in so that it isn't computed each time this method is called.</param>
        /// <param name="depthImage">Depth map.</param>
        /// <returns>Point in world coordinates.</returns>
        public static Point3D? ColorPointToWorldSpace(IKinectCalibration kinectCalibration, Point2D point2D, Matrix<double> colorExtrinsicsInverse, Shared<Image> depthImage)
        {
            Point3D pointInCameraSpace = kinectCalibration.ColorIntrinsics.ToCameraSpace(point2D, 1.0, true);
            double x = pointInCameraSpace.X * colorExtrinsicsInverse[0, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[0, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[0, 2] + colorExtrinsicsInverse[0, 3];
            double y = pointInCameraSpace.X * colorExtrinsicsInverse[1, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[1, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[1, 2] + colorExtrinsicsInverse[1, 3];
            double z = pointInCameraSpace.X * colorExtrinsicsInverse[2, 0] + pointInCameraSpace.Y * colorExtrinsicsInverse[2, 1] + pointInCameraSpace.Z * colorExtrinsicsInverse[2, 2] + colorExtrinsicsInverse[2, 3];
            Point3D pointInWorldSpace = new Point3D(x, y, z);
            Point3D cameraOriginInWorldSpace = new Point3D(colorExtrinsicsInverse[0, 3], colorExtrinsicsInverse[1, 3], colorExtrinsicsInverse[2, 3]);
            Line3D rgbLine = new Line3D(cameraOriginInWorldSpace, pointInWorldSpace);
            return DepthExtensions.IntersectLineWithDepthMesh(kinectCalibration, rgbLine, depthImage.Resource, 0.1);
        }

        /// <summary>
        /// Callback from pipeline that receives an image, a list of points, and a calibration.
        /// </summary>
        /// <param name="data">list of points to transform, associated camera image, and calibration.</param>
        /// <param name="e">Pipeline sample information.</param>
        protected override void Receive((Shared<Image>, List<Point2D>, IKinectCalibration) data, Envelope e)
        {
            var point2DList = data.Item2;
            var depthImage = data.Item1;
            var kinectCalibration = data.Item3;
            List<Point3D> point3DList = new List<Point3D>();

            if (kinectCalibration != null)
            {
                var colorExtrinsicsInverse = kinectCalibration.ColorExtrinsics.Inverse();
                foreach (var point2D in point2DList)
                {
                    var result = ProjectTo3D.ColorPointToWorldSpace(kinectCalibration, point2D, colorExtrinsicsInverse, depthImage);
                    if (result != null)
                    {
                        point3DList.Add(result.Value);
                    }
                }

                this.Out.Post(point3DList, e.OriginatingTime);
            }
        }
    }
}
