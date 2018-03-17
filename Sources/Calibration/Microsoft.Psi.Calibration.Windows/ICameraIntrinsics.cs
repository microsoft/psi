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
        /// Gets or sets the Instrinsic matrix
        /// </summary>
        Matrix<double> Transform { get; set; }

        /// <summary>
        /// Gets the inverse of Instrinsic matrix
        /// </summary>
        Matrix<double> InvTransform { get; }

        /// <summary>
        /// Gets or sets the radial distortion parameters
        /// </summary>
        Vector<double> RadialDistortion { get; set; }

        /// <summary>
        /// Gets or sets the tangential distortion parameters
        /// </summary>
        Vector<double> TangentialDistortion { get; set; }

        /// <summary>
        /// Gets or sets the focal length
        /// </summary>
        double FocalLength { get; set; }

        /// <summary>
        /// Gets or sets the principal point
        /// </summary>
        Point2D PrincipalPoint { get; set; }

        /// <summary>
        /// Gets or sets width of the camera's image in pixels
        /// </summary>
        int ImageWidth { get; set; }

        /// <summary>
        /// Gets or sets height of the camera's image in pixels
        /// </summary>
        int ImageHeight { get; set; }

        /// <summary>
        /// Projects a 3D point into the camera
        /// </summary>
        /// <param name="pt">Point in camera space</param>
        /// <param name="distort">If true then distortion is applied</param>
        /// <returns>Point in pixel space</returns>
        Point2D ToPixelSpace(Point3D pt, bool distort);

        /// <summary>
        /// Unprojects a point from pixel space into 3D camera space
        /// </summary>
        /// <param name="pt">Point in pixel space</param>
        /// <param name="depth">Depth at pixel</param>
        /// <param name="undistort">If true then undistortion is applied</param>
        /// <returns>Point in camera space</returns>
        Point3D ToCameraSpace(Point2D pt, double depth, bool undistort);

        /// <summary>
        /// Applies distortion to the point in pixel space
        /// </summary>
        /// <param name="undistortedPt">Point in pixel space to distort</param>
        /// <returns>Distorted point in pixel space</returns>
        Point2D DistortPoint(Point2D undistortedPt);

        /// <summary>
        /// Applies undistortion to the point in pixel space
        /// </summary>
        /// <param name="distortedPt">Distorted point in pixel space to undistort</param>
        /// <returns>Undistorted point in pixel space</returns>
        Point2D UndistortPoint(Point2D distortedPt);
    }
}
