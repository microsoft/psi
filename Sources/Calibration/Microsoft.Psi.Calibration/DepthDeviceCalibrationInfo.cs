// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Defines the calibration information (intrinsics and extrinsics of color and depth cameras) for a depth device.
    /// </summary>
    public class DepthDeviceCalibrationInfo : IDepthDeviceCalibrationInfo
    {
        /// <summary>
        /// The default calibration info.
        /// </summary>
        public static readonly DepthDeviceCalibrationInfo Default = new DepthDeviceCalibrationInfo();

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthDeviceCalibrationInfo"/> class.
        /// </summary>
        public DepthDeviceCalibrationInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DepthDeviceCalibrationInfo"/> class.
        /// </summary>
        /// <param name="colorWidth">Width of color camera in pixels.</param>
        /// <param name="colorHeight">Height of color camera in pixels.</param>
        /// <param name="colorTransform">Transform to be applied to color image.</param>
        /// <param name="colorRadialDistortionCoefficients">Color sensor's radial distortion coefficients (k1-k6).</param>
        /// <param name="colorTangentialDistortionCoefficients">Color sensor's tangential distortion coefficients (p1-p2).</param>
        /// <param name="depthToColorTransform">Transform from depth to color camera.</param>
        /// <param name="depthWidth">Width of depth image in pixels.</param>
        /// <param name="depthHeight">Height of depth image in pixels.</param>
        /// <param name="depthTransform">Transform to be applied to depth image.</param>
        /// <param name="depthRadialDistortionCoefficients">Depth sensor's radial distortion coefficients (k1-k6).</param>
        /// <param name="depthTangentialDistortionCoefficients">Depth sensor's tangential distortion coefficients (p1-p2).</param>
        /// <param name="depthExtrinsics">Depth extrinsics transform.</param>
        public DepthDeviceCalibrationInfo(
            int colorWidth,
            int colorHeight,
            Matrix<double> colorTransform,
            double[] colorRadialDistortionCoefficients,
            double[] colorTangentialDistortionCoefficients,
            Matrix<double> depthToColorTransform,
            int depthWidth,
            int depthHeight,
            Matrix<double> depthTransform,
            double[] depthRadialDistortionCoefficients,
            double[] depthTangentialDistortionCoefficients,
            Matrix<double> depthExtrinsics)
        {
            this.ColorIntrinsics = new CameraIntrinsics(
                colorWidth,
                colorHeight,
                colorTransform.DeepClone(),
                Vector<double>.Build.DenseOfArray(colorRadialDistortionCoefficients),
                Vector<double>.Build.DenseOfArray(colorTangentialDistortionCoefficients));

            this.ColorExtrinsics = new CoordinateSystem(depthToColorTransform);

            this.DepthIntrinsics = new CameraIntrinsics(
                depthWidth,
                depthHeight,
                depthTransform.DeepClone(),
                Vector<double>.Build.DenseOfArray(depthRadialDistortionCoefficients),
                Vector<double>.Build.DenseOfArray(depthTangentialDistortionCoefficients));

            this.DepthExtrinsics = new CoordinateSystem(depthExtrinsics);
        }

        /// <inheritdoc/>
        public CoordinateSystem ColorExtrinsics { get; }

        /// <inheritdoc/>
        public ICameraIntrinsics ColorIntrinsics { get; }

        /// <inheritdoc/>
        public CoordinateSystem DepthExtrinsics { get; }

        /// <inheritdoc/>
        public ICameraIntrinsics DepthIntrinsics { get; }

        /// <inheritdoc/>
        public Point2D ToColorSpace(Point3D point3D)
        {
            var point3DInColorCamera = this.ColorExtrinsics.Transform(point3D);
            return this.ColorIntrinsics.ToPixelSpace(point3DInColorCamera, true);
        }
    }
}
