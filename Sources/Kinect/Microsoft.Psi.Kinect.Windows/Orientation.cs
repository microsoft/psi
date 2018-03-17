// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;

#pragma warning disable SA1600
    internal class Orientation
    {
        public static Matrix Rodrigues(Matrix r)
        {
            // where r rotation vector, theta = norm(r), M = skew(r/theta)
            // R = I + sin(theta) M + (1-cos(theta)) M M
            double theta = r.Norm();
            var rn = new Matrix(3, 1);
            rn.Normalize(r);

            var matM = new Matrix(3, 3);
            matM[0, 0] = 0;
            matM[0, 1] = -rn[2];
            matM[0, 2] = rn[1];
            matM[1, 0] = rn[2];
            matM[1, 1] = 0;
            matM[1, 2] = -rn[0];
            matM[2, 0] = -rn[1];
            matM[2, 1] = rn[0];
            matM[2, 2] = 0;

            var matR = new Matrix(3, 3);
            matR.Identity();

            var sinThetaM = new Matrix(3, 3);
            sinThetaM.Scale(matM, Math.Sin(theta));
            matR.Add(sinThetaM);

            var matMM = new Matrix(3, 3);
            matMM.Mult(matM, matM);
            var cosThetaMM = new Matrix(3, 3);
            cosThetaMM.Scale(matMM, 1 - Math.Cos(theta));
            matR.Add(cosThetaMM);

            return matR;
        }

        // a port from http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToAngle/index.htm
        public static Matrix RotationVector(Matrix m)
        {
            double angle, x, y, z; // variables for result

            var v = new Matrix(3, 1);

            double epsilon = 0.01; // margin to allow for rounding errors
            double epsilon2 = 0.1; // margin to distinguish between 0 and 180 degrees
            // optional check that input is pure rotation, 'isRotationMatrix' is defined at:
            // http://www.euclideanspace.com/maths/algebra/matrix/orthogonal/rotation/
            // assert isRotationMatrix(m) : "not valid rotation matrix" ;// for debugging
            if ((Math.Abs(m[0, 1] - m[1, 0]) < epsilon)
              && (Math.Abs(m[0, 2] - m[2, 0]) < epsilon)
              && (Math.Abs(m[1, 2] - m[2, 1]) < epsilon))
            {
                // singularity found
                // first check for identity matrix which must have +1 for all terms
                //  in leading diagonal and zero in other terms
                if ((Math.Abs(m[0, 1] + m[1, 0]) < epsilon2)
                  && (Math.Abs(m[0, 2] + m[2, 0]) < epsilon2)
                  && (Math.Abs(m[1, 2] + m[2, 1]) < epsilon2)
                  && (Math.Abs(m[0, 0] + m[1, 1] + m[2, 2] - 3) < epsilon2))
                {
                    // this singularity is identity matrix so angle = 0
                    // return new axisAngle(0, 1, 0, 0); // zero angle, arbitrary axis
                    v.Zero();
                    return v;
                }

                // otherwise this singularity is angle = 180
                angle = Math.PI;
                double xx = (m[0, 0] + 1) / 2;
                double yy = (m[1, 1] + 1) / 2;
                double zz = (m[2, 2] + 1) / 2;
                double xy = (m[0, 1] + m[1, 0]) / 4;
                double xz = (m[0, 2] + m[2, 0]) / 4;
                double yz = (m[1, 2] + m[2, 1]) / 4;
                if ((xx > yy) && (xx > zz))
                { // m[0,0] is the largest diagonal term
                    if (xx < epsilon)
                    {
                        x = 0;
                        y = 0.7071;
                        z = 0.7071;
                    }
                    else
                    {
                        x = Math.Sqrt(xx);
                        y = xy / x;
                        z = xz / x;
                    }
                }
                else if (yy > zz)
                { // m[1,1] is the largest diagonal term
                    if (yy < epsilon)
                    {
                        x = 0.7071;
                        y = 0;
                        z = 0.7071;
                    }
                    else
                    {
                        y = Math.Sqrt(yy);
                        x = xy / y;
                        z = yz / y;
                    }
                }
                else
                { // m[2,2] is the largest diagonal term so base result on this
                    if (zz < epsilon)
                    {
                        x = 0.7071;
                        y = 0.7071;
                        z = 0;
                    }
                    else
                    {
                        z = Math.Sqrt(zz);
                        x = xz / z;
                        y = yz / z;
                    }
                }

                // return axis angle
                v[0] = x;
                v[1] = y;
                v[2] = z;
                v.Scale(angle);
                return v;
            }

            // as we have reached here there are no singularities so we can handle normally
            double s = Math.Sqrt(((m[2, 1] - m[1, 2]) * (m[2, 1] - m[1, 2]))
                + ((m[0, 2] - m[2, 0]) * (m[0, 2] - m[2, 0]))
                + ((m[1, 0] - m[0, 1]) * (m[1, 0] - m[0, 1]))); // used to normalise
            if (Math.Abs(s) < 0.001)
            {
                s = 1;
            }

            // prevent divide by zero, should not happen if matrix is orthogonal and should be
            // caught by singularity test above, but I've left it in just in case
            angle = Math.Acos((m[0, 0] + m[1, 1] + m[2, 2] - 1) / 2);
            x = (m[2, 1] - m[1, 2]) / s;
            y = (m[0, 2] - m[2, 0]) / s;
            z = (m[1, 0] - m[0, 1]) / s;

            // return new axisAngle(angle, x, y, z);
            v[0] = x;
            v[1] = y;
            v[2] = z;
            v.Scale(angle);
            return v;
        }
    }
#pragma warning restore SA1600
}
