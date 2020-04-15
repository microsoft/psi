// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// ICameraIntrinsics defines our interface for specifying the intrinsics
    /// for a camera (i.e. converting from camera space coordinates into pixel
    /// coordinates).
    /// </summary>
    public interface ICameraIntrinsics
    {
        /// <summary>
        /// Gets the intrinsics matrix. This transform converts camera coordinates (in the camera's local space) into
        /// normalized device coordinates (NDC) ranging from -1 .. +1.
        /// </summary>
        Matrix<double> Transform { get; }

        /// <summary>
        /// Gets the inverse of instrinsics matrix.
        /// </summary>
        Matrix<double> InvTransform { get; }

        /// <summary>
        /// Gets the radial distortion parameters.
        /// </summary>
        Vector<double> RadialDistortion { get; }

        /// <summary>
        /// Gets the tangential distortion parameters.
        /// </summary>
        Vector<double> TangentialDistortion { get; }

        /// <summary>
        /// Gets the focal length (in pixels).
        /// </summary>
        double FocalLength { get; }

        /// <summary>
        /// Gets or the focal length separated in X and Y (in pixels).
        /// </summary>
        Point2D FocalLengthXY { get; }

        /// <summary>
        /// Gets the principal point (in pixels).
        /// </summary>
        Point2D PrincipalPoint { get; }

        /// <summary>
        /// Gets the width of the camera's image (in pixels).
        /// </summary>
        int ImageWidth { get; }

        /// <summary>
        /// Gets the height of the camera's image (in pixels).
        /// </summary>
        int ImageHeight { get; }

        /// <summary>
        /// Projects a 3D point into the pixel space.
        /// </summary>
        /// <param name="point3D">Point in 3D space, assuming MathNet basis (Forward=X, Left=Y, Up=Z).</param>
        /// <param name="distort">Indicates whether to apply distortion.</param>
        /// <returns>Point in pixel space.</returns>
        Point2D ToPixelSpace(Point3D point3D, bool distort);

        /// <summary>
        /// Unprojects a point from pixel space into 3D space.
        /// </summary>
        /// <param name="point2D">Point in pixel space.</param>
        /// <param name="depth">Depth at pixel.</param>
        /// <param name="undistort">Indicates whether to apply undistortion.</param>
        /// <returns>Point in 3D space, assuming MathNet basis (Forward=X, Left=Y, Up=Z).</returns>
        Point3D ToCameraSpace(Point2D point2D, double depth, bool undistort);

        /// <summary>
        /// Applies the distortion model to a point in the camera post-projection coordinates.
        ///
        /// The undistortion is defined by the following equations:
        ///   Xu = Xd (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xd^2) + T0 * 2 * XdYd
        ///   Yu = Yd (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yd^2) + T1 * 2 * XdYd
        /// where:
        ///    Xd,Yd - distorted point's coordinates
        ///    Xu,Yu - undistorted point's coordinates
        ///    K0,K1 - radial distortion coefficients
        ///    T0,T1 - tangential distortion coefficients
        ///    r - sqrt(Xd*Xd+Yd*Yd)
        ///
        /// Newton's method is used to find the inverse of this. That is
        ///             Xd(n+1) = Xd(n) + J^-1 * F(Xd,Yd).
        /// </summary>
        /// <param name="undistortedPoint">The undistorted point in camera post-projection coordinates.</param>
        /// <param name="distortedPoint">The distorted point.</param>
        /// <returns>True if 'distortedPoint' contains the distorted point, or false if the algorithm did not converge.</returns>
        bool DistortPoint(Point2D undistortedPoint, out Point2D distortedPoint);

        /// <summary>
        /// Applies the camera's radial and tangential undistortion to the specified (distorted) point.
        ///
        /// The undistortion is defined by the following equations:
        ///   Xu = Xd (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xd^2) + T0 * 2 * XdYd
        ///   Yu = Yd (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yd^2) + T1 * 2 * XdYd
        /// where:
        ///    Xd,Yd - distorted point's coordinates
        ///    Xu,Yu - undistorted point's coordinates
        ///    K0,K1 - radial distortion coefficients
        ///    T0,T1 - tangential distortion coefficients.
        ///
        /// </summary>
        /// <param name="distortedPoint">Distorted point in camera post-projection coordinates.</param>
        /// <returns>Undistorted coordinates in camera post-projection coordinates.</returns>
        Point2D UndistortPoint(Point2D distortedPoint);
    }
}
