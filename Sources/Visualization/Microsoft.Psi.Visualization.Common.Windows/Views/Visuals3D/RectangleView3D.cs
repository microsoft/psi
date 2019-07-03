// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Visualization.Config;

    /// <summary>
    /// Represents a 3D rectangle view.
    /// </summary>
    public class RectangleView3D : ModelVisual3D, IView3D<Rect3D, Rectangle3DVisualizationObjectConfiguration>
    {
        private static readonly int PipeDiv = 7;
        private readonly PipeVisual3D[] edges;
        private Rect3D currentRectangle = default(Rect3D);

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleView3D"/> class.
        /// </summary>
        public RectangleView3D()
        {
            var initConfiguration = new Rectangle3DVisualizationObjectConfiguration();

            this.edges = new PipeVisual3D[12];
            for (int i = 0; i < this.edges.Length; i++)
            {
                this.edges[i] = new PipeVisual3D() { ThetaDiv = PipeDiv };
            }

            this.UpdateConfiguration(initConfiguration);
        }

        /// <inheritdoc/>
        public void UpdateData(Rect3D rectangle, DateTime originatingTime)
        {
            if (rectangle == default(Rect3D))
            {
                this.ClearAll();
                return;
            }

            rectangle.DeepClone(ref this.currentRectangle);

            var p0 = new Point3D(rectangle.Location.X, rectangle.Location.Y, rectangle.Location.Z);
            var p1 = new Point3D(rectangle.Location.X + rectangle.SizeX, rectangle.Location.Y, rectangle.Location.Z);
            var p2 = new Point3D(rectangle.Location.X + rectangle.SizeX, rectangle.Location.Y + rectangle.SizeY, rectangle.Location.Z);
            var p3 = new Point3D(rectangle.Location.X, rectangle.Location.Y + rectangle.SizeY, rectangle.Location.Z);
            var p4 = new Point3D(rectangle.Location.X, rectangle.Location.Y, rectangle.Location.Z + rectangle.SizeZ);
            var p5 = new Point3D(rectangle.Location.X + rectangle.SizeX, rectangle.Location.Y, rectangle.Location.Z + rectangle.SizeZ);
            var p6 = new Point3D(rectangle.Location.X + rectangle.SizeX, rectangle.Location.Y + rectangle.SizeY, rectangle.Location.Z + rectangle.SizeZ);
            var p7 = new Point3D(rectangle.Location.X, rectangle.Location.Y + rectangle.SizeY, rectangle.Location.Z + rectangle.SizeZ);

            this.edges[0].Point1 = p0;
            this.edges[0].Point2 = p1;
            this.edges[1].Point1 = p1;
            this.edges[1].Point2 = p2;
            this.edges[2].Point1 = p2;
            this.edges[2].Point2 = p3;
            this.edges[3].Point1 = p3;
            this.edges[3].Point2 = p0;
            this.edges[4].Point1 = p4;
            this.edges[4].Point2 = p5;
            this.edges[5].Point1 = p5;
            this.edges[5].Point2 = p6;
            this.edges[6].Point1 = p6;
            this.edges[6].Point2 = p7;
            this.edges[7].Point1 = p7;
            this.edges[7].Point2 = p4;
            this.edges[8].Point1 = p0;
            this.edges[8].Point2 = p4;
            this.edges[9].Point1 = p1;
            this.edges[9].Point2 = p5;
            this.edges[10].Point1 = p2;
            this.edges[10].Point2 = p6;
            this.edges[11].Point1 = p3;
            this.edges[11].Point2 = p7;

            if (!this.Children.Contains(this.edges[0]))
            {
                foreach (var line in this.edges)
                {
                    this.Children.Add(line);
                }
            }
        }

        /// <inheritdoc/>
        public void UpdateConfiguration(Rectangle3DVisualizationObjectConfiguration newConfig)
        {
            double opacity = Math.Max(0, Math.Min(100, newConfig.Opacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                newConfig.Color.R,
                newConfig.Color.G,
                newConfig.Color.B);

            foreach (var line in this.edges)
            {
                line.Diameter = newConfig.Thickness / 1000.0;
                line.Fill = new SolidColorBrush(alphaColor);
            }

            this.UpdateData(this.currentRectangle, default(DateTime));
        }

        /// <inheritdoc/>
        public void ClearAll()
        {
            this.Children.Clear();
        }
    }
}
