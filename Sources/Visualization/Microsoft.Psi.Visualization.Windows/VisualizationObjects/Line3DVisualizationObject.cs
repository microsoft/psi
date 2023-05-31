// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements a 3D line visualization object.
    /// </summary>
    [VisualizationObject("3D Line")]
    public class Line3DVisualizationObject : ModelVisual3DVisualizationObject<Line3D?>
    {
        private Color color = Colors.White;
        private double thicknessMm = 15;
        private double opacity = 100;
        private int pipeDiv = 7;

        // The edges that make up the 3D rectangle
        private PipeVisual3D line3D;

        /// <summary>
        /// Initializes a new instance of the <see cref="Line3DVisualizationObject"/> class.
        /// </summary>
        public Line3DVisualizationObject()
        {
            this.line3D = new PipeVisual3D();

            this.UpdateLineProperties();
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("The color of the line(s).")]
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
        [Description("The thickness of the line(s) in millimeters.")]
        public double ThicknessMm
        {
            get { return this.thicknessMm; }
            set { this.Set(nameof(this.ThicknessMm), ref this.thicknessMm, value); }
        }

        /// <summary>
        /// Gets or sets the line opacity.
        /// </summary>
        [DataMember]
        [Description("The opacity of the line(s).")]
        public double Opacity
        {
            get { return this.opacity; }
            set { this.Set(nameof(this.Opacity), ref this.opacity, value); }
        }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering the line as a pipe.
        /// </summary>
        [DataMember]
        [Description("Number of divisions to use when rendering each line as a pipe (minimum value is 3).")]
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
                this.line3D.Point1 = new System.Windows.Media.Media3D.Point3D(this.CurrentData.Value.StartPoint.X, this.CurrentData.Value.StartPoint.Y, this.CurrentData.Value.StartPoint.Z);
                this.line3D.Point2 = new System.Windows.Media.Media3D.Point3D(this.CurrentData.Value.EndPoint.X, this.CurrentData.Value.EndPoint.Y, this.CurrentData.Value.EndPoint.Z);
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

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.line3D, this.Visible && this.CurrentData.HasValue);
        }

        private void UpdateLineProperties()
        {
            double opacity = Math.Max(0, Math.Min(100, this.Opacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.Color.R,
                this.Color.G,
                this.Color.B);

            this.line3D.Diameter = this.ThicknessMm / 1000.0;
            this.line3D.ThetaDiv = this.PipeDivisions;
            this.line3D.Fill = new SolidColorBrush(alphaColor);
        }
    }
}
