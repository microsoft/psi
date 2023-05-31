// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a voxel grid, with each voxel containing data of a specified type.
    /// </summary>
    /// <typeparam name="T">The type of data in the voxel.</typeparam>
    public class VoxelGrid<T> : IEnumerable<Voxel<T>>
    {
        // The collection of voxels, stored as a dictionary for fast access
        private readonly Dictionary<(int X, int Y, int Z), Voxel<T>> voxels = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelGrid{T}"/> class.
        /// </summary>
        /// <param name="voxelSize">The voxel size.</param>
        public VoxelGrid(double voxelSize)
        {
            if (voxelSize <= 0)
            {
                throw new ArgumentException("Voxel size must be strictly positive.", nameof(voxelSize));
            }

            this.VoxelSize = voxelSize;
        }

        /// <summary>
        /// Gets the voxel size in meters.
        /// </summary>
        public double VoxelSize { get; }

        /// <summary>
        /// Gets the voxel for a specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The voxel.</returns>
        public Voxel<T> this[(int x, int y, int z) index] => this.voxels[index];

        /// <summary>
        /// Gets the voxel for a specified set of coordinates.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">The z coordinate.</param>
        /// <returns>The voxel.</returns>
        public Voxel<T> this[double x, double y, double z] => this[this.GetIndex(x, y, z)];

        /// <summary>
        /// Gets the voxel for a specified 3D point.
        /// </summary>
        /// <param name="point3D">The 3D point.</param>
        /// <returns>The voxel.</returns>
        public Voxel<T> this[Point3D point3D] => this[this.GetIndex(point3D)];

        /// <summary>
        /// Gets the voxel index for a specified set of coordinates.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="z">The z coordinate.</param>
        /// <returns>The corresponding voxel index.</returns>
        public (int X, int Y, int Z) GetIndex(double x, double y, double z)
            => ((int)Math.Floor(x / this.VoxelSize), (int)Math.Floor(y / this.VoxelSize), (int)Math.Floor(z / this.VoxelSize));

        /// <summary>
        /// Gets the voxel index for a specified 3D point.
        /// </summary>
        /// <param name="point3D">The specified 3D point.</param>
        /// <returns>The corresponding voxel index.</returns>
        public (int X, int Y, int Z) GetIndex(Point3D point3D)
            => this.GetIndex(point3D.X, point3D.Y, point3D.Z);

        /// <summary>
        /// Indicates whether the voxel grid contains a voxel at a specified index.
        /// </summary>
        /// <param name="index">The specified index.</param>
        /// <returns>True if the voxel grid contains a voxel at the specified index.</returns>
        public bool Contains((int X, int Y, int Z) index)
            => this.voxels.ContainsKey(index);

        /// <summary>
        /// Indicates whether the voxel grid covers a specified 3D point.
        /// </summary>
        /// <param name="point3D">The 3D point.</param>
        /// <returns>True if the voxel grid covers the specified 3D point.</returns>
        public bool Contains(Point3D point3D)
            => this.voxels.ContainsKey(this.GetIndex(point3D));

        /// <summary>
        /// Adds a voxel to the grid at the specified index, with a specified value.
        /// </summary>
        /// <param name="index">The voxel index.</param>
        /// <param name="value">The voxel value.</param>
        public void Add((int X, int Y, int Z) index, T value)
            => this.voxels.Add(index, new Voxel<T>(index, value, this.VoxelSize));

        /// <summary>
        /// Removes a set of voxels by a specified predicate on the voxel data.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        public void Remove(Predicate<T> predicate) => this.Remove(v => predicate(v.Value));

        /// <summary>
        /// Removes a set of voxels by a specified predicate on the voxel index.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        public void Remove(Predicate<(int X, int Y, int Z)> predicate) => this.Remove(v => predicate(v.Index));

        /// <summary>
        /// Removes a set of voxels by a specified predicate on the voxel.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        public void Remove(Predicate<Voxel<T>> predicate)
        {
            var keysToRemove = this.voxels.Where(kvp => predicate(kvp.Value)).Select(kvp => kvp.Key).ToArray();

            foreach (var key in keysToRemove)
            {
                this.voxels.Remove(key);
            }
        }

        /// <inheritdoc/>
        public IEnumerator<Voxel<T>> GetEnumerator() => this.voxels.Values.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
