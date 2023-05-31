// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a 2-dimensional rectangle embedded in 3D space.
    /// </summary>
    /// <remarks>
    /// The rectangle is characterized by its four corner points.
    /// </remarks>
    public readonly struct Rectangle3D : IEquatable<Rectangle3D>
    {
        /// <summary>
        /// Gets the top-left corner of the rectangle.
        /// </summary>
        public readonly Point3D TopLeft;

        /// <summary>
        /// Gets the bottom-left corner of the rectangle.
        /// </summary>
        public readonly Point3D BottomLeft;

        /// <summary>
        /// Gets the top-right corner of the rectangle.
        /// </summary>
        public readonly Point3D TopRight;

        /// <summary>
        /// Gets the bottom-right corner of the rectangle.
        /// </summary>
        public readonly Point3D BottomRight;

        /// <summary>
        /// Gets whether or not this rectangle is degenerate (0-length width or height).
        /// </summary>
        public readonly bool IsDegenerate;

        /// <summary>
        /// Gets the width.
        /// </summary>
        public readonly double Width;

        /// <summary>
        /// Gets the height.
        /// </summary>
        public readonly double Height;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rectangle3D"/> struct.
        /// </summary>
        /// <param name="origin">The origin of the rectangle.</param>
        /// <param name="widthAxis">The horizontal width axis of the rectangle.</param>
        /// <param name="heightAxis">The vertical height axis of the rectangle.</param>
        /// <param name="left">The left edge of the rectangle (relative to origin along the width axis).</param>
        /// <param name="bottom">The bottom edge of the rectangle (relative to origin along the height axis).</param>
        /// <param name="width">The width of the rectangle (must be positive).</param>
        /// <param name="height">The height of the rectangle (must be positive).</param>
        /// <remarks>
        /// The edges of the rectangle are aligned to the specified width and height axes, which must be perpendicular.
        /// </remarks>
        public Rectangle3D(
            Point3D origin,
            UnitVector3D widthAxis,
            UnitVector3D heightAxis,
            double left,
            double bottom,
            double width,
            double height)
        {
            if (!widthAxis.IsPerpendicularTo(heightAxis, 0.001))
            {
                throw new ArgumentException("The width and height axes must be perpendicular to each other.");
            }

            if (width < 0 || height < 0)
            {
                throw new ArgumentException("Width and height must be non-negative values");
            }

            if (width == 0 || height == 0)
            {
                this.IsDegenerate = true;
            }
            else
            {
                this.IsDegenerate = false;
            }

            this.Width = width;
            this.Height = height;
            this.BottomLeft = origin + widthAxis.ScaleBy(left) + heightAxis.ScaleBy(bottom);
            var widthVector = widthAxis.ScaleBy(width);
            var heightVector = heightAxis.ScaleBy(height);
            this.BottomRight = this.BottomLeft + widthVector;
            this.TopLeft = this.BottomLeft + heightVector;
            this.TopRight = this.TopLeft + widthVector;
        }

        /// <summary>
        /// Returns a value indicating whether the specified rectangles are the same.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are the same; otherwise false.</returns>
        public static bool operator ==(Rectangle3D left, Rectangle3D right) => left.Equals(right);

        /// <summary>
        /// Returns a value indicating whether the specified rectangles are different.
        /// </summary>
        /// <param name="left">The first rectangle.</param>
        /// <param name="right">The second rectangle.</param>
        /// <returns>True if the rectangles are different; otherwise false.</returns>
        public static bool operator !=(Rectangle3D left, Rectangle3D right) => !left.Equals(right);

        /// <summary>
        /// Gets the center of the 3D rectangle.
        /// </summary>
        /// <returns>The center point of the rectangle.</returns>
        public Point3D GetCenter()
            => new (
                (this.TopLeft.X + this.BottomRight.X) / 2,
                (this.TopLeft.Y + this.BottomRight.Y) / 2,
                (this.TopLeft.Z + this.BottomRight.Z) / 2);

        /// <summary>
        /// Gets the corners of the rectangle.
        /// </summary>
        /// <returns>An enumeration containing the rectangle corners.</returns>
        public IEnumerable<Point3D> GetCorners()
        {
            yield return this.TopLeft;
            yield return this.TopRight;
            yield return this.BottomRight;
            yield return this.BottomLeft;
        }

        /// <summary>
        /// Computes the intersection between a 3D ray and this planar rectangle.
        /// </summary>
        /// <param name="ray3D">The 3D ray.</param>
        /// <param name="tolerance">An optional tolerance to account for floating point errors.</param>
        /// <returns>The intersection point, if one exists.</returns>
        public Point3D? IntersectionWith(Ray3D ray3D, double tolerance = 1E-10)
        {
            // compute the plane of the rectangle from three corner points
            var plane = Plane.FromPoints(this.TopLeft, this.TopRight, this.BottomRight);

            // compute the intersection of the ray with the plane of the rectangle
            var intersection = plane.IntersectionWith(ray3D, tolerance);

            // check whether the intersection is in the direction the ray is pointing
            // or in the opposite direction.
            if ((intersection - ray3D.ThroughPoint).DotProduct(ray3D.Direction) < 0)
            {
                // if the intersection is in the opposite direction, return null,
                // as this method should compute only intersection points "forward"
                // on the ray.
                return null;
            }

            return this.Contains(intersection, tolerance) ? intersection : null;
        }

        /// <summary>
        /// Computes the closest point in this planar rectangle to a specified 3D point.
        /// </summary>
        /// <param name="point3D">The 3D point to compute the closest point to.</param>
        /// <returns>The intersection point, if one exists.</returns>
        public Point3D ClosestPointTo(Point3D point3D)
        {
            // compute the plane of the rectangle from three corner points
            var candidates = new List<Point3D>();

            // compute the projection of the point in the plane of the rectangle, and,
            // if that projection falls inside the rectangle, that's the closest point
            var projectedPoint3D = point3D.ProjectOn(this.GetPlane());
            if (this.Contains(projectedPoint3D))
            {
                return projectedPoint3D;
            }

            candidates.Add(new LineSegment3D(this.BottomLeft, this.BottomRight).ClosestPointTo(point3D));
            candidates.Add(new LineSegment3D(this.BottomRight, this.TopRight).ClosestPointTo(point3D));
            candidates.Add(new LineSegment3D(this.TopRight, this.TopLeft).ClosestPointTo(point3D));
            candidates.Add(new LineSegment3D(this.TopLeft, this.BottomLeft).ClosestPointTo(point3D));

            var minDistance = candidates.Min(c => c.DistanceTo(point3D));
            return candidates.First(c => c.DistanceTo(point3D) == minDistance);
        }

        /// <summary>
        /// Gets the <see cref="Plane"/> in which the <see cref="Rectangle3D"/> lies.
        /// </summary>
        /// <returns>The <see cref="Plane"/> in which the <see cref="Rectangle3D"/> lies.</returns>
        public Plane GetPlane()
            => Plane.FromPoints(this.TopLeft, this.TopRight, this.BottomLeft);

        /// <summary>
        /// Get a coordinate system pose, with origin at the center.
        /// X-axis points in the facing direction of the normal.
        /// Y-axis points in the width direction.
        /// Z-axis points in the height direction.
        /// </summary>
        /// <returns>The centered coordinate system pose for the rectangle.</returns>
        public CoordinateSystem GetCenteredCoordinateSystem()
        {
            var widthVector = (this.BottomRight - this.BottomLeft).Normalize();
            var heightVector = (this.TopLeft - this.BottomLeft).Normalize();
            var normalVector = widthVector.CrossProduct(heightVector);
            return new CoordinateSystem(this.GetCenter(), normalVector, widthVector, heightVector);
        }

        /// <summary>
        /// Determines whether the rectangle contains a specified point.
        /// </summary>
        /// <param name="point3D">The point.</param>
        /// <param name="tolerance">An optional tolerance to account for floating point errors.</param>
        /// <returns>True if the rectangle contains the specified point, false otherwise.</returns>
        public bool Contains(Point3D point3D, double tolerance = 1E-10)
        {
            // Check first that the point is in the plane
            var distance = this.GetPlane().AbsoluteDistanceTo(point3D);

            if (distance > tolerance)
            {
                return false;
            }

            // Construct a width vector pointing left-to-right and a height vector pointing top-to-bottom (rooted in the top-left corner),
            var widthVector = this.TopRight - this.TopLeft;
            var heightVector = this.BottomLeft - this.TopLeft;

            // Construct a vector pointing from the top-left corner to the intersection point.
            // If the projection of this vector to two of the sides (width and height) are within
            // the bounds of each edge, then the point is inside the rectangle.
            // (0 <= c.w <= w.w) && (0 <= c.h <= h.h)
            var cornerToPoint = point3D - this.TopLeft;
            var widthVectorProjection = cornerToPoint.DotProduct(widthVector);
            var heightVectorProjection = cornerToPoint.DotProduct(heightVector);

            if (widthVectorProjection >= 0 && widthVectorProjection <= widthVector.DotProduct(widthVector) &&
                heightVectorProjection >= 0 && heightVectorProjection <= heightVector.DotProduct(heightVector))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Equals(Rectangle3D other) =>
            this.BottomLeft == other.BottomLeft &&
            this.BottomRight == other.BottomRight &&
            this.TopLeft == other.TopLeft &&
            this.TopRight == other.TopRight;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Rectangle3D other && this.Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(
            this.BottomRight,
            this.BottomLeft,
            this.TopRight,
            this.TopLeft);
    }
}
