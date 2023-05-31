// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a 3-dimensional surface mesh.
    /// </summary>
    public class Mesh3D
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh3D"/> class.
        /// </summary>
        /// <param name="vertices">Vertex points.</param>
        /// <param name="triangleIndices">Triangle indices.</param>
        public Mesh3D(Point3D[] vertices, uint[] triangleIndices)
        {
            this.Vertices = vertices;
            this.TriangleIndices = triangleIndices;
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
        /// Converts the mesh vertices to a point cloud.
        /// </summary>
        /// <returns>A <see cref="PointCloud3D"/> for the mesh vertices.</returns>
        public PointCloud3D ToPointCloud3D() => new (this.Vertices);
    }
}
