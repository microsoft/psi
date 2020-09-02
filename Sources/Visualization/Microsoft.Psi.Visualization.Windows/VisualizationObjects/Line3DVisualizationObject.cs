// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents a 3D line visualization object.
    /// </summary>
    [VisualizationObject("3D Line")]
    public class Line3DVisualizationObject : ModelVisual3DVisualizationObject<Line3D>
    {
        private static readonly int PipeDiv = 7;

        private Color color = Colors.White;
        private double thickness = 15;
        private double opacity = 100;

        // The edges that make up the 3D rectangle
        private PipeVisual3D line3D;

        /// <summary>
        /// Initializes a new instance of the <see cref="Line3DVisualizationObject"/> class.
        /// </summary>
        public Line3DVisualizationObject()
        {
            this.CreateLine3D();
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the thickness.
        /// </summary>
        [DataMember]
        public double Thickness
        {
            get { return this.thickness; }
            set { this.Set(nameof(this.Thickness), ref this.thickness, value); }
        }

        /// <summary>
        /// Gets or sets the line opacity.
        /// </summary>
        [DataMember]
        public double Opacity
        {
            get { return this.opacity; }
            set { this.Set(nameof(this.Opacity), ref this.opacity, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != default)
            {
                this.line3D.Point1 = new System.Windows.Media.Media3D.Point3D(this.CurrentData.StartPoint.X, this.CurrentData.StartPoint.Y, this.CurrentData.StartPoint.Z);
                this.line3D.Point2 = new System.Windows.Media.Media3D.Point3D(this.CurrentData.EndPoint.X, this.CurrentData.EndPoint.Y, this.CurrentData.EndPoint.Z);
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            // Check if the changed property is one that require updating the lines in the image.
            if (propertyName == nameof(this.Color) ||
                propertyName == nameof(this.Opacity) ||
                propertyName == nameof(this.Thickness))
            {
                this.UpdateLineProperties();
            }

            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void CreateLine3D()
        {
            // Create the edges
            this.line3D = new PipeVisual3D() { ThetaDiv = PipeDiv };

            // Set the color, thickness, opacity
            this.UpdateLineProperties();
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.line3D, this.Visible);
        }

        private void UpdateLineProperties()
        {
            double opacity = Math.Max(0, Math.Min(100, this.Opacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.Color.R,
                this.Color.G,
                this.Color.B);

            this.line3D.Diameter = this.Thickness / 1000.0;
            this.line3D.Fill = new SolidColorBrush(alphaColor);
        }
    }
}
