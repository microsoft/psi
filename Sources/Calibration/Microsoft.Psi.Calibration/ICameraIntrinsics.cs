// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// ICameraIntrinsics defines our interface for specifying the intrinsics
    /// for a camera (i.e. converting from camera space coordinates into pixel
    /// coordinates).
    /// </summary>
    public interface ICameraIntrinsics : IEquatable<ICameraIntrinsics>
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
        /// Gets a value indicating whether the closed form equation of the Brown-Conrady Distortion model
        /// distorts or undistorts. i.e. if true then:
        ///      Xdistorted = Xundistorted * (1+K1*R2+K2*R3+...
        /// otherwise:
        ///      Xundistorted = Xdistorted * (1+K1*R2+K2*R3+...
        /// </summary>
        bool ClosedFormDistorts { get; }

        /// <summary>
        /// Gets the width of the camera's image (in pixels).
        /// </summary>
        int ImageWidth { get; }

        /// <summary>
        /// Gets the height of the camera's image (in pixels).
        /// </summary>
        int ImageHeight { get; }

        /// <summary>
        /// Gets the corresponding pixel position for a point in 3D space.
        /// </summary>
        /// <param name="point3D">Point in 3D space, assuming MathNet basis (Forward=X, Left=Y, Up=Z).</param>
        /// <param name="distort">Indicates whether to apply distortion.</param>
        /// <param name="nullIfOutsideFieldOfView">Optional flag indicating whether to return null if point is outside the field of view (default true).</param>
        /// <returns>Point containing the pixel position.</returns>
        /// <remarks>Points that are behind the camera, i.e., with the X value below zero lead to null returns,
        /// regardless of value of the <paramref name="nullIfOutsideFieldOfView"/> parameter.</remarks>
        Point2D? GetPixelPosition(Point3D point3D, bool distort, bool nullIfOutsideFieldOfView = true);

        /// <summary>
        /// Gets the corresponding pixel position for a point in 3D space.
        /// </summary>
        /// <param name="point3D">Point in 3D space, assuming MathNet basis (Forward=X, Left=Y, Up=Z).</param>
        /// <param name="distort">Indicates whether to apply distortion.</param>
        /// <param name="pixelPosition">Output point containing the pixel position.</param>
        /// <param name="nullIfOutsideFieldOfView">Optional flag indicating whether to return null if point is outside the field of view (default true).</param>
        /// <returns>True if <paramref name="pixelPosition"/> is within field of view, otherwise false.</returns>
        /// <remarks>Points that are behind the camera, i.e., with the X value below zero lead to a return value of false,
        /// regardless of value of the <paramref name="nullIfOutsideFieldOfView"/> parameter.</remarks>
        bool TryGetPixelPosition(Point3D point3D, bool distort, out Point2D pixelPosition, bool nullIfOutsideFieldOfView = true);

        /// <summary>
        /// Gets the corresponding 3D camera space position at a given depth along a specified pixel.
        /// </summary>
        /// <param name="point2D">The pixel position.</param>
        /// <param name="depth">The depth along the specified pixel position.</param>
        /// <param name="depthValueSemantics">How depth values should be interpreted.</param>
        /// <param name="undistort">Indicates whether to apply undistortion.</param>
        /// <returns>Point in 3D space, assuming MathNet basis (Forward=X, Left=Y, Up=Z).</returns>
        Point3D GetCameraSpacePosition(Point2D point2D, double depth, DepthValueSemantics depthValueSemantics, bool undistort);

        /// <summary>
        /// Gets a mapping matrix that can be used to transform pixels into 3D space.
        /// </summary>
        /// <param name="depthValueSemantics">How depth values should be interpreted.</param>
        /// <param name="undistort">Indicates whether to apply undistortion.</param>
        /// <returns>
        /// A matrix of 3D points that can be used to transform depth values at a specified pixel
        /// into 3D space. To use this matrix simply piecewise multiply the depth value by the X
        /// Y and Z dimensions of the <see cref="Point3D"/> in the matrix at the location indexed
        /// by the pixel.</returns>
        Point3D[,] GetPixelToCameraSpaceMapping(DepthValueSemantics depthValueSemantics, bool undistort);

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
        /// <returns>True if <paramref name="distortedPoint"></paramref> contains the distorted point, or false if the algorithm did not converge.</returns>
        bool TryDistortPoint(Point2D undistortedPoint, out Point2D distortedPoint);

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
        /// <param name="undistortedPoint">Returns the undistorted point in camera post-projection coordinates.</param>
        /// <returns>True if <paramref name="undistortedPoint"/> contains the undistorted point, or false if the algorithm did not converge.</returns>
        bool TryUndistortPoint(Point2D distortedPoint, out Point2D undistortedPoint);
    }
}
