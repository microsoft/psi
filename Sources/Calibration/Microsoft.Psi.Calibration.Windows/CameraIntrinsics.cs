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
        /// Gets or sets the Focal length
        /// </summary>
        public double FocalLength { get; set; }

        /// <summary>
        /// Gets or sets the principal point
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
        /// <param name="xscale">Scale factor to apply to X axis</param>
        /// <param name="yscale">Scale factor to apply to Y axis</param>
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
                pixelPt = this.DistortPoint(pixelPt);
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
        /// DistortPoint applies the camera's radial and tangential distortion
        /// to the specified (undistorted) point. Point is in NDC space (-1..+1).
        ///
        /// The distortion is defined by the following equations:
        ///   Xd = Xu (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xu^2) + T0 * 2 * XuYu
        ///   Yd = Yu (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yu^2) + T1 * 2 * XuYu
        /// where:
        ///    Xu,Yu - undistorted point's coordinates
        ///    Xd,Yd - distorted point's coordinates
        ///    K0,K1 - radial distortion coefficients
        ///    T0,T1 - tangential distortion coefficients
        ///
        /// </summary>
        /// <param name="undistortedPt">Undistorted point in NDC coordinates</param>
        /// <returns>Distorted pixel coordinates in NDC</returns>
        public Point2D DistortPoint(Point2D undistortedPt)
        {
            double radiusSquared = (undistortedPt.X * undistortedPt.X) + (undistortedPt.Y * undistortedPt.Y);

            // Undistort pixel
            double xp, yp;
            if (this.RadialDistortion != null)
            {
                xp = undistortedPt.X * (1.0 + (this.RadialDistortion[0] * radiusSquared) + (this.RadialDistortion[1] * radiusSquared * radiusSquared));
                yp = undistortedPt.Y * (1.0 + (this.RadialDistortion[0] * radiusSquared) + (this.RadialDistortion[1] * radiusSquared * radiusSquared));
            }
            else
            {
                xp = undistortedPt.X;
                yp = undistortedPt.Y;
            }

            // If we are incorporating tangential distortion, include that here
            if (this.TangentialDistortion != null && (this.TangentialDistortion[0] != 0.0 || this.TangentialDistortion[1] != 0.0))
            {
                double xy = 2.0 * undistortedPt.X * undistortedPt.Y;
                double x2 = 2.0 * undistortedPt.X * undistortedPt.X;
                double y2 = 2.0 * undistortedPt.Y * undistortedPt.Y;
                xp += (this.TangentialDistortion[1] * (radiusSquared + x2)) + (this.TangentialDistortion[0] * xy);
                yp += (this.TangentialDistortion[0] * (radiusSquared + y2)) + (this.TangentialDistortion[1] * xy);
            }

            return new Point2D(xp, yp);
        }

        /// <summary>
        /// Undistort takes a pixel coordinate that has had distortion applied to it
        /// and computes an undistorted pixel coordinate.
        ///
        /// The distortion is defined by the following equations:
        ///   Xd = Xu (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xu^2) + T0 * 2 * XuYu
        ///   Yd = Yu (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yu^2) + T1 * 2 * XuYu
        /// where:
        ///    Xu,Yu - undistorted point's coordinates
        ///    Xd,Yd - distorted point's coordinates
        ///    K0,K1 - radial distortion coefficients
        ///    T0,T1 - tangential distortion coefficients
        ///    r - sqrt(Xu*Xu+Yu*Yu)
        ///
        /// We use Newton's method for finding the inverse of this. That is
        ///             Xu(n+1) = Xu(n) + J^-1 * F(Xd,Yd)
        /// </summary>
        /// <param name="distortedPt">Defines our distorted pixel coordinate</param>
        /// <returns>The undistorted point in NDC coordinates</returns>
        public Point2D UndistortPoint(Point2D distortedPt)
        {
            // Our distortion model is defined as:
            //    Xd = Xu (1 + K0 * r^2 + K1 * r^4) + T1 * (r^2 + 2Xu^2) + T0 * 2 * XuYu
            //    Yd = Yu (1 + K0 * r^2 + K1 * r^4) + T0 * (r^2 + 2Yu^2) + T1 * 2 * XuYu
            // Next we need to compute the Jacobian of this:
            //    @Fx/@x = (1 + K0 * r^2 + K1 * r^4) + Xu @/@x (1 + K0 * r^2 + K1 * r^4) + @/@x T1*r^2 + T1*4*Xu + T0*2*Yu
            //           = (1 + K0 * r^2 + K1 * r^4) + Xu (K0 * @/@x r^2 + K1 * @/@x r^4) + T1 @/@x r^2 + T1*4*Xu + T0*2*Yu
            //    @Fx/@y = Xu @/@y (1 + K0 * r^2 + K1 * r^4) + @/@y T1*r^2 + T0*2*Xu
            //           = Xu (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T1*r^2 + T0*2*Xu
            //    @Fy/@x = Yu (K0 * @/@x r^2 + K1 * @/@x r^4) + @/@x T0*r^2 + T1*2*Yu
            //    @Fy/@y = (1 + K0 * r^2 + K1 * r^4) + Yu (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T0*r^2 + 4*Yu*T0 + 2*T1*Xu
            // Next compute partials of r^2 and r^4
            //    r^2=x^2+y^2
            //    @r^2/@x = 2x
            //    @r^2/@y = 2y
            //    @r^4/@x = @/@x(r^2)^2 = 2*r^2*@/@x(r^2) = 2*r^2*2x=4xr^2
            // Next solving for:
            //                          1                            | @Fy/@y   -@Fx/@y |
            //    J(F(x))^-1 =  ------------------------------------ |                  |
            //                   @Fx/@x * @Fy/dy - @Fy/@x * @Fx/@y   | -@Fy/@x  @Fx/@x  |

            // Check if we are accounting for tangential distortion. If not, then the solution is considerably simpler.
            double x = distortedPt.X;
            double y = distortedPt.Y;
            if (this.RadialDistortion[0] != 0.0 || this.RadialDistortion[1] != 0.0 ||
                this.TangentialDistortion[0] != 0.0 || this.TangentialDistortion[1] != 0.0)
            {
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

                    // dFxdx = (1 + K0 * r^2 + K1 * r^4) + Xu (K0 * @/@x r^2 + K1 * @/@x r^4) + T1 @/@x r^2 + T1*4*Xu + T0*2*Yu
                    double dFxdx = (1 + (k0 * radiusSq) + (k1 * radiusSqSq)) +
                        (x * ((k0 * 2 * x) + (k1 * 4 * x * radiusSq))) + (t1 * 2 * x) + (t1 * 4 * x) + (t0 * 2 * y);

                    // dFxdy = Xu (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T1*r^2 + T0*2*Xu
                    double dFxdy = (x * ((k0 * 2 * y) + (k1 * 4 * y * radiusSq))) + (t1 * 2 * y) + (t0 * 2 * x);

                    // dFydx = Yu (K0 * @/@x r^2 + K1 * @/@x r^4) + @/@x T0*r^2 + T1*2*Yu
                    double dFydx = (y * ((k0 * 2 * x) + (k1 * 4 * x * radiusSq))) + (t0 * 2 * x) + (t1 * 2 * y);

                    // dFydy = (1 + K0 * r^2 + K1 * r^4) + Yu (K0 * @/@y r^2 + K1 * @/@y r^4) + @/@y T0*r^2 + 4*Yu*T0 + 2*T1*Xu
                    double dFydy = (1 + ((k0 * radiusSq) + (k1 * radiusSqSq))) +
                        (y * ((k0 * 2 * y) + (k1 * 4 * y * radiusSq))) + (t0 * 2 * y) + (t0 * 4 * y) + (t1 * 2 * x);

                    double det = 1.0 / ((dFxdx * dFydy) - (dFydx * dFxdy));
                    dFxdx = dFxdx / det;
                    dFxdy = dFxdy / det;
                    dFydx = dFydx / det;
                    dFydy = dFydy / det;

                    // We now want to compute:
                    //     Xn = Xn-1 - J(F(x)) * F(x)
                    double xy = 2.0 * x * y;
                    double x2 = 2.0 * x * x;
                    double y2 = 2.0 * y * y;
                    double xp = (x * (1.0 + (k0 * radiusSq) + (k1 * radiusSqSq))) + (t1 * (radiusSq + x2)) + (t0 * xy);
                    double yp = (y * (1.0 + (k0 * radiusSq) + (k1 * radiusSqSq))) + (t0 * (radiusSq + y2)) + (t1 * xy);

                    // Check for convergence
                    double dx = xp - distortedPt.X;
                    double dy = yp - distortedPt.Y;
                    if ((dx * dx) + (dy * dy) < 0.0001)
                    {
                        converged = true;
                    }

                    // Account for F(x) = distort(Xu) - Xd
                    xp -= distortedPt.X;
                    yp -= distortedPt.Y;

                    // Update our new guess (i.e. x = x - J(F(x)) * F(x))
                    x = x - ((dFydy * xp) - (dFxdy * yp));
                    y = y - ((-dFydx * xp) + (dFxdx * yp));
                }
#pragma warning restore SA1305
            }
            else
            {
                x = distortedPt.X;
                y = distortedPt.Y;
            }

            return new Point2D(x, y);
        }
    }
}
