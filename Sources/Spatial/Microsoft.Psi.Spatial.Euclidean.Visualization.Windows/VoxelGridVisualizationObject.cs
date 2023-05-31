// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a base visualization object for a voxel grid.
    /// </summary>
    /// <typeparam name="TVoxelVisual3D">The type of the voxel visual.</typeparam>
    /// <typeparam name="TVoxelData">The type of data contained in the voxel.</typeparam>
    public abstract class VoxelGridVisualizationObject<TVoxelVisual3D, TVoxelData> : ModelVisual3DVisualizationObject<VoxelGrid<TVoxelData>>
        where TVoxelVisual3D : Visual3D, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelGridVisualizationObject{TVoxelVisual3D, TVoxelData}"/> class.
        /// </summary>
        public VoxelGridVisualizationObject()
        {
            this.VoxelVisuals = new (null);
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets the number of voxels.
        /// </summary>
        [DataMember]
        [DisplayName("Voxel Count")]
        [Description("The number of voxels.")]
        public int VoxelCount => this.CurrentData != null ? this.CurrentData.Count() : 0;

        /// <summary>
        /// Gets the visual nodes.
        /// </summary>
        protected UpdatableVisual3DDictionary<(int, int, int), TVoxelVisual3D> VoxelVisuals { get; }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.VoxelVisuals.BeginUpdate();

            if (this.CurrentData != null)
            {
                // update the joints
                foreach (var voxel in this.CurrentData)
                {
                    var nodeVisualizationObject = this.VoxelVisuals[voxel.Index];
                    this.UpdateVoxelVisuals(nodeVisualizationObject, voxel);
                }
            }

            this.VoxelVisuals.EndUpdate();

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <summary>
        /// Provides an abstract method for updating voxel visualization.
        /// </summary>
        /// <param name="voxelVisual3D">The voxel visual to update.</param>
        /// <param name="voxel">The voxel.</param>
        protected abstract void UpdateVoxelVisuals(TVoxelVisual3D voxelVisual3D, Voxel<TVoxelData> voxel);

        private void UpdateVisibility()
            => this.UpdateChildVisibility(this.VoxelVisuals, this.Visible && this.CurrentData != null);
    }
}
