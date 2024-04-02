// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;
    using static Operators;

    /// <summary>
    /// Represents a 3-dimensional surface mesh.
    /// </summary>
    [Serializer(typeof(Mesh3D.CustomSerializer))]
    public class Mesh3D
    {
        /// <summary>
        /// The private pose (used to determine if the Pose has been mutated and update the inversePose).
        /// </summary>
        [NonSerialized]
        private CoordinateSystem pose = null;

        /// <summary>
        /// The inverse pose (cached for efficiently resolving various queries, e.g., ray intersection).
        /// </summary>
        [NonSerialized]
        private CoordinateSystem inversePose = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh3D"/> class.
        /// </summary>
        /// <param name="vertices">Vertex points.</param>
        /// <param name="triangleIndices">Triangle indices.</param>
        /// <param name="pose">An optional pose for the mesh (by default, at origin).</param>
        public Mesh3D(Point3D[] vertices, uint[] triangleIndices, CoordinateSystem pose = null)
        {
            this.Vertices = vertices;
            this.TriangleIndices = triangleIndices;
            this.Pose = pose ?? new CoordinateSystem();
        }

        /// <summary>
        /// Gets mesh vertex points.
        /// </summary>
        public Point3D[] Vertices { get; private set; }

        /// <summary>
        /// Gets mesh triangle indices.
        /// </summary>
        public uint[] TriangleIndices { get; private set; }

        /// <summary>
        /// Gets the pose of the mesh.
        /// </summary>
        public CoordinateSystem Pose { get; }

        /// <summary>
        /// Converts the mesh vertices to a point cloud.
        /// </summary>
        /// <returns>A <see cref="PointCloud3D"/> for the mesh vertices.</returns>
        public PointCloud3D ToPointCloud3D() => new (this.Vertices.Select(v => v.TransformBy(this.Pose)));

        /// <summary>
        /// Computes the intersection point between a 3D ray and this mesh by testing the intersection with every triangle.
        /// </summary>
        /// <param name="ray3d">The 3D ray.</param>
        /// <returns>The intersection point, if one exists.</returns>
        public Point3D? IntersectionWith(Ray3D ray3d)
        {
            if (!Equals(this.Pose, this.pose))
            {
                this.Pose.DeepClone(ref this.pose);
                this.Pose.Invert().DeepClone(ref this.inversePose);
            }

            ray3d = this.inversePose.Transform(ray3d);
            Point3D? closestPoint = null;
            var minDistance = double.MaxValue;

            // Intersect the ray with each triangle in the mesh
            for (int i = 0; i < this.TriangleIndices.Length - 2; i += 3)
            {
                var a = this.Vertices[this.TriangleIndices[i]];
                var b = this.Vertices[this.TriangleIndices[i + 1]];
                var c = this.Vertices[this.TriangleIndices[i + 2]];

                if (TryComputePlane(a, b, c, out Plane trianglePlane))
                {
                    var planeIntersection = ray3d.IntersectionWith(trianglePlane);
                    if (planeIntersection.HasValue && TriangleContainsPoint(planeIntersection.Value, a, b, c))
                    {
                        // Check if the intersection is closer than any previous one
                        var distanceToTriangle = ray3d.ThroughPoint.DistanceTo(planeIntersection.Value);
                        if (distanceToTriangle < minDistance)
                        {
                            minDistance = distanceToTriangle;
                            closestPoint = planeIntersection.Value;
                        }
                    }
                }
            }

            return closestPoint?.TransformBy(this.Pose);
        }

        /// <summary>
        /// Computes the point on the mesh that is closest to the given query point.
        /// </summary>
        /// <param name="queryPoint">The query point.</param>
        /// <returns>The point on the surface of the mesh that is closest.</returns>
        /// <remarks>The computed point can be anywhere on the mesh's faces. It does not need to be a mesh vertex.</remarks>
        public Point3D ClosestPoint(Point3D queryPoint)
        {
            if (!Equals(this.Pose, this.pose))
            {
                this.Pose.DeepClone(ref this.pose);
                this.Pose.Invert().DeepClone(ref this.inversePose);
            }

            queryPoint = this.inversePose.Transform(queryPoint);

            // Keep track of the closest point and its distance
            Point3D closestPoint = default;
            var minDistance = double.MaxValue;
            void TestPointCloseness(Point3D p)
            {
                var d = queryPoint.DistanceTo(p);
                if (d < minDistance)
                {
                    minDistance = d;
                    closestPoint = p;
                }
            }

            // Test the query point against each triangle in the mesh
            for (int i = 0; i < this.TriangleIndices.Length - 2; i += 3)
            {
                var a = this.Vertices[this.TriangleIndices[i]];
                var b = this.Vertices[this.TriangleIndices[i + 1]];
                var c = this.Vertices[this.TriangleIndices[i + 2]];

                if (TryComputePlane(a, b, c, out Plane trianglePlane))
                {
                    // Project the point to the plane and check if it is inside the triangle.
                    var projectedQueryPoint = queryPoint.ProjectOn(trianglePlane);
                    if (TriangleContainsPoint(projectedQueryPoint, a, b, c))
                    {
                        TestPointCloseness(projectedQueryPoint);
                    }
                    else
                    {
                        // Otherwise test against all three sides of the triangle
                        TestPointCloseness(new LineSegment3D(a, b).ClosestPointTo(queryPoint));
                        TestPointCloseness(new LineSegment3D(b, c).ClosestPointTo(queryPoint));
                        TestPointCloseness(new LineSegment3D(c, a).ClosestPointTo(queryPoint));
                    }
                }
            }

            return closestPoint.TransformBy(this.Pose);
        }

        /// <summary>
        /// Provides custom read- backcompat serialization for <see cref="Mesh3D"/> objects.
        /// </summary>
        public class CustomSerializer : BackCompatClassSerializer<Mesh3D>
        {
            // When introducing a custom serializer, the LatestSchemaVersion
            // is set to be one above the auto-generated schema version (given by
            // RuntimeInfo.LatestSerializationSystemVersion, which was 2 at the time)
            private const int LatestSchemaVersion = 3;
            private SerializationHandler<Point3D[]> verticesHandler;
            private SerializationHandler<uint[]> triangleIndicesHandler;

            /// <summary>
            /// Initializes a new instance of the <see cref="CustomSerializer"/> class.
            /// </summary>
            public CustomSerializer()
                : base(LatestSchemaVersion)
            {
            }

            /// <inheritdoc/>
            public override void InitializeBackCompatSerializationHandlers(int schemaVersion, KnownSerializers serializers, TypeSchema targetSchema)
            {
                if (schemaVersion <= 2)
                {
                    this.verticesHandler = serializers.GetHandler<Point3D[]>();
                    this.triangleIndicesHandler = serializers.GetHandler<uint[]>();
                }
                else
                {
                    throw new NotSupportedException($"{nameof(Mesh3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }

            /// <inheritdoc/>
            public override void BackCompatDeserialize(int schemaVersion, BufferReader reader, ref Mesh3D target, SerializationContext context)
            {
                if (schemaVersion <= 2)
                {
                    var vertices = default(Point3D[]);
                    var triangleIndices = default(uint[]);
                    this.verticesHandler.Deserialize(reader, ref vertices, context);
                    this.triangleIndicesHandler.Deserialize(reader, ref triangleIndices, context);
                    target = new Mesh3D(vertices, triangleIndices);
                }
                else
                {
                    throw new NotSupportedException($"{nameof(Mesh3D.CustomSerializer)} only supports schema versions 2 and 3.");
                }
            }
        }
    }
}
