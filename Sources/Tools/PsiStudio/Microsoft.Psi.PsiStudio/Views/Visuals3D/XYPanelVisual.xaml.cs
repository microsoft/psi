// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.ComponentModel;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for XYPanelVisual.xaml
    /// </summary>
    public partial class XYPanelVisual : ModelVisual3D
    {
        private XYPanel3DVisualizationObject visualizationObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYPanelVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The XY panel 3D visualization object.</param>
        public XYPanelVisual( XYPanel3DVisualizationObject visualizationObject)
        {
            this.InitializeComponent();
            this.RootHost2DIn3D.DataContext = this.visualizationObject = visualizationObject;
            this.SetPosition();
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
            this.visualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
        }

        private void SetPosition()
        {
            var positions = new Point3DCollection();
            var w = this.visualizationObject.Configuration.Width / 2.0;
            var h = this.visualizationObject.Configuration.Height / 2.0;

            positions.Add(new Point3D(-w, 0, h));
            positions.Add(new Point3D(-w, 0, -h));
            positions.Add(new Point3D(w, 0, -h));
            positions.Add(new Point3D(w, 0, h));
            this.Mesh.Positions = positions;
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XYPanel3DVisualizationObjectConfiguration.Width) || e.PropertyName == nameof(XYPanel3DVisualizationObjectConfiguration.Height))
            {
                this.SetPosition();
            }
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XYPanel3DVisualizationObject.CurrentValue))
            {
                var globalTransform = this.visualizationObject.CurrentValue.GetValueOrDefault().Data;
                if (globalTransform != null)
                {
                    globalTransform = globalTransform.Transform(this.visualizationObject.Configuration.LocalTransform);
                    this.Transform = new MatrixTransform3D(globalTransform.GetMatrix3D());
                }
            }
        }
    }
}
