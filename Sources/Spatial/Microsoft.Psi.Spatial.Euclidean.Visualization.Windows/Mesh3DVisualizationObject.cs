// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Mesh3D = Microsoft.Psi.Spatial.Euclidean.Mesh3D;

    /// <summary>
    /// Implements a 3D mesh visualization object.
    /// </summary>
    [VisualizationObject("3D Mesh")]
    public class Mesh3DVisualizationObject : ModelVisual3DVisualizationObject<Mesh3D>
    {
        private readonly MeshGeometryVisual3D modelVisual;

        // Fill properties
        private Color fillColor = Colors.DodgerBlue;
        private double fillOpacity = 100;
        private bool fillVisible = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh3DVisualizationObject"/> class.
        /// </summary>
        public Mesh3DVisualizationObject()
        {
            this.modelVisual = new MeshGeometryVisual3D
            {
                MeshGeometry = new MeshGeometry3D(),
            };

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the fill color.
        /// </summary>
        [DataMember]
        [DisplayName("Fill Color")]
        [Description("The fill color of the mesh.")]
        public Color FillColor
        {
            get { return this.fillColor; }
            set { this.Set(nameof(this.FillColor), ref this.fillColor, value); }
        }

        /// <summary>
        /// Gets or sets the fill opacity.
        /// </summary>
        [DataMember]
        [DisplayName("Fill Opacity")]
        [Description("The fill opacity of the mesh.")]
        public double FillOpacity
        {
            get { return this.fillOpacity; }
            set { this.Set(nameof(this.FillOpacity), ref this.fillOpacity, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the mesh is filled.
        /// </summary>
        [DataMember]
        [DisplayName("Fill Visibility")]
        [Description("Indicates whether the mesh is filled.")]
        public bool FillVisible
        {
            get { return this.fillVisible; }
            set { this.Set(nameof(this.FillVisible), ref this.fillVisible, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            var mesh = this.CurrentData;
            var geometry = new MeshGeometry3D();
            geometry.Positions = new Point3DCollection(mesh.Vertices.Select(p => p.ToPoint3D()));
            geometry.TriangleIndices = new Int32Collection(mesh.TriangleIndices.Select(i => (int)i));
            this.modelVisual.MeshGeometry = geometry;

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.FillColor) ||
                propertyName == nameof(this.FillOpacity))
            {
                this.UpdateMeshContents();
            }
            else if (propertyName == nameof(this.Visible) ||
                     propertyName == nameof(this.FillVisible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateMeshContents()
        {
            double opacity = Math.Max(0, Math.Min(100, this.fillOpacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.fillColor.R,
                this.fillColor.G,
                this.fillColor.B);
            var material = new DiffuseMaterial(new SolidColorBrush(alphaColor));
            this.modelVisual.Material = material;
            this.modelVisual.BackMaterial = material;
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.modelVisual, this.Visible && this.FillVisible);
        }
    }
}
