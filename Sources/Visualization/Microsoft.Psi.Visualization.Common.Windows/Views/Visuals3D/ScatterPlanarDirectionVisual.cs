// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a scatter planar direction visual.
    /// </summary>
    public class ScatterPlanarDirectionVisual : ModelVisual3D
    {
        private ScatterPlanarDirectionVisualizationObject visualizationObject;
        private List<PieSliceVisual3D> pieSlices = new List<PieSliceVisual3D>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterPlanarDirectionVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The scatter planar direction visualization object.</param>
        public ScatterPlanarDirectionVisual(ScatterPlanarDirectionVisualizationObject visualizationObject)
        {
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
            this.UpdateProperties();
        }

        private void AddPlane()
        {
            var pieSlice = new PieSliceVisual3D()
            {
                StartAngle = 0,
                EndAngle = 90,
                InnerRadius = 0.1,
                OuterRadius = 0.4,
                Material = new DiffuseMaterial(new SolidColorBrush(this.visualizationObject.Color)),
                BackMaterial = new DiffuseMaterial(new SolidColorBrush(this.visualizationObject.Color)),
            };
            this.pieSlices.Add(pieSlice);
            this.Children.Add(pieSlice);

            this.UpdateProperties();
        }

        private void ClearAll()
        {
            this.pieSlices.Clear();
            this.Children.Clear();
        }

        private void UpdateProperties()
        {
            var brush = new SolidColorBrush(this.visualizationObject.Color);
            brush.Opacity = 0.3;
            foreach (var pieSlice in this.pieSlices)
            {
                pieSlice.Material = new DiffuseMaterial(brush);
                pieSlice.BackMaterial = pieSlice.Material;
                pieSlice.OuterRadius = this.visualizationObject.Size;
            }
        }

        private void Update()
        {
            var data = this.visualizationObject.CurrentValue.GetValueOrDefault().Data;
            if (data != null)
            {
                // if we have more people than we need, clear all
                if (data.Count < this.pieSlices.Count)
                {
                    this.ClearAll();
                }

                // add planes if we don't have enough
                for (int i = this.pieSlices.Count; i < data.Count; i++)
                {
                    this.AddPlane();
                }

                int j = 0;
                foreach (var coordinateSystem in data)
                {
                    this.pieSlices[j].Center = coordinateSystem.Origin.ToPoint3D();
                    this.pieSlices[j].Normal = coordinateSystem.XAxis.ToVector3D();
                    j++;
                }
            }
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScatterPlanarDirectionVisualizationObject.Size) ||
                e.PropertyName == nameof(ScatterPlanarDirectionVisualizationObject.Color))
            {
                this.UpdateProperties();
            }

            this.Update();
        }
    }
}
