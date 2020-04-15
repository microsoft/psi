// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.ComponentModel;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for AnimatedModelVisual.xaml.
    /// </summary>
    public partial class AnimatedModelVisual : ModelVisual3D
    {
        private AnimatedModel3DVisualizationObject visualizationObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimatedModelVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The animated model 3D visualization object.</param>
        public AnimatedModelVisual(AnimatedModel3DVisualizationObject visualizationObject)
        {
            this.InitializeComponent();
            this.LoadModel();
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
        }

        /// <summary>
        /// Gets a value indicating whether this is a camera location.
        /// </summary>
        public bool IsCameraLocation => this.visualizationObject.CameraTransform != null;

        /// <summary>
        /// Gets the camera transformation.
        /// </summary>
        public Matrix3D CameraTransform => this.visualizationObject.CameraTransform.GetMatrix3D();

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.visualizationObject.CurrentValue))
            {
                var data = this.visualizationObject.CurrentValue.GetValueOrDefault().Data;
                if (data != null)
                {
                    this.Transform = new MatrixTransform3D(data.GetMatrix3D());
                }
            }
            else if (e.PropertyName == nameof(this.visualizationObject.Source))
            {
                this.LoadModel();
            }
        }

        private void LoadModel()
        {
            lock (this)
            {
                if (this.visualizationObject?.Source != null && this.root.Content == null)
                {
                    HelixToolkit.Wpf.StLReader reader = new HelixToolkit.Wpf.StLReader();
                    var model = reader.Read(this.visualizationObject.Source);
                    model.Freeze();
                    this.root.Content = model;
                }
            }
        }

        private void ChangeMaterials(Model3DCollection children, Material material)
        {
            foreach (var child in children)
            {
                if (child is GeometryModel3D)
                {
                    ((GeometryModel3D)child).Material = material;
                }

                if (child is Model3DGroup)
                {
                    this.ChangeMaterials(((Model3DGroup)child).Children, material);
                }
            }
        }
    }
}
