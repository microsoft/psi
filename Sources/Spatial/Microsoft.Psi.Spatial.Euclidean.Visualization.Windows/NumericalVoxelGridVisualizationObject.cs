// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a visualization object for a voxel grid of doubles.
    /// </summary>
    [VisualizationObject("Voxel grid.")]
    public class NumericalVoxelGridVisualizationObject : VoxelGridVisualizationObject<BoxVisual3D, double>
    {
        private double threshold = 0;
        private Color fillColor = Colors.Blue;

        /// <summary>
        /// Gets or sets the value threshold defining which voxels are shown.
        /// </summary>
        [DataMember]
        [DisplayName("Threshold")]
        [Description("The threshold value below which the voxel is not shown.")]
        public double Threshold
        {
            get { return this.threshold; }
            set { this.Set(nameof(this.Threshold), ref this.threshold, value); }
        }

        /// <summary>
        /// Gets or sets the voxels fill color.
        /// </summary>
        [DataMember]
        [DisplayName("Fill Color")]
        [Description("The fill color of the voxels.")]
        public Color FillColor
        {
            get { return this.fillColor; }
            set { this.Set(nameof(this.FillColor), ref this.fillColor, value); }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Threshold))
            {
                this.UpdateData();
            }

            base.NotifyPropertyChanged(propertyName);
        }

        /// <inheritdoc/>
        protected override void UpdateVoxelVisuals(BoxVisual3D voxelVisual3D, Voxel<double> voxel)
        {
            voxelVisual3D.BeginEdit();

            voxelVisual3D.Visible = this.GetVoxelVisibility(voxel);

            if (voxelVisual3D.Visible)
            {
                voxelVisual3D.Fill = new LinearGradientBrush(this.FillColor, Color.FromArgb(128, this.FillColor.R, this.FillColor.G, this.FillColor.B), 45.0d);
                voxelVisual3D.Width = voxel.VoxelSize * 0.9d;
                voxelVisual3D.Height = voxel.VoxelSize * 0.9d;
                voxelVisual3D.Length = voxel.VoxelSize * 0.9d;
                voxelVisual3D.Center = voxel.GetCenter().ToPoint3D();
            }

            voxelVisual3D.EndEdit();
        }

        private bool GetVoxelVisibility(Voxel<double> voxel)
            => voxel.Value > this.threshold;
    }
}
