// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// An enumeration of the facets for a 3D box.
    /// </summary>
    public enum Box3DFacet
    {
        /// <summary>
        /// The facet that lies on the plane having the minimum value along the x-axis.
        /// </summary>
        MinX,

        /// <summary>
        /// The facet that lies on the plane having the maximum value along the x-axis.
        /// </summary>
        MaxX,

        /// <summary>
        /// The facet that lies on the plane having the minimum value along the y-axis.
        /// </summary>
        MinY,

        /// <summary>
        /// The facet that lies on the plane having the maximum value along the y-axis.
        /// </summary>
        MaxY,

        /// <summary>
        /// The facet that lies on the plane having the minimum value along the z-axis.
        /// </summary>
        MinZ,

        /// <summary>
        /// The facet that lies on the plane having the maximum value along the z-axis.
        /// </summary>
        MaxZ,
    }

    /// <summary>
    /// Represents a 3D rectangular box.
    /// </summary>
    public class Box3D : IEquatable<Box3D>
    {
        /// <summary>
        /// The private pose (used to determine if the Pose has been mutated and update the inversePose).
        /// </summary>
        [NonSerialized]
        private CoordinateSystem pose = null;

        /// <summary>
        /// The inverse pose (cached for efficiently resolving ContainsPoint queries).
        /// </summary>
        [NonSerialized]
        private CoordinateSystem inversePose = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3D"/> class.
        /// </summary>
        /// <param name="x1">The x-offset of the first diagonal corner of the box relative to its origin.</param>
        /// <param name="x2">The x-offset of the second diagonal corner of the box relative to its origin.</param>
        /// <param name="y1">The y-offset of the first diagonal corner of the box relative to its origin.</param>
        /// <param name="y2">The y-offset of the second diagonal corner of the box relative to its origin.</param>
        /// <param name="z1">The z-offset of the first diagonal corner of the box relative to its origin.</param>
        /// <param name="z2">The z-offset of the second diagonal corner of the box relative to its origin.</param>
        /// <param name="pose">An optional pose for the box (by default, at origin).</param>
        /// <remarks>
        /// The box is defined by two opposite corners whose offsets are specified relative to its origin.
        /// The edges of the box are aligned to the x, y and z axes of its coordinate system.
        /// </remarks>
        public Box3D(double x1, double x2, double y1, double y2, double z1, double z2, CoordinateSystem pose = null)
            : this(new Bounds3D(x1, x2, y1, y2, z1, z2), pose)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3D"/> class.
        /// </summary>
        /// <param name="bounds">The bounds for the box.</param>
        /// <param name="pose">An optional pose for the box (by default, at origin).</param>
        public Box3D(Bounds3D bounds, CoordinateSystem pose = null)
        {
            this.Pose = pose ?? new CoordinateSystem();
            this.Bounds = bounds;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Box3D"/> class.
        /// </summary>
        /// <param name="origin">The origin of the rectangle.</param>
        /// <param name="xAxis">The x-axis of the rectangle.</param>
        /// <param name="yAxis">The y-axis of the rectangle.</param>
        /// <param name="zAxis">The z-axis of the rectangle.</param>
        /// <param name="x1">The x-offset of the first diagonal corner of the box relative to its origin.</param>
        /// <param name="x2">The x-offset of the second diagonal corner of the box relative to its origin.</param>
        /// <param name="y1">The y-offset of the first diagonal corner of the box relative to its origin.</param>
        /// <param name="y2">The y-offset of the second diagonal corner of the box relative to its origin.</param>
        /// <param name="z1">The z-offset of the first diagonal corner of the box relative to its origin.</param>
        /// <param name="z2">The z-offset of the second diagonal corner of the box relative to its origin.</param>
        /// <remarks>
        /// The box is defined by two opposite corners whose offsets are specified relative to its origin.
        /// The edges of the box are aligned to the specified x, y and z axes.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:Field names should not use Hungarian notation", Justification = "Allow use of param names xAxis, yAxis and zAxis.")]
        public Box3D(Point3D origin, UnitVector3D xAxis, UnitVector3D yAxis, UnitVector3D zAxis, double x1, double x2, double y1, double y2, double z1, double z2)
            : this(x1, x2, y1, y2, z1, z2, new CoordinateSystem(origin, xAxis, yAxis, zAxis))
        {
        }

        /// <summary>
        /// Gets the pose of the box.
        /// </summary>
        public CoordinateSystem Pose { get; }

        /// <summary>
        /// Gets the bounds for the box.
        /// </summary>
        public Bounds3D Bounds { get; }

        /// <summary>
        /// Gets the origin of the box.
        /// </summary>
        public Point3D Origin => this.Pose.Origin;

        /// <summary>
        /// Gets the x-axis of the box.
        /// </summary>
        public UnitVector3D XAxis => this.Pose.XAxis.Normalize();

        /// <summary>
        /// Gets the y-axis of the box.
        /// </summary>
        public UnitVector3D YAxis => this.Pose.YAxis.Normalize();

        /// <summary>
        /// Gets the z-axis of the box.
        /// </summary>
        public UnitVector3D ZAxis => this.Pose.ZAxis.Normalize();

        /// <summary>
        /// Gets the geometric center of the box.
        /// </summary>
        public Point3D Center => this.Bounds.Center.TransformBy(this.Pose);

        /// <summary>
        /// Gets the length of the box along its x-axis.
        /// </summary>
        public double LengthX => this.Bounds.SizeX;

        /// <summary>
        /// Gets the length of the box along its y-axis.
        /// </summary>
        public double LengthY => this.Bounds.SizeY;

        /// <summary>
        /// Gets the length of the box along its z-axis.
        /// </summary>
        public double LengthZ => this.Bounds.SizeZ;

        /// <summary>
        /// Gets the volume of the box.
        /// </summary>
        public double Volume => this.Bounds.Volume;

        /// <summary>
        /// Gets a value indicating whether the box is degenerate
        /// (i.e. one or more of its edges has zero length).
        /// </summary>
        public bool IsDegenerate => this.Bounds.IsDegenerate;

        /// <summary>
        /// Returns a value indicating whether the specified boxes are the same.
        /// </summary>
        /// <param name="left">The first box.</param>
        /// <param name="right">The second box.</param>
        /// <returns>True if the boxes are the same; otherwise false.</returns>
        public static bool operator ==(Box3D left, Box3D right)
        {
            if (left is null)
            {
                if (right is null)
                {
                    return true;
                }

                return false;
            }
            else
            {
                return left.Equals(right);
            }
        }

        /// <summary>
        /// Returns a value indicating whether the specified boxes are different.
        /// </summary>
        /// <param name="left">The first box.</param>
        /// <param name="right">The second box.</param>
        /// <returns>True if the boxes are different; otherwise false.</returns>
        public static bool operator !=(Box3D left, Box3D right) => !(left == right);

        /// <summary>
        /// Gets the corner points of the box.
        /// </summary>
        /// <returns>An enumeration containing the corner points for the box.</returns>
        public IEnumerable<Point3D> GetCorners()
        {
            var pose = this.Pose;
            return this.Bounds.GetCorners().Select(p => p.TransformBy(pose));
        }

        /// <summary>
        /// Determines whether the <see cref="Box3D"/> contains a point.
        /// </summary>
        /// <param name="point3D">The point to check.</param>
        /// <param name="epsilon">An optional epsilon parameter to specify a numerical tolerance.</param>
        /// <returns>True if this <see cref="Box3D"/> contains the point, otherwise false.</returns>
        public bool ContainsPoint(Point3D point3D, double epsilon = 0)
        {
            if (!Equals(this.Pose, this.pose))
            {
                this.Pose.DeepClone(ref this.pose);
                this.Pose.Invert().DeepClone(ref this.inversePose);
            }

            return this.Bounds.ContainsPoint(point3D.TransformBy(this.inversePose), epsilon);
        }

        /// <summary>
        /// Computes the intersection point between a 3D ray and this box.
        /// </summary>
        /// <param name="ray3D">The 3D ray.</param>
        /// <returns>The intersection point, if one exists.</returns>
        public Point3D? IntersectionWith(Ray3D ray3D)
        {
            // If the box is degenerate, return.
            // TODO: Better handle degenerate (e.g. planar) cases
            if (this.IsDegenerate)
            {
                return null;
            }

            var results = new List<Point3D>();

            // Compute the intersection points with each face of the box
            foreach (Box3DFacet facet in Enum.GetValues(typeof(Box3DFacet)))
            {
                var point3D = this.GetFacet(facet).IntersectionWith(ray3D);
                if (point3D.HasValue)
                {
                    results.Add(point3D.Value);
                }
            }

            // If there are any intersection points, get the closest one
            return results.Any() ? results.OrderBy(p => p.DistanceTo(ray3D.ThroughPoint)).First() : null;
        }

        /// <inheritdoc/>
        public bool Equals(Box3D other)
        {
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (this.GetType() != other.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            return this.Pose == other.Pose && this.Bounds == other.Bounds;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) =>
            obj is not null && obj.GetType().Equals(typeof(Box3D)) && this.Equals((Box3D)obj);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            HashCode.Combine(this.Pose, this.Bounds);

        /// <summary>
        /// Returns the facets for the box.
        /// </summary>
        /// <returns>An enumeration containing the facets for the box.</returns>
        public IEnumerable<Rectangle3D> GetFacets()
        {
            yield return this.GetFacet(Box3DFacet.MinX);
            yield return this.GetFacet(Box3DFacet.MaxX);
            yield return this.GetFacet(Box3DFacet.MinY);
            yield return this.GetFacet(Box3DFacet.MaxY);
            yield return this.GetFacet(Box3DFacet.MinZ);
            yield return this.GetFacet(Box3DFacet.MaxZ);
        }

        /// <summary>
        /// Returns a <see cref="Rectangle3D"/> representing the specified facet of this <see cref="Box3D"/>.
        /// </summary>
        /// <param name="facet">The requested facet.</param>
        /// <returns>The <see cref="Rectangle3D"/> representing the requested facet.</returns>
        public Rectangle3D GetFacet(Box3DFacet facet)
        {
            return facet switch
            {
                // MinX Facet:
                // Origin at lower-left corner (looking at the face from outside the box along the +X axis).
                // Width axis points in -Y direction, and height axis points in +Z direction.
                Box3DFacet.MinX =>
                    new Rectangle3D(
                        new Point3D(this.Bounds.Min.X, this.Bounds.Max.Y, this.Bounds.Min.Z).TransformBy(this.Pose),
                        this.YAxis.Negate(),
                        this.ZAxis,
                        0,
                        0,
                        this.LengthY,
                        this.LengthZ),

                // MaxX Facet:
                // Origin at lower-left corner (looking at the face from outside the box along the -X axis).
                // Width axis points in +Y direction, and height axis points in +Z direction.
                Box3DFacet.MaxX =>
                    new Rectangle3D(
                        new Point3D(this.Bounds.Max.X, this.Bounds.Min.Y, this.Bounds.Min.Z).TransformBy(this.Pose),
                        this.YAxis,
                        this.ZAxis,
                        0,
                        0,
                        this.LengthY,
                        this.LengthZ),

                // MinY Facet:
                // Origin at lower-left corner (looking at the face from outside the box along the +Y axis).
                // Width axis points in +X direction, and height axis points in +Z direction.
                Box3DFacet.MinY =>
                    new Rectangle3D(
                        new Point3D(this.Bounds.Min.X, this.Bounds.Min.Y, this.Bounds.Min.Z).TransformBy(this.Pose),
                        this.XAxis,
                        this.ZAxis,
                        0,
                        0,
                        this.LengthX,
                        this.LengthZ),

                // MaxY Facet:
                // Origin at lower-left corner (looking at the face from outside the box along the -Y axis).
                // Width axis points in -X direction, and height axis points in +Z direction.
                Box3DFacet.MaxY =>
                    new Rectangle3D(
                        new Point3D(this.Bounds.Max.X, this.Bounds.Max.Y, this.Bounds.Min.Z).TransformBy(this.Pose),
                        this.XAxis.Negate(),
                        this.ZAxis,
                        0,
                        0,
                        this.LengthX,
                        this.LengthZ),

                // MinZ Facet:
                // Origin at lower-left corner (looking at the face from outside the box along the +Z axis).
                // Width axis points in -X direction, and height axis points in +Y direction.
                Box3DFacet.MinZ =>
                    new Rectangle3D(
                        new Point3D(this.Bounds.Max.X, this.Bounds.Min.Y, this.Bounds.Min.Z).TransformBy(this.Pose),
                        this.XAxis.Negate(),
                        this.YAxis,
                        0,
                        0,
                        this.LengthX,
                        this.LengthY),

                // MaxZ Facet:
                // Origin at lower-left corner (looking at the face from outside the box along the -Z axis).
                // Width axis points in +X direction, and height axis points in +Y direction.
                Box3DFacet.MaxZ =>
                    new Rectangle3D(
                        new Point3D(this.Bounds.Min.X, this.Bounds.Min.Y, this.Bounds.Max.Z).TransformBy(this.Pose),
                        this.XAxis,
                        this.YAxis,
                        0,
                        0,
                        this.LengthX,
                        this.LengthY),

                _ => throw new ArgumentException(nameof(facet)),
            };
        }

        /// <summary>
        /// Transforms the box by a coordinate system.
        /// </summary>
        /// <param name="coordinateSystem">The coordinate system to transform the box by.</param>
        /// <returns>The transformed box.</returns>
        public Box3D TransformBy(CoordinateSystem coordinateSystem)
            => new (this.Bounds, this.Pose.TransformBy(coordinateSystem));

        /// <summary>
        /// Convert <see cref="Box3D"/> to canonical form with origin in the center.
        /// </summary>
        /// <returns><see cref="Box3D"/> in with origin in the center.</returns>
        public Box3D ToCenteredBox3D()
        {
            var r = (this.Bounds.Min - this.Bounds.Max) / 2;
            var bounds = new Bounds3D(-r.X, r.X, -r.Y, r.Y, -r.Z, r.Z);
            var translate = CoordinateSystem.Translation(this.Origin.VectorTo(this.Center));
            var pose = this.Pose.TransformBy(translate);
            return new Box3D(bounds, pose);
        }

        /// <summary>
        /// Inflates the <see cref="Box3D"/> by a specified scale factor.
        /// </summary>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>The inflated <see cref="Box3D"/>.</returns>
        public Box3D Scale(double scaleFactor)
            => new (this.Bounds.Scale(scaleFactor), this.Pose);

        /// <summary>
        /// Inflates the <see cref="Box3D"/> by specified scale factors on different axes.
        /// </summary>
        /// <param name="scaleFactorX">The scale factor on the X axis.</param>
        /// <param name="scaleFactorY">The scale factor on the Y axis.</param>
        /// <param name="scaleFactorZ">The scale factor on the Z axis.</param>
        /// <returns>The inflated <see cref="Box3D"/>.</returns>
        public Box3D Scale(double scaleFactorX, double scaleFactorY, double scaleFactorZ)
            => new (this.Bounds.Scale(scaleFactorX, scaleFactorY, scaleFactorZ), this.Pose);
    }
}
