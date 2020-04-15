// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for SpriteVisual.xaml.
    /// </summary>
    public partial class SpriteVisual : ModelVisual3D
    {
        private Sprite3DVisualizationObject visualizationObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpriteVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The sprite 3D visualization object.</param>
        public SpriteVisual(Sprite3DVisualizationObject visualizationObject)
        {
            this.InitializeComponent();
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Sprite3DVisualizationObject.CurrentValue))
            {
                this.SetTransform();
            }
            else if (e.PropertyName == nameof(Sprite3DVisualizationObject.Source))
            {
                this.LoadTexture();
            }
            else if (e.PropertyName == nameof(Sprite3DVisualizationObject.VertexPositions))
            {
                this.SetVertexPositions();
            }
        }

        private void SetVertexPositions()
        {
            if (this.visualizationObject.VertexPositions != null)
            {
                // need to do a simple type change from Math.NET to Media3D points
                var vertexPositions = this.visualizationObject.VertexPositions.Select(pt => new Point3D(pt.X, pt.Y, pt.Z));
                this.Mesh.Positions = new Point3DCollection(vertexPositions);
            }
        }

        private void SetTransform()
        {
            var globalTransform = this.visualizationObject.CurrentValue.GetValueOrDefault().Data;
            if (globalTransform != null)
            {
                this.Transform = new MatrixTransform3D(globalTransform.GetMatrix3D());
            }
        }

        private void LoadTexture()
        {
            if (!string.IsNullOrEmpty(this.visualizationObject.Source))
            {
                this.ImageBrush.ImageSource = new BitmapImage(new Uri(this.visualizationObject.Source));
            }
            else
            {
                this.ImageBrush = null;
            }
        }
    }
}
