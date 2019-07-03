// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;

    /// <summary>
    /// Represents the calibration information (intrinsics and extrinsics of color and depth cameras) for a Kinect sensor.
    /// </summary>
    public class KinectCalibration : IKinectCalibration
    {
        /// <summary>
        /// The default calibration.
        /// </summary>
        public static readonly KinectCalibration Default = new KinectCalibration();

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectCalibration"/> class.
        /// </summary>
        public KinectCalibration()
        {
            this.ColorIntrinsics = new CameraIntrinsics(
                1920,
                1080,
                Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { 1061.045, 0,          1003.34 },
                    { 0,        1064.394,   530.836 },
                    { 0,        0,          1 },
                }),
                Vector<double>.Build.DenseOfArray(new double[] { 0.068854329271834366, -0.064609483770925957 }),
                Vector<double>.Build.DenseOfArray(new double[] { -0.00051222793642456373, 0.0048256000710909233 }));

            this.DepthIntrinsics = new CameraIntrinsics(
                512,
                424,
                Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { 366.615, 0, 262.899 },
                    { 0, 366.641, 212.625 },
                    { 0, 0, 1 },
                }),
                Vector<double>.Build.DenseOfArray(new double[] { 0.0599477, -0.165643 }));

            this.DepthToColorTransform = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { 1, 0.00056841, 0.000158119, 0.0523835 },
                { -0.000568614, 0.999999, 0.00129824, -0.000142453 },
                { -0.000157381, -0.00129833, 0.999999, 2.00825E-05 },
                { 0, 0, 0, 1 },
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectCalibration"/> class.
        /// </summary>
        /// <param name="colorWidth">Width of color camera in pixels.</param>
        /// <param name="colorHeight">Height of color camera in pixels.</param>
        /// <param name="colorTransform">Transform to be applied to color image.</param>
        /// <param name="colorRadial0">1st Radial distortion parameter.</param>
        /// <param name="colorRadial1">2nd Radial distortion parameter.</param>
        /// <param name="colorTangential0">1st Tangential distortion parameter.</param>
        /// <param name="colorTangential1">2nd Tangential distortion parameter.</param>
        /// <param name="depthToColorTransform">Transform from depth to color camera.</param>
        /// <param name="depthWidth">Width of depth image in pixels.</param>
        /// <param name="depthHeight">Height of depth image in pixels.</param>
        /// <param name="depthTransform">Transform to be applied to depth image.</param>
        /// <param name="depthRadial0">1st Radial distortion parameter for depth image.</param>
        /// <param name="depthRadial1">2nd Radial distortion parameter for depth image.</param>
        /// <param name="depthTangential0">1st Tangential distortion parameter for depth image.</param>
        /// <param name="depthTangential1">2nd Tangential distortion parameter for depth image.</param>
        public KinectCalibration(
            int colorWidth,
            int colorHeight,
            Matrix<double> colorTransform,
            double colorRadial0,
            double colorRadial1,
            double colorTangential0,
            double colorTangential1,
            Matrix<double> depthToColorTransform,
            int depthWidth,
            int depthHeight,
            Matrix<double> depthTransform,
            double depthRadial0,
            double depthRadial1,
            double depthTangential0,
            double depthTangential1)
        {
            this.ColorIntrinsics = new CameraIntrinsics(
                colorWidth,
                colorHeight,
                colorTransform.DeepClone(),
                Vector<double>.Build.DenseOfArray(new double[] { colorRadial0, colorRadial1 }),
                Vector<double>.Build.DenseOfArray(new double[] { colorTangential0, colorTangential1 }));

            this.ColorExtrinsics = new CoordinateSystem(depthToColorTransform);

            this.DepthIntrinsics = new CameraIntrinsics(
                depthWidth,
                depthHeight,
                depthTransform.DeepClone(),
                Vector<double>.Build.DenseOfArray(new double[] { depthRadial0, depthRadial1 }),
                Vector<double>.Build.DenseOfArray(new double[] { depthTangential0, depthTangential1 }));

            this.DepthExtrinsics = new CoordinateSystem();
        }

        /// <inheritdoc/>
        public CoordinateSystem ColorExtrinsics { get; }

        /// <inheritdoc/>
        public ICameraIntrinsics ColorIntrinsics { get; }

        /// <inheritdoc/>
        public CoordinateSystem DepthExtrinsics { get; }

        /// <inheritdoc/>
        public ICameraIntrinsics DepthIntrinsics { get; }

        /// <summary>
        /// Gets depth to color transform.
        /// </summary>
        public Matrix<double> DepthToColorTransform { get; }

        /// <inheritdoc/>
        public Point2D ToColorSpace(Point3D point3D)
        {
            var point3DInColorCamera = this.ColorExtrinsics.Transform(point3D);
            return this.ColorIntrinsics.ToPixelSpace(point3DInColorCamera, true);
        }
    }
}
