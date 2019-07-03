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
            this.RadialDistortion = radialDistortion ?? Vector<double>.Build.Dense(2, 0);
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

        /// <summary>
        /// Build is used to create our camera's intrinsic matrix. This matrix
        /// converts from camera space coordinates into pixel coordinates.
        /// </summary>
        /// <param name="principalPoint">Image planes principal point.</param>
        /// <param name="imageWidth">Width in pixels of image plane.</param>
        /// <param name="imageHeight">Height in pixels of image plane.</param>
        /// <param name="focalLength">focal length of the camera in mm.</param>
        /// <param name="skew">Skew factor to account for non-perpendicular image plane axis.</param>
        /// <param name="xscale">Scale factor to apply to X axis (pixels per mm).</param>
        /// <param name="yscale">Scale factor to apply to Y axis (pixels per mm).</param>
        /// <returns>Returns a new IntrinsicData object.</returns>
        public static CameraIntrinsics Build(Point2D principalPoint, int imageWidth, int imageHeight, double focalLength, double skew, double xscale, double yscale)
        {
            // Set up our projection matrix (converts from camera coordinates into NDC)
            // For more details, see Hartley & Zisserman's "Multiple View Geometry in Computer Vision", page 157.
            var transform = Matrix<double>.Build.Dense(4, 4);
            transform[0, 0] = focalLength * xscale;
            transform[0, 1] = skew;
            transform[0, 2] = xscale;
            transform[1, 1] = focalLength * yscale;
            transform[1, 2] = yscale;

            return new CameraIntrinsics(imageWidth, imageHeight, transform);
        }

        /// <inheritdoc/>
        public Point2D ToPixelSpace(Point3D pt, bool distort)
        {
            Point2D pixelPt = new Point2D(pt.X / pt.Z, pt.Y / pt.Z);
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

            return new Point3D(pixelPt.X * depth, pixelPt.Y * depth, depth);
        }

        /// <inheritdoc/>
        public bool DistortPoint(Point2D undistortedPt, out Point2D distortedPt)
        {
            double x = undistortedPt.X;
            double y = undistortedPt.Y;

            // Check if we are accounting for tangential distortion. If not, then the solution is considerably simpler.
            if (this.TangentialDistortion[0] == 0.0 && this.TangentialDistortion[1] == 0.0 &&
                (this.RadialDistortion[0] != 0.0 || this.RadialDistortion[1] != 0.0))
            {
                // In this case there is no tangential distortion. Thus we can just take the derivative relative
                // to the radius and rescale the current point.
                double k0 = this.RadialDistortion[0];
                double k1 = this.RadialDistortion[1];

#pragma warning disable SA1305
                bool converged = false;
                double ru = System.Math.Sqrt((x * x) + (y * y));
                double r = ru;
                double r2 = 0;
                double r4 = 0;
                double factor = 1.0;
                for (int j = 0; j < 100 && !converged; j++)
                {
                    r2 = r * r;
                    r4 = r2 * r2;
                    factor = 1 + k0 * r2 + k1 * r4;
                    double num = r * factor - ru;
                    double denom = 1 + 3 * r2 * k0 + 5 * r4 * k1;
                    converged = System.Math.Abs(num / denom) < 1E-16;
                    r = r - num / denom;
                }

                x = undistortedPt.X / factor;
                y = undistortedPt.Y / factor;
#pragma warning restore SA1305
            }
            else if (this.RadialDistortion[0] != 0.0 || this.RadialDistortion[1] != 0.0)
            {
                // Our distortion model is defined as:
                //    Xu = Xd (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xd^2) + T0 * 2 * XdYd
                //    Yu = Yd (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yd^2) + T1 * 2 * XdYd
                // Next we need to compute the Jacobian of this:
                //    @Fx/@x = (1 + K0 * r^2 + K1 * r^4) + Xd @/@x (1 + K0 * r^2 + K1 * r^4) + @/@x T1*r^2 + T1*4*Xd + T0*2*Yd
                //           = (1 + K0 * r^2 + K1 * r^4) + Xd (K0 * @/@x r^2 + K1 * @/@x r^4) + T1 @/@x r^2 + T1*4*Xd + T0*2*Yd
                //    @Fx/@y = Xd @/@y (1 + K0 * r^2 + K1 * r^4) + @/@y T1*r^2 + T0*2*Xd
                //           = Xd (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T1*r^2 + T0*2*Xd
                //    @Fy/@x = Yd (K0 * @/@x r^2 + K1 * @/@x r^4) + @/@x T0*r^2 + T1*2*Yd
                //    @Fy/@y = (1 + K0 * r^2 + K1 * r^4) + Yd (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T0*r^2 + 4*Yd*T0 + 2*T1*Xd
                // Next compute partials of r^2 and r^4
                //    r^2=x^2+y^2
                //    @r^2/@x = 2x
                //    @r^2/@y = 2y
                //    @r^4/@x = @/@x(r^2)^2 = 2*r^2*@/@x(r^2) = 2*r^2*2x=4xr^2
                // Next solving for:
                //                          1                            | @Fy/@y   -@Fx/@y |
                //    J(F(x))^-1 =  ------------------------------------ |                  |
                //                   @Fx/@x * @Fy/dy - @Fy/@x * @Fx/@y   | -@Fy/@x  @Fx/@x  |
                double k0 = this.RadialDistortion[0];
                double k1 = this.RadialDistortion[1];
                double t0 = this.TangentialDistortion[0];
                double t1 = this.TangentialDistortion[1];

#pragma warning disable SA1305
                bool converged = false;
                for (int j = 0; j < 100 && !converged; j++)
                {
                    double distortedRadius = (x * x) + (y * y);
                    double radiusSq = distortedRadius;
                    double radiusSqSq = radiusSq * radiusSq;

                    // dFxdx = (1 + K0 * r^2 + K1 * r^4) + Xd (K0 * @/@x r^2 + K1 * @/@x r^4) + T1 @/@x r^2 + T1*4*Xd + T0*2*Yd
                    double dFxdx = (1 + (k0 * radiusSq) + (k1 * radiusSqSq)) +
                        (x * ((k0 * 2 * x) + (k1 * 4 * x * radiusSq))) + (t1 * 2 * x) + (t1 * 4 * x) + (t0 * 2 * y);

                    // dFxdy = Xd (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T1*r^2 + T0*2*Xd
                    double dFxdy = (x * ((k0 * 2 * y) + (k1 * 4 * y * radiusSq))) + (t1 * 2 * y) + (t0 * 2 * x);

                    // dFydx = Yd (K0 * @/@x r^2 + K1 * @/@x r^4) + @/@x T0*r^2 + T1*2*Yd
                    double dFydx = (y * ((k0 * 2 * x) + (k1 * 4 * x * radiusSq))) + (t0 * 2 * x) + (t1 * 2 * y);

                    // dFydy = (1 + K0 * r^2 + K1 * r^4) + Yd (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T0*r^2 + 4*Yd*T0 + 2*T1*Xd
                    double dFydy = (1 + ((k0 * radiusSq) + (k1 * radiusSqSq))) +
                        (y * ((k0 * 2 * y) + (k1 * 4 * y * radiusSq))) + (t0 * 2 * y) + (t0 * 4 * y) + (t1 * 2 * x);

                    double det = 1.0 / ((dFxdx * dFydy) - (dFydx * dFxdy));

                    if (det < 1E-16)
                    {
                        // Not invertible. Perform no undistortion
                        distortedPt = new Point2D(0.0, 0.0);
                        return false;
                    }

                    dFxdx = dFxdx / det;
                    dFxdy = dFxdy / det;
                    dFydx = dFydx / det;
                    dFydy = dFydy / det;

                    // We now want to compute:
                    //     Xn = Xn-1 - J(F(x))^-1 * F(x)
                    double xy = 2.0 * x * y;
                    double x2 = 2.0 * x * x;
                    double y2 = 2.0 * y * y;
                    double xp = (x * (1.0 + (k0 * radiusSq) + (k1 * radiusSqSq))) + (t1 * (radiusSq + x2)) + (t0 * xy);
                    double yp = (y * (1.0 + (k0 * radiusSq) + (k1 * radiusSqSq))) + (t0 * (radiusSq + y2)) + (t1 * xy);

                    // Account for F(x) = distort(Xd) - Xd, since we want to solve:
                    //   0 = Xd (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xd^2) + T0 * 2 * XdYd - Xu
                    //   0 = Yd (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yd^2) + T1 * 2 * XdYd - Yu
                    xp -= undistortedPt.X;
                    yp -= undistortedPt.Y;

                    if ((xp * xp) + (yp * yp) < 1E-16)
                    {
                        converged = true;
                        break;
                    }

                    // Update our new guess (i.e. x = x - J(F(x))^-1 * F(x))
                    x = x - ((dFydy * xp) - (dFxdy * yp));
                    y = y - ((-dFydx * xp) + (dFxdx * yp));
#pragma warning restore SA1305
                }
            }

            distortedPt = new Point2D(x, y);
            return true;
        }

        /// <inheritdoc/>
        public Point2D UndistortPoint(Point2D distortedPt)
        {
            double radiusSquared = (distortedPt.X * distortedPt.X) + (distortedPt.Y * distortedPt.Y);

            // Undistort pixel
            double xp, yp;
            if (this.RadialDistortion != null)
            {
                xp = distortedPt.X * (1.0 + (this.RadialDistortion[0] * radiusSquared) + (this.RadialDistortion[1] * radiusSquared * radiusSquared));
                yp = distortedPt.Y * (1.0 + (this.RadialDistortion[0] * radiusSquared) + (this.RadialDistortion[1] * radiusSquared * radiusSquared));
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
