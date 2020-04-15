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
        private Points3DVisualizationObject visualizationObject;
        private List<SphereVisual3D> spheres = new List<SphereVisual3D>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PointsVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The points 3D visualization object.</param>
        public PointsVisual(Points3DVisualizationObject visualizationObject)
        {
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
        }

        private void AddSphere()
        {
            var sphere = new SphereVisual3D() { Radius = .05 };
            this.Children.Add(sphere);
            this.spheres.Add(sphere);
        }

        private void UpdateProperties()
        {
            var brush = new SolidColorBrush(this.visualizationObject.Color);

            foreach (var sphere in this.spheres)
            {
                sphere.Radius = this.visualizationObject.Size;
                sphere.Fill = brush;
            }
        }

        private void UpdatePoints(List<Point3D> points)
        {
            if (points != null)
            {
                while (this.spheres.Count < points.Count)
                {
                    this.AddSphere();
                }

                for (int i = 0; i < this.spheres.Count; i++)
                {
                    if (i < points.Count)
                    {
                        var point = points[i];
                        this.spheres[i].Transform = new TranslateTransform3D(-point.Z, point.X, point.Y);
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
            else if (e.PropertyName == nameof(Points3DVisualizationObject.Color) || e.PropertyName == nameof(Points3DVisualizationObject.Size))
            {
                this.UpdateProperties();
            }
        }
    }
}
