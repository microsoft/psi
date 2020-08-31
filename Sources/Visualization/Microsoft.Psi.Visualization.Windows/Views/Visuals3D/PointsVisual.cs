// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a points visual.
    /// </summary>
    public class PointsVisual : ModelVisual3D
    {
        private readonly Points3DVisualizationObject visualizationObject;
        private readonly List<SphereVisual3D> spheres = new List<SphereVisual3D>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PointsVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The points 3D visualization object.</param>
        public PointsVisual(Points3DVisualizationObject visualizationObject)
        {
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
        }

        private void UpdateProperties()
        {
            foreach (var sphere in this.spheres)
            {
                sphere.Radius = this.visualizationObject.RadiusCm * 0.01;
                sphere.Material = new DiffuseMaterial(new SolidColorBrush(this.visualizationObject.Color));
            }
        }

        private void UpdatePoints(List<Point3D> points)
        {
            if (points != null)
            {
                while (this.spheres.Count < points.Count)
                {
                    var sphere = new SphereVisual3D()
                    {
                        Radius = this.visualizationObject.RadiusCm * 0.01,
                        Material = new DiffuseMaterial(new SolidColorBrush(this.visualizationObject.Color)),
                    };
                    this.Children.Add(sphere);
                    this.spheres.Add(sphere);
                }

                for (int i = 0; i < this.spheres.Count; i++)
                {
                    if (i < points.Count)
                    {
                        var point = points[i];
                        this.spheres[i].Transform = new TranslateTransform3D(point.X, point.Y, point.Z);
                        this.spheres[i].Visible = true;
                    }
                    else
                    {
                        this.spheres[i].Visible = false;
                    }
                }
            }
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Points3DVisualizationObject.CurrentValue))
            {
                this.UpdatePoints(this.visualizationObject.CurrentValue.GetValueOrDefault().Data);
            }
            else if (e.PropertyName == nameof(Points3DVisualizationObject.Color) || e.PropertyName == nameof(Points3DVisualizationObject.RadiusCm))
            {
                this.UpdateProperties();
            }
        }
    }
}
