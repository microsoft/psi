// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a scatter coordinate system visual.
    /// </summary>
    public class ScatterCoordinateSystemVisual : ModelVisual3D
    {
        private ScatterCoordinateSystemsVisualizationObject visualizationObject;
        private List<Tuple<ArrowVisual3D, ArrowVisual3D, ArrowVisual3D>> axes = new List<Tuple<ArrowVisual3D, ArrowVisual3D, ArrowVisual3D>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterCoordinateSystemVisual"/> class.
        /// </summary>
        /// <param name="visualizationObject">The scatter coordinate system visualization object.</param>
        public ScatterCoordinateSystemVisual(ScatterCoordinateSystemsVisualizationObject visualizationObject)
        {
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
        }

        private void AddAxes()
        {
            var arrowVisualX = new ArrowVisual3D()
            {
                Fill = System.Windows.Media.Brushes.Red
            };
            var arrowVisualY = new ArrowVisual3D()
            {
                Fill = System.Windows.Media.Brushes.Green
            };
            var arrowVisualZ = new ArrowVisual3D()
            {
                Fill = System.Windows.Media.Brushes.Blue
            };
            this.axes.Add(Tuple.Create(arrowVisualX, arrowVisualY, arrowVisualZ));
            this.Children.Add(arrowVisualX);
            this.Children.Add(arrowVisualY);
            this.Children.Add(arrowVisualZ);
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var coordinateSystems = this.visualizationObject.CurrentValue.GetValueOrDefault().Data;
            if (coordinateSystems != null)
            {
                for (int i = this.axes.Count; i < coordinateSystems.Count; i++)
                {
                    this.AddAxes();
                }

                for (int i = 0; i < coordinateSystems.Count; i++)
                {
                    // get the coordinate system
                    var size = this.visualizationObject.Configuration.Size;
                    var cs = coordinateSystems[i];
                    var x = cs.Origin + (size * cs.XAxis.Normalize());
                    var y = cs.Origin + (size * cs.YAxis.Normalize());
                    var z = cs.Origin + (size * cs.ZAxis.Normalize());
                    this.axes[i].Item1.Point1 = new Point3D(cs.Origin.X, cs.Origin.Y, cs.Origin.Z);
                    this.axes[i].Item1.Point2 = new Point3D(x.X, x.Y, x.Z);
                    this.axes[i].Item1.Visible = true;
                    this.axes[i].Item1.Diameter = size * 0.2;
                    this.axes[i].Item2.Point1 = new Point3D(cs.Origin.X, cs.Origin.Y, cs.Origin.Z);
                    this.axes[i].Item2.Point2 = new Point3D(y.X, y.Y, y.Z);
                    this.axes[i].Item2.Visible = true;
                    this.axes[i].Item2.Diameter = size * 0.2;
                    this.axes[i].Item3.Point1 = new Point3D(cs.Origin.X, cs.Origin.Y, cs.Origin.Z);
                    this.axes[i].Item3.Point2 = new Point3D(z.X, z.Y, z.Z);
                    this.axes[i].Item3.Visible = true;
                    this.axes[i].Item3.Diameter = size * 0.2;
                }
            }

            for (int i = (coordinateSystems == null) ? 0 : coordinateSystems.Count; i < this.axes.Count; i++)
            {
                this.axes[i].Item1.Visible = false;
                this.axes[i].Item2.Visible = false;
                this.axes[i].Item3.Visible = false;
            }
        }
    }
}
