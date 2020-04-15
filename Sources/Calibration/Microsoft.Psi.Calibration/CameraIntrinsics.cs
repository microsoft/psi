// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// CameraIntrinsics defines the intrinsic properties for a given camera.
    /// </summary>
    public class CameraIntrinsics : ICameraIntrinsics
    {
        private Matrix<double> transform;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraIntrinsics"/> class.
        /// </summary>
        /// <param name="imageWidth">The width of the image.</param>
        /// <param name="imageHeight">The height of the image.</param>
        /// <param name="transform">The intrinsics transform matrix.</param>
        /// <param name="radialDistortion">The radial distortion parameters.</param>
        /// <param name="tangentialDistortion">The tangential distortion parameters.</param>
        public CameraIntrinsics(
            int imageWidth,
            int imageHeight,
            Matrix<double> transform,
            Vector<double> radialDistortion = null,
            Vector<double> tangentialDistortion = null)
        {
            this.ImageWidth = imageWidth;
            this.ImageHeight = imageHeight;
            this.Transform = transform;
            this.RadialDistortion = radialDistortion ?? Vector<double>.Build.Dense(6, 0);
            this.TangentialDistortion = tangentialDistortion ?? Vector<double>.Build.Dense(2, 0);
            this.FocalLengthXY = new Point2D(this.Transform[0, 0], this.Transform[1, 1]);
            this.PrincipalPoint = new Point2D(this.Transform[0, 2], this.Transform[1, 2]);
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
        public int ImageWidth { get; private set; }

        /// <inheritdoc/>
        public int ImageHeight { get; private set; }

        /// <inheritdoc/>
        public Point2D ToPixelSpace(Point3D pt, bool distort)
        {
            // X points in the depth dimension. Y points to the left, and Z points up.
            Point2D pixelPt = new Point2D(-pt.Y / pt.X, -pt.Z / pt.X);
            if (distort)
            {
                this.DistortPoint(pixelPt, out pixelPt);
            }

            Point3D tmp = new Point3D(pixelPt.X, pixelPt.Y, 1.0);
            tmp = tmp.TransformBy(this.transform);
            return new Point2D(tmp.X, this.ImageHeight - tmp.Y);
        }

        /// <inheritdoc/>
        public Point3D ToCameraSpace(Point2D pt, double depth, bool undistort)
        {
            // Convert from pixel coordinates to NDC
            Point3D tmp = new Point3D(pt.X, pt.Y, 1.0);
            tmp = tmp.TransformBy(this.InvTransform);

            // Undistort the pixel
            Point2D pixelPt = new Point2D(tmp.X, tmp.Y);
            if (undistort)
            {
                pixelPt = this.UndistortPoint(pixelPt);
            }

            // X points in the depth dimension. Y points to the left, and Z points up.
            return new Point3D(depth, -pixelPt.X * depth, -pixelPt.Y * depth);
        }

        /// <inheritdoc/>
        public bool DistortPoint(Point2D undistortedPt, out Point2D distortedPt)
        {
            double x = undistortedPt.X;
            double y = undistortedPt.Y;

            // Our distortion model is defined as:
            // See https://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html?highlight=convertpointshomogeneous
            //        r^2 = x^2 + y^2
            //               (1+k1*r^2+k2*r^3+k3^r^6)
            //        Fx = x ------------------------ + t1*(r^2+ 2 * x^2) + 2 * t0 * x*y
            //               (1+k4*r^2+k5*r^3+k6^r^6)
            //
            //               (1+k1*r^2+k2*r^3+k3^r^6)
            //        Fy = y ------------------------ + t0*(r^2+ 2 * y^2) + 2 * t1 * x*y
            //               (1+k4*r^2+k5*r^3+k6^r^6)
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
            double k1 = this.RadialDistortion[0];
            double k2 = this.RadialDistortion[1];
            double k3 = this.RadialDistortion[2];
            double k4 = this.RadialDistortion[3];
            double k5 = this.RadialDistortion[4];
            double k6 = this.RadialDistortion[5];
            double t0 = this.TangentialDistortion[0];
            double t1 = this.TangentialDistortion[1];

#pragma warning disable SA1305
            bool converged = false;
            for (int j = 0; j < 100 && !converged; j++)
            {
                double distortedRadius = (x * x) + (y * y);
                double radiusSq = distortedRadius;
                double radiusSqSq = radiusSq * radiusSq;
                double g = 1 + k1 * radiusSq + k2 * radiusSqSq + k3 * radiusSq * radiusSqSq;
                double h = 1 + k4 * radiusSq + k5 * radiusSqSq + k6 * radiusSq * radiusSqSq;
                double dr2dx = 2 * x;
                double dr2dy = 2 * y;
                double d = g / h;
                double dgdr2 = k1 + 2 * k2 * radiusSq + 3 * k3 * radiusSqSq;
                double dhdr2 = k4 * 2 * k5 * radiusSq + 3 * k6 * radiusSqSq;
                double dddr2 = (dgdr2 * h - g * dhdr2) / (h * h);
                double dddx = dddr2 * 2 * x;
                double dddy = dddr2 * 2 * y;
                double dFxdx = x * dddx + d + 2 * t0 * y + 6 * t1 * x;
                double dFxdy = x * dddy + 2 * t0 * x + 2 * t1 * y;
                double dFydx = y * dddx + 2 * t1 * y + 2 * t0 * x;
                double dFydy = y * dddy + d + 2 * t1 * x + 6 * t0 * y;

                double det = (dFxdx * dFydy) - dFydx * dFxdy;

                if (System.Math.Abs(det) < 1E-16)
                {
                    // Not invertible. Perform no distortion
                    distortedPt = new Point2D(undistortedPt.X, undistortedPt.Y);
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
                xp -= undistortedPt.X;
                yp -= undistortedPt.Y;

                if ((xp * xp) + (yp * yp) < 1E-16)
                {
                    converged = true;
                    break;
                }

                // Update our new guess (i.e. x = x - J(F(x))^-1 * F(x))
                x = x - ((dFydy * xp) - (dFxdy * yp)) / det;
                y = y - ((-dFydx * xp) + (dFxdx * yp)) / det;
#pragma warning restore SA1305
            }

            distortedPt = new Point2D(x, y);
            return true;
        }

        /// <inheritdoc/>
        public Point2D UndistortPoint(Point2D distortedPt)
        {
            // Undistort pixel
            double xp, yp;
            double radiusSquared = (distortedPt.X * distortedPt.X) + (distortedPt.Y * distortedPt.Y);
            if (this.RadialDistortion != null)
            {
                double k1 = this.RadialDistortion[0];
                double k2 = this.RadialDistortion[1];
                double k3 = this.RadialDistortion[2];
                double k4 = this.RadialDistortion[3];
                double k5 = this.RadialDistortion[4];
                double k6 = this.RadialDistortion[5];
                double g = 1 + k1 * radiusSquared + k2 * radiusSquared * radiusSquared + k3 * radiusSquared * radiusSquared * radiusSquared;
                double h = 1 + k4 * radiusSquared + k5 * radiusSquared * radiusSquared + k6 * radiusSquared * radiusSquared * radiusSquared;
                double d = g / h;

                xp = distortedPt.X * d;
                yp = distortedPt.Y * d;
            }
            else
            {
                xp = distortedPt.X;
                yp = distortedPt.Y;
            }

            // If we are incorporating tangential distortion, include that here
            if (this.TangentialDistortion != null && (this.TangentialDistortion[0] != 0.0 || this.TangentialDistortion[1] != 0.0))
            {
                double xy = 2.0 * distortedPt.X * distortedPt.Y;
                double x2 = 2.0 * distortedPt.X * distortedPt.X;
                double y2 = 2.0 * distortedPt.Y * distortedPt.Y;
                xp += (this.TangentialDistortion[1] * (radiusSquared + x2)) + (this.TangentialDistortion[0] * xy);
                yp += (this.TangentialDistortion[0] * (radiusSquared + y2)) + (this.TangentialDistortion[1] * xy);
            }

            return new Point2D(xp, yp);
        }
    }
}
