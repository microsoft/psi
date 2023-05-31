// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;

    /// <summary>
    /// Implements a 3D rectangles visualization object.
    /// </summary>
    [VisualizationObject("3D Rectangle")]
    public class Rect3DVisualizationObject : ModelVisual3DVisualizationObject<Rect3D?>
    {
        private Color color = Colors.White;
        private double thicknessMm = 15;
        private double opacity = 100;
        private int pipeDiv = 7;

        // The edges that make up the 3D rectangle
        private PipeVisual3D[] edges;

        /// <summary>
        /// Initializes a new instance of the <see cref="Rect3DVisualizationObject"/> class.
        /// </summary>
        public Rect3DVisualizationObject()
        {
            this.Create3DRectangle();
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("The color of the rectangle(s).")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        [DataMember]
        [DisplayName("Thickness (mm)")]
        [Description("The thickness of the rectangle edges in millimeters.")]
        public double ThicknessMm
        {
            get { return this.thicknessMm; }
            set { this.Set(nameof(this.ThicknessMm), ref this.thicknessMm, value); }
        }

        /// <summary>
        /// Gets or sets the line opacity.
        /// </summary>
        [DataMember]
        [Description("The opacity of the rectangle(s).")]
        public double Opacity
        {
            get { return this.opacity; }
            set { this.Set(nameof(this.Opacity), ref this.opacity, value); }
        }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering each edge as a pipe.
        /// </summary>
        [DataMember]
        [Description("Number of divisions to use when rendering each rectangle edge as a pipe (minimum value is 3).")]
        public int PipeDivisions
        {
            get { return this.pipeDiv; }
            set { this.Set(nameof(this.PipeDivisions), ref this.pipeDiv, value < 3 ? 3 : value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData.HasValue)
            {
                var p0 = new Point3D(this.CurrentData.Value.Location.X, this.CurrentData.Value.Location.Y, this.CurrentData.Value.Location.Z);
                var p1 = new Point3D(this.CurrentData.Value.Location.X + this.CurrentData.Value.SizeX, this.CurrentData.Value.Location.Y, this.CurrentData.Value.Location.Z);
                var p2 = new Point3D(this.CurrentData.Value.Location.X + this.CurrentData.Value.SizeX, this.CurrentData.Value.Location.Y + this.CurrentData.Value.SizeY, this.CurrentData.Value.Location.Z);
                var p3 = new Point3D(this.CurrentData.Value.Location.X, this.CurrentData.Value.Location.Y + this.CurrentData.Value.SizeY, this.CurrentData.Value.Location.Z);
                var p4 = new Point3D(this.CurrentData.Value.Location.X, this.CurrentData.Value.Location.Y, this.CurrentData.Value.Location.Z + this.CurrentData.Value.SizeZ);
                var p5 = new Point3D(this.CurrentData.Value.Location.X + this.CurrentData.Value.SizeX, this.CurrentData.Value.Location.Y, this.CurrentData.Value.Location.Z + this.CurrentData.Value.SizeZ);
                var p6 = new Point3D(this.CurrentData.Value.Location.X + this.CurrentData.Value.SizeX, this.CurrentData.Value.Location.Y + this.CurrentData.Value.SizeY, this.CurrentData.Value.Location.Z + this.CurrentData.Value.SizeZ);
                var p7 = new Point3D(this.CurrentData.Value.Location.X, this.CurrentData.Value.Location.Y + this.CurrentData.Value.SizeY, this.CurrentData.Value.Location.Z + this.CurrentData.Value.SizeZ);

                this.UpdateLinePosition(this.edges[0], p0, p1);
                this.UpdateLinePosition(this.edges[1], p1, p2);
                this.UpdateLinePosition(this.edges[2], p2, p3);
                this.UpdateLinePosition(this.edges[3], p3, p0);
                this.UpdateLinePosition(this.edges[4], p4, p5);
                this.UpdateLinePosition(this.edges[5], p5, p6);
                this.UpdateLinePosition(this.edges[6], p6, p7);
                this.UpdateLinePosition(this.edges[7], p7, p4);
                this.UpdateLinePosition(this.edges[8], p0, p4);
                this.UpdateLinePosition(this.edges[9], p1, p5);
                this.UpdateLinePosition(this.edges[10], p2, p6);
                this.UpdateLinePosition(this.edges[11], p3, p7);
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            // Check if the changed property is one that require updating the lines in the image.
            if (propertyName == nameof(this.Color) ||
                propertyName == nameof(this.Opacity) ||
                propertyName == nameof(this.ThicknessMm) ||
                propertyName == nameof(this.PipeDivisions))
            {
                this.UpdateLineProperties();
            }

            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void Create3DRectangle()
        {
            // Create the edges
            this.edges = new PipeVisual3D[12];
            for (int i = 0; i < this.edges.Length; i++)
            {
                this.edges[i] = new PipeVisual3D();
            }

            // Set the color, thickness, opacity
            this.UpdateLineProperties();
        }

        private void UpdateLinePosition(Visual3D visual, Point3D point1, Point3D point2)
        {
            PipeVisual3D line = visual as PipeVisual3D;
            line.Point1 = point1;
            line.Point2 = point2;
        }

        private void UpdateVisibility()
        {
            foreach (PipeVisual3D line in this.edges)
            {
                this.UpdateChildVisibility(line, this.Visible && this.CurrentData.HasValue);
            }
        }

        private void UpdateLineProperties()
        {
            double opacity = Math.Max(0, Math.Min(100, this.Opacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.Color.R,
                this.Color.G,
                this.Color.B);

            foreach (PipeVisual3D line in this.edges)
            {
                line.Diameter = this.ThicknessMm / 1000.0;
                line.Fill = new SolidColorBrush(alphaColor);
                line.ThetaDiv = this.PipeDivisions;
            }
        }
    }
}
