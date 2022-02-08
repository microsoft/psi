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
        public bool ContainsPoint(Point3D point)
        {
            return
                point.X >= this.Min.X &&
                point.X <= this.Max.X &&
                point.Y >= this.Min.Y &&
                point.Y <= this.Max.Y &&
                point.Z >= this.Min.Z &&
                point.Z <= this.Max.Z;
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
