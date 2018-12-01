// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// CameraIntrinsics defines the intrinsic properties for a given camera
    /// </summary>
    public class CameraIntrinsics : ICameraIntrinsics
    {
        private Matrix<double> transform;

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraIntrinsics"/> class.
        /// </summary>
        public CameraIntrinsics()
        {
            this.RadialDistortion = Vector<double>.Build.Dense(2);
            this.RadialDistortion[0] = 0.0;
            this.RadialDistortion[1] = 0.0;
            this.TangentialDistortion = Vector<double>.Build.Dense(2);
            this.TangentialDistortion[0] = 0.0;
            this.TangentialDistortion[1] = 0.0;
        }

        /// <summary>
        /// Gets or sets the camera's radial distortion coefficients
        /// This should be a 2x1 vector of coefficients.
        /// </summary>
        public Vector<double> RadialDistortion { get; set; }

        /// <summary>
        /// Gets or sets the camera's tangential distortion coefficients.
        /// This should be a 2x1 vector of coefficients.
        /// </summary>
        public Vector<double> TangentialDistortion { get; set; }

        /// <summary>
        /// Gets or sets the camera's instrinsic transform.
        /// This transform converts camera coordinates (in the camera's local space) into
        /// normalized device coordinates (NDC) ranging from -1..+1
        /// </summary>
        public Matrix<double> Transform
        {
            get
            {
                return this.transform;
            }

            set
            {
                this.transform = value;
                this.InvTransform = this.transform.Inverse();
            }
        }

        /// <summary>
        /// Gets the camera's inverse intrinsic transform.
        /// </summary>
        public Matrix<double> InvTransform { get; private set; }

        /// <summary>
        /// Gets or sets the Focal length (in pixels)
        /// </summary>
        public double FocalLength { get; set; }

        /// <summary>
        /// Gets or sets the Focal length separated in X and Y (in pixels)
        /// </summary>
        public Point2D FocalLengthXY { get; set; }

        /// <summary>
        /// Gets or sets the principal point (in pixels)
        /// </summary>
        public Point2D PrincipalPoint { get; set; }

        /// <summary>
        /// Gets or sets the width of the image plane in pixels
        /// </summary>
        public int ImageWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the image plane in pixels
        /// </summary>
        public int ImageHeight { get; set; }

        /// <summary>
        /// Build is used to create our camera's intrinsic matrix. This matrix
        /// converts from camera space coordinates into pixel coordinates.
        /// </summary>
        /// <param name="principalPoint">Image planes principal point</param>
        /// <param name="imageWidth">Width in pixels of image plane</param>
        /// <param name="imageHeight">Height in pixels of image plane</param>
        /// <param name="focalLength">focal length of the camera in mm</param>
        /// <param name="skew">Skew factor to account for non-perpendicular image plane axis</param>
        /// <param name="xscale">Scale factor to apply to X axis (pixels per mm)</param>
        /// <param name="yscale">Scale factor to apply to Y axis (pixels per mm)</param>
        /// <returns>Returns a new IntrinsicData object</returns>
        public static CameraIntrinsics Build(Point2D principalPoint, int imageWidth, int imageHeight, double focalLength, double skew, double xscale, double yscale)
        {
            CameraIntrinsics data = new CameraIntrinsics();

            // First setup our projection matrix (converts from camera coordinates into NDC)
            // For more detauls, see Hartley & Zisserman's "Multiple View Geometry in Computer Vision", page 157.
            data.Transform = Matrix<double>.Build.Dense(4, 4);
            data.Transform[0, 0] = focalLength * xscale;
            data.Transform[0, 1] = skew;
            data.Transform[0, 2] = xscale;
            data.Transform[1, 1] = focalLength * yscale;
            data.Transform[1, 2] = yscale;
            data.InvTransform = data.Transform.Inverse();
            data.ImageWidth = imageWidth;
            data.ImageHeight = imageHeight;
            data.PrincipalPoint = principalPoint;
            data.FocalLength = focalLength;
            return data;
        }

        /// <summary>
        /// Converts a point in the camera's local space to a pixel coordinate
        /// </summary>
        /// <param name="pt">Point in camera space</param>
        /// <param name="distort">Should distortion be applied to the project pixel coordinates</param>
        /// <returns>Point in pixel space</returns>
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

        /// <summary>
        /// Converts a pixel coordinate back into a camera space coordinate
        /// </summary>
        /// <param name="pt">Point in pixel space</param>
        /// <param name="depth">Depth at specified pixel</param>
        /// <param name="undistort">Should undistortion be applied to the point</param>
        /// <returns>Returns a point in camera space</returns>
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

        /// <summary>
        /// UndistortPoint applies the camera's radial and tangential undistortion
        /// to the specified (distorted) point. Point is camera space after projection (camera post-projection space).
        ///
        /// The undistortion is defined by the following equations:
        ///   Xu = Xd (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xd^2) + T0 * 2 * XdYd
        ///   Yu = Yd (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yd^2) + T1 * 2 * XdYd
        /// where:
        ///    Xd,Yd - distorted point's coordinates
        ///    Xu,Yu - undistorted point's coordinates
        ///    K0,K1 - radial distortion coefficients
        ///    T0,T1 - tangential distortion coefficients
        ///
        /// </summary>
        /// <param name="distortedPt">Distorted point in camera post-projection coordinates</param>
        /// <returns>Undistorted coordinates in camera post-projection coordinates</returns>
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

        /// <summary>
        /// Distort takes a camera post-projection coordinate and applies the distortion
        /// model to it.
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
        /// We use Newton's method for finding the inverse of this. That is
        ///             Xd(n+1) = Xd(n) + J^-1 * F(Xd,Yd)
        /// </summary>
        /// <param name="undistortedPt">Defines our distorted point in camera post-projection coordinates</param>
        /// <param name="distortedPt">Returns the distorted point</param>
        /// <returns>true if 'distortedPt' contains the distorted point. false if it didn't converge</returns>
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
    }
}
