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
    /// Implements a 3D point visualization object.
    /// </summary>
    [VisualizationObject("3D Sphere")]
    public class Point3DAsSphereVisualizationObject : ModelVisual3DVisualizationObject<Point3D?>
    {
        private SphereVisual3D sphereVisual;
        private Color color = Colors.White;
        private double radiusCm = 2;
        private double opacity = 100;
        private int sphereDiv = 7;

        /// <summary>
        /// Initializes a new instance of the <see cref="Point3DAsSphereVisualizationObject"/> class.
        /// </summary>
        public Point3DAsSphereVisualizationObject()
        {
            this.sphereVisual = new SphereVisual3D();

            this.UpdatePointProperties();
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("The color of the point(s).")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the point(s) in centimeters.
        /// </summary>
        [DataMember]
        [DisplayName("Radius (cm)")]
        [Description("The radius of the point(s) in centimeters.")]
        public double RadiusCm
        {
            get { return this.radiusCm; }
            set { this.Set(nameof(this.RadiusCm), ref this.radiusCm, value); }
        }

        /// <summary>
        /// Gets or sets the opacity of the point(s).
        /// </summary>
        [DataMember]
        [Description("The opacity of the point(s).")]
        public double Opacity
        {
            get { return this.opacity; }
            set { this.Set(nameof(this.Opacity), ref this.opacity, value); }
        }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering the sphere.
        /// </summary>
        [DataMember]
        [Description("Number of divisions to use when rendering each point as a sphere (minimum value is 3).")]
        public int SphereDivisions
        {
            get { return this.sphereDiv; }
            set { this.Set(nameof(this.SphereDivisions), ref this.sphereDiv, value < 3 ? 3 : value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData.HasValue)
            {
                this.sphereVisual.Transform = new TranslateTransform3D(this.CurrentData.Value.X, this.CurrentData.Value.Y, this.CurrentData.Value.Z);
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            // Check if the changed property is one that require updating the lines in the image.
            if (propertyName == nameof(this.Color) ||
                propertyName == nameof(this.Opacity) ||
                propertyName == nameof(this.RadiusCm) ||
                propertyName == nameof(this.SphereDivisions))
            {
                this.UpdatePointProperties();
            }

            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.sphereVisual, this.Visible && this.CurrentData.HasValue);
        }

        private void UpdatePointProperties()
        {
            double opacity = Math.Max(0, Math.Min(100, this.Opacity));
            var alphaColor = Color.FromArgb(
                (byte)(opacity * 2.55),
                this.Color.R,
                this.Color.G,
                this.Color.B);

            this.sphereVisual.Fill = new SolidColorBrush(alphaColor);
            this.sphereVisual.Radius = this.RadiusCm * 0.01;
            this.sphereVisual.PhiDiv = this.sphereDiv;
            this.sphereVisual.ThetaDiv = this.sphereDiv;
        }
    }
}
