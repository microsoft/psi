// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// KinectCalibration holds the intrinsics and extrinsics for a Kinect camera
    /// </summary>
    public class KinectCalibration : IKinectCalibration
    {
        /// <summary>
        /// The default calibration
        /// </summary>
        public static readonly KinectCalibration Default = new KinectCalibration();

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectCalibration"/> class.
        /// </summary>
        public KinectCalibration()
        {
            this.ColorIntrinsics = new Microsoft.Psi.Calibration.CameraIntrinsics();
            this.ColorIntrinsics.ImageWidth = 1920;
            this.ColorIntrinsics.ImageHeight = 1280;
            this.ColorIntrinsics.Transform = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { 1064.8, 0, 960.384 },
                    { 0, 1064.72, 540.902 },
                    { 0, 0, 1 }
                });
            this.ColorIntrinsics.RadialDistortion = Vector<double>.Build.DenseOfArray(new double[]
                { 0.0148094, -0.00237727 });

            this.DepthIntrinsics = new Microsoft.Psi.Calibration.CameraIntrinsics();
            this.DepthIntrinsics.ImageWidth = 512;
            this.DepthIntrinsics.ImageHeight = 424;
            this.DepthIntrinsics.Transform = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { 366.615, 0, 262.899 },
                    { 0, 366.641, 212.625 },
                    { 0, 0, 1 }
                });
            this.DepthIntrinsics.RadialDistortion = Vector<double>.Build.DenseOfArray(new double[]
                { 0.0599477, -0.165643 });
            this.DepthToColorTransform = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { 1, 0.00056841, 0.000158119, 0.0523835 },
                    { -0.000568614, 0.999999, 0.00129824, -0.000142453 },
                    { -0.000157381, -0.00129833, 0.999999, 2.00825E-05 },
                    { 0, 0, 0, 1 }
                });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectCalibration"/> class.
        /// </summary>
        /// <param name="colorWidth">Width of color camera in pixels</param>
        /// <param name="colorHeight">Height of color camera in pixels</param>
        /// <param name="colorTransform">Transform to be applied to color image</param>
        /// <param name="colorRadial0">1st Radial distortion parameter</param>
        /// <param name="colorRadial1">2nd Radial distortion parameter</param>
        /// <param name="colorTangential0">1st Tangential distortion parameter</param>
        /// <param name="colorTangential1">2nd Tangential distortion parameter</param>
        /// <param name="depthToColorTransform">Transform from depth to color camera</param>
        /// <param name="depthWidth">Width of depth image in pixels</param>
        /// <param name="depthHeight">Height of depth image in pixels</param>
        /// <param name="depthTransform">Transform to be applied to depth image</param>
        /// <param name="depthRadial0">1st Radial distortion parameter for depth image</param>
        /// <param name="depthRadial1">2nd Radial distortion parameter for depth image</param>
        /// <param name="depthTangential0">1st Tangential distortion parameter for depth image</param>
        /// <param name="depthTangential1">2nd Tangential distortion parameter for depth image</param>
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
            this.ColorIntrinsics = new Microsoft.Psi.Calibration.CameraIntrinsics();
            this.ColorIntrinsics.ImageWidth = colorWidth;
            this.ColorIntrinsics.ImageHeight = colorHeight;
            this.ColorIntrinsics.Transform = colorTransform.DeepClone();
            this.ColorIntrinsics.RadialDistortion[0] = colorRadial0;
            this.ColorIntrinsics.RadialDistortion[1] = colorRadial1;
            this.ColorIntrinsics.TangentialDistortion[0] = colorTangential0;
            this.ColorIntrinsics.TangentialDistortion[1] = colorTangential1;
            this.ColorExtrinsics = new CoordinateSystem(depthToColorTransform);

            this.DepthIntrinsics = new Microsoft.Psi.Calibration.CameraIntrinsics();
            this.DepthIntrinsics.ImageWidth = depthWidth;
            this.DepthIntrinsics.ImageHeight = depthHeight;
            this.DepthIntrinsics.Transform = depthTransform.DeepClone();
            this.DepthIntrinsics.RadialDistortion[0] = depthRadial0;
            this.DepthIntrinsics.RadialDistortion[1] = depthRadial1;
            this.DepthIntrinsics.TangentialDistortion[0] = depthTangential0;
            this.DepthIntrinsics.TangentialDistortion[1] = depthTangential1;
            this.DepthExtrinsics = new CoordinateSystem();
        }

        /// <summary>
        /// Gets or sets the color camera's extrinsics
        /// </summary>
        public CoordinateSystem ColorExtrinsics { get; set; }

        /// <summary>
        /// Gets or sets the color camera's intrinsics
        /// </summary>
        public Microsoft.Psi.Calibration.ICameraIntrinsics ColorIntrinsics { get; set; }

        /// <summary>
        /// Gets or sets the depth camera's extrinsics
        /// </summary>
        public CoordinateSystem DepthExtrinsics { get; set; }

        /// <summary>
        /// Gets or sets the depth camera's intrinsics
        /// </summary>
        public Microsoft.Psi.Calibration.ICameraIntrinsics DepthIntrinsics { get; set; }

        /// <summary>
        /// Gets or sets the transform to go from the depth camera to the color camera
        /// </summary>
        public Matrix<double> DepthToColorTransform { get; set; }
    }
}
