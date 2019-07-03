// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Config;
    using Win3D=System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a coordinate system visual.
    /// </summary>
    public class CoordinateSystemView3D : Win3D.ModelVisual3D, IView3D<CoordinateSystem, CoordinateSystemVisualizationObjectConfiguration>
    {
        private (ArrowVisual3D, ArrowVisual3D, ArrowVisual3D) axes;
        private CoordinateSystemVisualizationObjectConfiguration currentConfiguration;
        private CoordinateSystem currentCoordinateSystem = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemView3D"/> class.
        /// </summary>
        public CoordinateSystemView3D()
        {
            var arrowVisualX = new ArrowVisual3D()
            {
                Fill = System.Windows.Media.Brushes.Red,
            };
            var arrowVisualY = new ArrowVisual3D()
            {
                Fill = System.Windows.Media.Brushes.Green,
            };
            var arrowVisualZ = new ArrowVisual3D()
            {
                Fill = System.Windows.Media.Brushes.Blue,
            };

            this.axes = (arrowVisualX, arrowVisualY, arrowVisualZ);
            this.currentConfiguration = new CoordinateSystemVisualizationObjectConfiguration();
        }

        /// <inheritdoc/>
        public void UpdateData(CoordinateSystem cs, DateTime originatingTime)
        {
            if (cs == null)
            {
                this.ClearAll();
                return;
            }

            if (this.currentCoordinateSystem == null)
            {
                this.currentCoordinateSystem = new CoordinateSystem();
            }

            cs.CopyTo(this.currentCoordinateSystem);

            var size = this.currentConfiguration.Size / 1000.0;
            var x = cs.Origin + (size * cs.XAxis.Normalize());
            var y = cs.Origin + (size * cs.YAxis.Normalize());
            var z = cs.Origin + (size * cs.ZAxis.Normalize());
            this.axes.Item1.Point1 = new Win3D.Point3D(cs.Origin.X, cs.Origin.Y, cs.Origin.Z);
            this.axes.Item1.Point2 = new Win3D.Point3D(x.X, x.Y, x.Z);
            this.axes.Item1.Diameter = size * 0.2;
            this.axes.Item2.Point1 = new Win3D.Point3D(cs.Origin.X, cs.Origin.Y, cs.Origin.Z);
            this.axes.Item2.Point2 = new Win3D.Point3D(y.X, y.Y, y.Z);
            this.axes.Item2.Diameter = size * 0.2;
            this.axes.Item3.Point1 = new Win3D.Point3D(cs.Origin.X, cs.Origin.Y, cs.Origin.Z);
            this.axes.Item3.Point2 = new Win3D.Point3D(z.X, z.Y, z.Z);
            this.axes.Item3.Diameter = size * 0.2;

            if (!this.Children.Contains(this.axes.Item1))
            {
                this.Children.Add(this.axes.Item1);
                this.Children.Add(this.axes.Item2);
                this.Children.Add(this.axes.Item3);
            }
        }

        /// <inheritdoc/>
        public void UpdateConfiguration(CoordinateSystemVisualizationObjectConfiguration newConfig)
        {
            this.currentConfiguration = newConfig;
            this.UpdateData(this.currentCoordinateSystem, default(DateTime));
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            this.Children.Clear();
            this.currentCoordinateSystem = null;
        }
    }
}
