// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a voxel carrying data of a specified type.
    /// </summary>
    /// <typeparam name="T">The type of the voxel data.</typeparam>
    public class Voxel<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel{T}"/> class.
        /// </summary>
        /// <param name="index">The voxel index.</param>
        /// <param name="value">The voxel value.</param>
        /// <param name="voxelSize">The voxel size.</param>
        internal Voxel((int X, int Y, int Z) index, T value, double voxelSize)
        {
            this.Index = index;
            this.Value = value;
            this.VoxelSize = voxelSize;
        }

        /// <summary>
        /// Gets the voxel index.
        /// </summary>
        public (int X, int Y, int Z) Index { get; }

        /// <summary>
        /// Gets or sets the voxel value.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Gets the voxel size.
        /// </summary>
        public double VoxelSize { get; }

        /// <summary>
        /// Gets the center point of the voxel.
        /// </summary>
        /// <returns>The center point of the voxel.</returns>
        public Point3D GetCenter()
            => new (
                (this.Index.X + 0.5) * this.VoxelSize,
                (this.Index.Y + 0.5) * this.VoxelSize,
                (this.Index.Z + 0.5) * this.VoxelSize);

        /// <summary>
        /// Gets the bounds of the voxel.
        /// </summary>
        /// <returns>The bounds of the voxel.</returns>
        public Bounds3D GetBounds3D()
            => new (
                this.Index.X * this.VoxelSize,
                (this.Index.X + 1) * this.VoxelSize,
                this.Index.Y * this.VoxelSize,
                (this.Index.Y + 1) * this.VoxelSize,
                this.Index.Z * this.VoxelSize,
                (this.Index.Z + 1) * this.VoxelSize);
    }
}
