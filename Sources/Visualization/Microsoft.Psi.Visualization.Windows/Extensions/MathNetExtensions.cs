// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Extensions
{
    using System.Windows.Media.Media3D;
    using euclidean = MathNet.Spatial.Euclidean;

    /// <summary>
    /// Extension methods for use with MathNet.Spatial.Euclidean objects.
    /// </summary>
    public static class MathNetExtensions
    {
        /// <summary>
        /// Converts coordinate system to 3D Matrix.
        /// </summary>
        /// <param name="cs">Coordinate system to convert.</param>
        /// <returns>The converted matrix.</returns>
        public static Matrix3D GetMatrix3D(this euclidean.CoordinateSystem cs)
        {
            return new Matrix3D(
                cs.Values[0],
                cs.Values[1],
                cs.Values[2],
                cs.Values[3],
                cs.Values[4],
                cs.Values[5],
                cs.Values[6],
                cs.Values[7],
                cs.Values[8],
                cs.Values[9],
                cs.Values[10],
                cs.Values[11],
                cs.Values[12],
                cs.Values[13],
                cs.Values[14],
                cs.Values[15]);
        }

        /// <summary>
        /// Convert MathNet.Spatial.Euclidean.Point3D to System.Windows.Media.Media3D.Point3D.
        /// </summary>
        /// <param name="point">The point to convert.</param>
        /// <returns>The converted point.</returns>
        public static Point3D ToPoint3D(this euclidean.Point3D point)
        {
            return new Point3D(point.X, point.Y, point.Z);
        }

        /// <summary>
        /// Convert MathNet.Spatial.Euclidean.Vector3D to System.Windows.Media.Media3D.Vector3D.
        /// </summary>
        /// <param name="vector">The vector to convert.</param>
        /// <returns>The converted vector.</returns>
        public static Vector3D ToVector3D(this euclidean.Vector3D vector)
        {
            return new Vector3D(vector.X, vector.Y, vector.Z);
        }
    }
}
