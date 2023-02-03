// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a 3-dimensional bounding box.
    /// </summary>
    public readonly struct Bounds3D : IEquatable<Bounds3D>
    {
        /// <summary>
        /// The offset of the first diagonal corner of the bounds.
        /// </summary>
        public readonly Vector3D Min;

        /// <summary>
        /// The offset of the second diagonal corner of the bounds.
        /// </summary>
        public readonly Vector3D Max;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bounds3D"/> struct.
        /// </summary>
        /// <param name="x1">The x-offset of the first diagonal corner of the bounds.</param>
        /// <param name="x2">The x-offset of the second diagonal corner of the bounds.</param>
        /// <param name="y1">The y-offset of the first diagonal corner of the bounds.</param>
        /// <param name="y2">The y-offset of the second diagonal corner of the bounds.</param>
        /// <param name="z1">The z-offset of the first diagonal corner of the bounds.</param>
        /// <param name="z2">The z-offset of the second diagonal corner of the bounds.</param>
        /// <remarks>
        /// The 3d bounds are defined by two opposite corners whose offsets are specified relative to origin.
        /// </remarks>
        public Bounds3D(double x1, double x2, double y1, double y2, double z1, double z2)
        {
            this.Min = new Vector3D(Math.Min(x1, x2), Math.Min(y1, y2), Math.Min(z1, z2));
            this.Max = new Vector3D(Math.Max(x1, x2), Math.Max(y1, y2), Math.Max(z1, z2));
        }

        /// <summary>
        /// Gets the size of the bounds along the x-axis.
        /// </summary>
        public double SizeX => this.Max.X - this.Min.X;

        /// <summary>
        /// Gets the size of the bounds along the y-axis.
        /// </summary>
        public double SizeY => this.Max.Y - this.Min.Y;

        /// <summary>
        /// Gets the size of the bounds along the z-axis.
        /// </summary>
        public double SizeZ => this.Max.Z - this.Min.Z;

        /// <summary>
        /// Gets the geometric center of the bounds.
        /// </summary>
        public Point3D Center => Point3D.MidPoint(this.Min.ToPoint3D(), this.Max.ToPoint3D());

        /// <summary>
        /// Gets the volume of the bounds.
        /// </summary>
        public double Volume => this.SizeX * this.SizeY * this.SizeZ;

        /// <summary>
        /// Gets a value indicating whether the bounds are degenerate
        /// (i.e. size zero in one or more dimensions).
        /// </summary>
        public bool IsDegenerate
        {
            get
            {
                return
                    this.Min.X == this.Max.X ||
                    this.Min.Y == this.Max.Y ||
                    this.Min.Z == this.Max.Z;
            }
        }

        /// <summary>
        /// Returns a value indicating whether the specified bounds are the same.
        /// </summary>
        /// <param name="left">The first bounds.</param>
        /// <param name="right">The second bounds.</param>
        /// <returns>True if the bounds are the same; otherwise false.</returns>
        public static bool operator ==(Bounds3D left, Bounds3D right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns a value indicating whether the specified bounds are different.
        /// </summary>
        /// <param name="left">The first bounds.</param>
        /// <param name="right">The second bounds.</param>
        /// <returns>True if bounds are different; otherwise false.</returns>
        public static bool operator !=(Bounds3D left, Bounds3D right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Gets the corner points for the bounds.
        /// </summary>
        /// <returns>An enumeration of corner points for the bounds.</returns>
        public IEnumerable<Point3D> GetCorners()
        {
            // Corner indices (0 = Min, 7 = Max)
            //   2------3
            //  /|     /|
            // 0-+----1 |
            // | |    | |
            // | 6----+-7
            // |/     |/
            // 4------5
            yield return new Point3D(this.Max.X, this.Max.Y, this.Max.Z);
            yield return new Point3D(this.Max.X, this.Min.Y, this.Max.Z);
            yield return new Point3D(this.Min.X, this.Max.Y, this.Max.Z);
            yield return new Point3D(this.Min.X, this.Min.Y, this.Max.Z);
            yield return new Point3D(this.Max.X, this.Max.Y, this.Min.Z);
            yield return new Point3D(this.Max.X, this.Min.Y, this.Min.Z);
            yield return new Point3D(this.Min.X, this.Max.Y, this.Min.Z);
            yield return new Point3D(this.Min.X, this.Min.Y, this.Min.Z);
        }

        /// <summary>
        /// Determines whether the <see cref="Bounds3D"/> contains a point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if this <see cref="Bounds3D"/> contains the point, otherwise false.</returns>
        /// <param name="epsilon">An optional epsilon parameter to specify a numerical tolerance.</param>
        public bool ContainsPoint(Point3D point, double epsilon = 0)
        {
            return
                point.X >= (this.Min.X - epsilon) &&
                point.X <= (this.Max.X + epsilon) &&
                point.Y >= (this.Min.Y - epsilon) &&
                point.Y <= (this.Max.Y + epsilon) &&
                point.Z >= (this.Min.Z - epsilon) &&
                point.Z <= (this.Max.Z + epsilon);
        }

        /// <summary>
        /// Inflates the bounds by a specified scale factor.
        /// </summary>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>The scaled bounds.</returns>
        public Bounds3D Scale(double scaleFactor)
            => this.Scale(scaleFactor, scaleFactor, scaleFactor);

        /// <summary>
        /// Inflates the bounds by specified scale factors on different axes.
        /// </summary>
        /// <param name="scaleFactorX">The scale factor on the X axis.</param>
        /// <param name="scaleFactorY">The scale factor on the Y axis.</param>
        /// <param name="scaleFactorZ">The scale factor on the Z axis.</param>
        /// <returns>The scaled bounds.</returns>
        public Bounds3D Scale(double scaleFactorX, double scaleFactorY, double scaleFactorZ)
        {
            var center = this.Center;
            var minX = center.X - (center.X - this.Min.X) * scaleFactorX;
            var maxX = center.X + (this.Max.X - center.X) * scaleFactorX;
            var minY = center.Y - (center.Y - this.Min.Y) * scaleFactorY;
            var maxY = center.Y + (this.Max.Y - center.Y) * scaleFactorY;
            var minZ = center.Z - (center.Z - this.Min.Z) * scaleFactorZ;
            var maxZ = center.Z + (this.Max.Z - center.Z) * scaleFactorZ;
            return new Bounds3D(minX, maxX, minY, maxY, minZ, maxZ);
        }

        /// <inheritdoc/>
        public bool Equals(Bounds3D other)
        {
            return
                this.Min == other.Min &&
                this.Max == other.Max;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Bounds3D other && this.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Min, this.Max);
        }
    }
}
