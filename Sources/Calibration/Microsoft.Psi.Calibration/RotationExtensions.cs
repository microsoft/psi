// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using MathNet.Numerics.LinearAlgebra;

    /// <summary>
    /// Define set of extensions for dealing with rotation matrices and vectors.
    /// </summary>
    public static class RotationExtensions
    {
        /// <summary>
        /// Use the Rodrigues formula for transforming a given rotation from axis-angle representation to a 3x3 matrix.
        /// Where 'r' is a rotation vector:
        /// theta = norm(r)
        /// M = skew(r/theta)
        /// R = I + M * sin(theta) + M*M * (1-cos(theta)).
        /// </summary>
        /// <param name="vectorRotation">Rotation in axis-angle vector representation,
        /// where the angle is represented by the length (L2-norm) of the vector.</param>
        /// <returns>Rotation in a 3x3 matrix representation.</returns>
        public static Matrix<double> AxisAngleToMatrix(Vector<double> vectorRotation)
        {
            if (vectorRotation.Count != 3)
            {
                throw new InvalidOperationException("The input must be a valid 3-element vector representing an axis-angle rotation.");
            }

            double theta = vectorRotation.L2Norm();

            var matR = Matrix<double>.Build.DenseIdentity(3, 3);

            // if there is no rotation (theta == 0) return identity rotation
            if (theta == 0)
            {
                return matR;
            }

            // Create a skew-symmetric matrix from the normalized axis vector
            var rn = vectorRotation.Normalize(2);
            var matM = Matrix<double>.Build.Dense(3, 3);
            matM[0, 0] = 0;
            matM[0, 1] = -rn[2];
            matM[0, 2] = rn[1];
            matM[1, 0] = rn[2];
            matM[1, 1] = 0;
            matM[1, 2] = -rn[0];
            matM[2, 0] = -rn[1];
            matM[2, 1] = rn[0];
            matM[2, 2] = 0;

            // I + M * sin(theta) + M*M * (1 - cos(theta))
            var sinThetaM = matM * Math.Sin(theta);
            matR += sinThetaM;
            var matMM = matM * matM;
            var cosThetaMM = matMM * (1 - Math.Cos(theta));
            matR += cosThetaMM;

            return matR;
        }

        /// <summary>
        /// Convert a rotation matrix to axis-angle representation (a unit vector scaled by the angular distance to rotate).
        /// </summary>
        /// <param name="m">Input rotation matrix.</param>
        /// <returns>Same rotation in axis-angle representation (L2-Norm of the vector represents angular distance).</returns>
        public static Vector<double> MatrixToAxisAngle(Matrix<double> m)
        {
            if (m.RowCount != 3 || m.ColumnCount != 3)
            {
                throw new InvalidOperationException("The input must be a valid 3x3 rotation matrix in order to compute its axis-angle representation.");
            }

            double epsilon = 0.01;

            // theta = arccos((Trace(m) - 1) / 2)
            double angle = Math.Acos((m.Trace() - 1.0) / 2.0);

            // Create the axis vector.
            var v = Vector<double>.Build.Dense(3, 0);

            if (angle < epsilon)
            {
                // If the angular distance to rotate is 0, we just return a vector of all zeroes.
                return v;
            }

            // Otherwise, the axis of rotation is extracted from the matrix as follows.
            v[0] = m[2, 1] - m[1, 2];
            v[1] = m[0, 2] - m[2, 0];
            v[2] = m[1, 0] - m[0, 1];

            if (v.L2Norm() < epsilon)
            {
                // if the axis to rotate around has 0 length, we are in a singularity where the angle has to be 180 degrees.
                angle = Math.PI;

                // We can extract the axis of rotation, knowing that v*vT = (m + I) / 2;
                // First compute r = (m + I) / 2
                var r = Matrix<double>.Build.Dense(3, 3);
                m.CopyTo(r);
                r[0, 0] += 1;
                r[1, 1] += 1;
                r[2, 2] += 1;
                r /= 2.0;

                // r = v*vT =
                // | v_x*v_x,  v_x*v_y,  v_x*v_z |
                // | v_x*v_y,  v_y*v_y,  v_y*v_z |
                // | v_x*v_z,  v_y*v_z,  v_z*v_z |
                // Extract x, y, and z as the square roots of the diagonal elements.
                var x = Math.Sqrt(r[0, 0]);
                var y = Math.Sqrt(r[1, 1]);
                var z = Math.Sqrt(r[2, 2]);

                // Now we need to determine the signs of x, y, and z.
                double xsign;
                double ysign;
                double zsign;

                double xy = r[0, 1];
                double xz = r[0, 2];

                if (xy > 0)
                {
                    if (xz > 0)
                    {
                        xsign = 1;
                        ysign = 1;
                        zsign = 1;
                    }
                    else
                    {
                        xsign = 1;
                        ysign = 1;
                        zsign = -1;
                    }
                }
                else
                {
                    if (xz > 0)
                    {
                        xsign = 1;
                        ysign = -1;
                        zsign = 1;
                    }
                    else
                    {
                        xsign = 1;
                        ysign = -1;
                        zsign = -1;
                    }
                }

                v[0] = x * xsign;
                v[1] = y * ysign;
                v[2] = z * zsign;
            }

            return v.Normalize(2) * angle;
        }
    }
}
