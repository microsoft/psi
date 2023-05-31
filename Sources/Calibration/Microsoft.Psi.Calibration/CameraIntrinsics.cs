// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using System.Runtime.Serialization;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// CameraIntrinsics defines the intrinsic properties for a given camera.
    /// </summary>
    public class CameraIntrinsics : ICameraIntrinsics, IEquatable<CameraIntrinsics>
    {
        private Matrix<double> transform;

        [OptionalField]
        private bool closedFormDistorts;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraIntrinsics"/> class.
        /// </summary>
        /// <param name="imageWidth">The width of the image.</param>
        /// <param name="imageHeight">The height of the image.</param>
        /// <param name="transform">The 3x3 intrinsics transform matrix.</param>
        /// <param name="radialDistortion">The radial distortion parameters (up to 6).</param>
        /// <param name="tangentialDistortion">The tangential distortion parameters (up to 2).</param>
        /// <param name="closedFormDistorts">Indicates which direction the closed form equation for Brown-Conrady Distortion model goes. I.e. does it perform distortion or undistortion. Default is to distort (thus making projection simpler and unprojection more complicated).</param>
        public CameraIntrinsics(
            int imageWidth,
            int imageHeight,
            Matrix<double> transform,
            Vector<double> radialDistortion = null,
            Vector<double> tangentialDistortion = null,
            bool closedFormDistorts = true)
        {
            this.ImageWidth = imageWidth;
            this.ImageHeight = imageHeight;

            if (transform == null || transform.RowCount != 3 || transform.ColumnCount != 3)
            {
                throw new ArgumentException($"{nameof(CameraIntrinsics)} must be initialized with a 3x3 transform matrix.");
            }

            this.Transform = transform;
            this.RadialDistortion = Vector<double>.Build.Dense(6, 0);
            if (radialDistortion != null)
            {
                radialDistortion.CopySubVectorTo(this.RadialDistortion, 0, 0, Math.Min(6, radialDistortion.Count));
            }

            this.TangentialDistortion = Vector<double>.Build.Dense(2, 0);
            if (tangentialDistortion != null)
            {
                tangentialDistortion.CopySubVectorTo(this.TangentialDistortion, 0, 0, Math.Min(2, tangentialDistortion.Count));
            }

            this.FocalLengthXY = new Point2D(this.Transform[0, 0], this.Transform[1, 1]);
            this.PrincipalPoint = new Point2D(this.Transform[0, 2], this.Transform[1, 2]);
            this.ClosedFormDistorts = closedFormDistorts;
        }

        /// <inheritdoc/>
        public Vector<double> RadialDistortion { get; private set; }

        /// <inheritdoc/>
        public Vector<double> TangentialDistortion { get; private set; }

        /// <inheritdoc/>
        public Matrix<double> Transform
        {
            get
            {
                return this.transform;
            }

            private set
            {
                this.transform = value;
                this.InvTransform = this.transform.Inverse();
            }
        }

        /// <inheritdoc/>
        public Matrix<double> InvTransform { get; private set; }

        /// <inheritdoc/>
        public double FocalLength => 0.5 * (this.FocalLengthXY.X + this.FocalLengthXY.Y);

        /// <inheritdoc/>
        public Point2D FocalLengthXY { get; private set; }

        /// <inheritdoc/>
        public Point2D PrincipalPoint { get; private set; }

        /// <inheritdoc/>
        public bool ClosedFormDistorts
        {
            get
            {
                return this.closedFormDistorts;
            }

            private set
            {
                this.closedFormDistorts = value;
            }
        }

        /// <inheritdoc/>
        public int ImageWidth { get; private set; }

        /// <inheritdoc/>
        public int ImageHeight { get; private set; }

        /// <summary>
        /// Returns a value indicating whether the specified camera intrinsics are the same.
        /// </summary>
        /// <param name="left">The first camera intrinsics.</param>
        /// <param name="right">The second camera intrinsics.</param>
        /// <returns>True if the camera intrinsics are the same; otherwise false.</returns>
        public static bool operator ==(CameraIntrinsics left, CameraIntrinsics right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns a value indicating whether the specified camera intrinsics are different.
        /// </summary>
        /// <param name="left">The first camera intrinsics.</param>
        /// <param name="right">The second camera intrinsics.</param>
        /// <returns>True if camera intrinsics are different; otherwise false.</returns>
        public static bool operator !=(CameraIntrinsics left, CameraIntrinsics right)
        {
            return !left.Equals(right);
        }

        /// <inheritdoc/>
        public Point2D? GetPixelPosition(Point3D point3D, bool distort, bool nullIfOutsideFieldOfView = true)
        {
            // X points in the depth dimension. Y points to the left, and Z points up.

            // If the point is not in front of the camera, we cannot compute the projection
            if (point3D.X <= 0)
            {
                return null;
            }

            var point2D = new Point2D(-point3D.Y / point3D.X, -point3D.Z / point3D.X);
            if (distort)
            {
                this.TryDistortPoint(point2D, out point2D);
            }

            var tmp = new Point3D(point2D.X, point2D.Y, 1.0);
            tmp = tmp.TransformBy(this.transform);

            if (nullIfOutsideFieldOfView && (tmp.X < 0 || this.ImageWidth <= tmp.X || tmp.Y < 0 || this.ImageHeight <= tmp.Y))
            {
                return null;
            }

            return new Point2D(tmp.X, tmp.Y);
        }

        /// <inheritdoc />
        public bool TryGetPixelPosition(Point3D point3D, bool distort, out Point2D pixelPosition, bool nullIfOutsideFieldOfView = true)
        {
            var point2D = this.GetPixelPosition(point3D, distort, nullIfOutsideFieldOfView);
            pixelPosition = point2D ?? default;
            return point2D.HasValue;
        }

        /// <inheritdoc/>
        public Point3D GetCameraSpacePosition(Point2D point2D, double depth, DepthValueSemantics depthValueSemantics, bool undistort)
        {
            // Convert from pixel coordinates to NDC
            var tmp = new Point3D(point2D.X, point2D.Y, 1.0);
            tmp = tmp.TransformBy(this.InvTransform);

            // Distort the pixel
            var pixelPoint2D = new Point2D(tmp.X, tmp.Y);
            if (undistort)
            {
                this.TryUndistortPoint(pixelPoint2D, out pixelPoint2D);
            }

            if (depthValueSemantics == DepthValueSemantics.DistanceToPoint)
            {
                double norm = Math.Sqrt(pixelPoint2D.X * pixelPoint2D.X + pixelPoint2D.Y * pixelPoint2D.Y + 1);
                depth /= norm;
            }

            // X points in the depth dimension. Y points to the left, and Z points up.
            return new Point3D(depth, -pixelPoint2D.X * depth, -pixelPoint2D.Y * depth);
        }

        /// <inheritdoc/>
        public bool TryUndistortPoint(Point2D distortedPt, out Point2D undistortedPt)
        {
            if (this.ClosedFormDistorts)
            {
                return this.InverseOfClosedForm(distortedPt, out undistortedPt);
            }

            return this.ClosedForm(distortedPt, out undistortedPt);
        }

        /// <inheritdoc/>
        public bool TryDistortPoint(Point2D undistortedPt, out Point2D distortedPt)
        {
            if (this.ClosedFormDistorts)
            {
                return this.ClosedForm(undistortedPt, out distortedPt);
            }

            return this.InverseOfClosedForm(undistortedPt, out distortedPt);
        }

        /// <inheritdoc/>
        public Point3D[,] GetPixelToCameraSpaceMapping(DepthValueSemantics depthValueSemantics, bool undistort)
        {
            var result = new Point3D[this.ImageWidth, this.ImageHeight];
            for (int i = 0; i < this.ImageWidth; i++)
            {
                for (int j = 0; j < this.ImageHeight; j++)
                {
                    // Convert from pixel coordinates to NDC
                    var tmp = new Point3D(i, j, 1.0);
                    tmp = tmp.TransformBy(this.InvTransform);

                    // Distort the pixel
                    var pixelPoint2D = new Point2D(tmp.X, tmp.Y);
                    if (undistort)
                    {
                        this.TryUndistortPoint(pixelPoint2D, out pixelPoint2D);
                    }

                    if (depthValueSemantics == DepthValueSemantics.DistanceToPoint)
                    {
                        double norm = Math.Sqrt(pixelPoint2D.X * pixelPoint2D.X + pixelPoint2D.Y * pixelPoint2D.Y + 1);
                        result[i, j] = new Point3D(1 / norm, -pixelPoint2D.X / norm, -pixelPoint2D.Y / norm);
                    }
                    else
                    {
                        result[i, j] = new Point3D(1, -pixelPoint2D.X, -pixelPoint2D.Y);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = default(HashCode);
            hashCode.Add(this.ClosedFormDistorts);
            hashCode.Add(this.FocalLengthXY);
            hashCode.Add(this.ImageHeight);
            hashCode.Add(this.ImageWidth);
            hashCode.Add(this.PrincipalPoint);
            hashCode.Add(this.RadialDistortion);
            hashCode.Add(this.TangentialDistortion);
            hashCode.Add(this.Transform);
            return hashCode.ToHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is CameraIntrinsics other && this.Equals(other);

        /// <inheritdoc/>
        public bool Equals(ICameraIntrinsics other) =>
            other is CameraIntrinsics cameraIntrinsics &&
            Equals(this.ClosedFormDistorts, cameraIntrinsics.ClosedFormDistorts) &&
            Equals(this.FocalLengthXY, cameraIntrinsics.FocalLengthXY) &&
            Equals(this.ImageHeight, cameraIntrinsics.ImageHeight) &&
            Equals(this.ImageWidth, cameraIntrinsics.ImageWidth) &&
            Equals(this.PrincipalPoint, cameraIntrinsics.PrincipalPoint) &&
            Equals(this.RadialDistortion, cameraIntrinsics.RadialDistortion) &&
            Equals(this.TangentialDistortion, cameraIntrinsics.TangentialDistortion) &&
            Equals(this.Transform, cameraIntrinsics.Transform);

        /// <inheritdoc/>
        public bool Equals(CameraIntrinsics other) => this.Equals((ICameraIntrinsics)other);

        private bool InverseOfClosedForm(Point2D inputPt, out Point2D outputPt)
        {
            double k1 = this.RadialDistortion[0];
            double k2 = this.RadialDistortion[1];
            double k3 = this.RadialDistortion[2];
            double k4 = this.RadialDistortion[3];
            double k5 = this.RadialDistortion[4];
            double k6 = this.RadialDistortion[5];
            double t0 = this.TangentialDistortion[0];
            double t1 = this.TangentialDistortion[1];

            double x = inputPt.X;
            double y = inputPt.Y;

            // Our distortion model is defined as:
            // See https://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html?highlight=convertpointshomogeneous
            //        r^2 = x^2 + y^2
            //               (1+k1*r^2+k2*r^4+k3^r^6)
            //        Fx = x ------------------------ + t1*(r^2+ 2 * x^2) + 2 * t0 * x*y
            //               (1+k4*r^2+k5*r^4+k6^r^6)
            //
            //               (1+k1*r^2+k2*r^4+k3^r^6)
            //        Fy = y ------------------------ + t0*(r^2+ 2 * y^2) + 2 * t1 * x*y
            //               (1+k4*r^2+k5*r^4+k6^r^6)
            //
            // We want to solve for:
            //                          1                            | @Fy/@y   -@Fx/@y |
            //    J(F(x))^-1 =  ------------------------------------ |                  |
            //                   @Fx/@x * @Fy/dy - @Fy/@x * @Fx/@y   | -@Fy/@x  @Fx/@x  |
            // where ("@y/@x" is used to represent the partial derivative of y with respect to x):
            //
            //    g = 1 + k1 * r^2 + k2 * r^4 + k3 * r^6
            //    h = 1 + k4 * r^2 + k5 * r^4 + k6 * r^6
            //    d = g / h
            //    @r^2/@x = 2x
            //    @r^2/@y = 2y
            //    @g/r^2 = k1 + 2*k2*r^2 + 3*k3*r^4
            //    @h/r^2 = k4 + 2*k5*r^2 + 3*k6*r^4
            //    @d/@x = @d/@r^2 * @r^2/@x = @d/@r^2 * 2*x
            //    @d/@y = @d/@r^2 * @r^2/@y = @d/@r^2 * 2*y
            //    @Fx/@x = x @d/@x + d + 2*t0*y + 6*t1*x
            //    @Fy/@y = y @d/@y + d + 2*t0*x + 6*t1*y
            //    @Fx/@y = x @d/@y + 2*t0*x + 2*t1*y
            //    @Fy/@x = y @d/@x + 2*t0*y + 2*t1*x
            //
            // In the code below @<x>/@<y> is named 'd<x>d<y>'.
#pragma warning disable SA1305
            bool converged = false;
            for (int j = 0; j < 100 && !converged; j++)
            {
                double distortedRadius = (x * x) + (y * y);
                double radiusSq = distortedRadius;
                double radiusSqSq = radiusSq * radiusSq;
                double g = 1 + k1 * radiusSq + k2 * radiusSqSq + k3 * radiusSq * radiusSqSq;
                double h = 1 + k4 * radiusSq + k5 * radiusSqSq + k6 * radiusSq * radiusSqSq;
                double d = g / h;
                double dgdr2 = k1 + 2 * k2 * radiusSq + 3 * k3 * radiusSqSq;
                double dhdr2 = k4 + 2 * k5 * radiusSq + 3 * k6 * radiusSqSq;
                double dddr2 = (dgdr2 * h - g * dhdr2) / (h * h);
                double dddx = dddr2 * 2 * x;
                double dddy = dddr2 * 2 * y;
                double dFxdx = x * dddx + d + 2 * t0 * y + 6 * t1 * x;
                double dFxdy = x * dddy + 2 * t0 * x + 2 * t1 * y;
                double dFydx = y * dddx + 2 * t1 * y + 2 * t0 * x;
                double dFydy = y * dddy + d + 2 * t1 * x + 6 * t0 * y;

                double det = (dFxdx * dFydy) - dFydx * dFxdy;

                if (Math.Abs(det) < 1E-16)
                {
                    // Not invertible. Perform no distortion
                    outputPt = new Point2D(inputPt.X, inputPt.Y);
                    return false;
                }

                // Compute the undisortion of our estimated distorted point.
                double xy = 2.0 * x * y;
                double x2 = 2.0 * x * x;
                double y2 = 2.0 * y * y;
                double xp = (x * d) + (t1 * (radiusSq + x2)) + (t0 * xy);
                double yp = (y * d) + (t0 * (radiusSq + y2)) + (t1 * xy);

                // We need the difference between our undistorted point
                // and the undistortion of our estimated distorted point
                // to be equal to 0:
                //       0 = F(xp) - Xu
                //       0 = F(yp) - Yu
                double errx = xp - inputPt.X;
                double erry = yp - inputPt.Y;

                double err = (errx * errx) + (erry * erry);
                if (err < 1.0e-16)
                {
                    converged = true;
                    break;
                }

                // Update our new guess (i.e. x = x - J(F(x))^-1 * F(x))
                x -= ((dFydy * errx) - (dFxdy * erry)) / det;
                y -= ((-dFydx * errx) + (dFxdx * erry)) / det;

#pragma warning restore SA1305
            }

            if (converged)
            {
                outputPt = new Point2D(x, y);
            }
            else
            {
                outputPt = new Point2D(inputPt.X, inputPt.Y);
            }

            return converged;
        }

        private bool ClosedForm(Point2D inputPt, out Point2D outputPt)
        {
            // Undistort pixel
            double xp, yp;
            double radiusSquared = (inputPt.X * inputPt.X) + (inputPt.Y * inputPt.Y);

            double k1 = this.RadialDistortion[0];
            double k2 = this.RadialDistortion[1];
            double k3 = this.RadialDistortion[2];
            double k4 = this.RadialDistortion[3];
            double k5 = this.RadialDistortion[4];
            double k6 = this.RadialDistortion[5];
            double g = 1 + k1 * radiusSquared + k2 * radiusSquared * radiusSquared + k3 * radiusSquared * radiusSquared * radiusSquared;
            double h = 1 + k4 * radiusSquared + k5 * radiusSquared * radiusSquared + k6 * radiusSquared * radiusSquared * radiusSquared;
            double d = g / h;

            xp = inputPt.X * d;
            yp = inputPt.Y * d;

            // Incorporate tangential distortion
            double xy = 2.0 * inputPt.X * inputPt.Y;
            double x2 = 2.0 * inputPt.X * inputPt.X;
            double y2 = 2.0 * inputPt.Y * inputPt.Y;
            xp += (this.TangentialDistortion[1] * (radiusSquared + x2)) + (this.TangentialDistortion[0] * xy);
            yp += (this.TangentialDistortion[0] * (radiusSquared + y2)) + (this.TangentialDistortion[1] * xy);

            outputPt = new Point2D(xp, yp);
            return true;
        }
    }
}
