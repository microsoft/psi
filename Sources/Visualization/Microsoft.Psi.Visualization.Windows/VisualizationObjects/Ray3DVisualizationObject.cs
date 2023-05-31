// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using mathNet = MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements a Ray3D visualization object.
    /// </summary>
    [VisualizationObject("Ray 3D")]
    public class Ray3DVisualizationObject : ModelVisual3DVisualizationObject<mathNet.Ray3D?>
    {
        private const int ThetaDivDefault = 5;

        private readonly ArrowVisual3D rayArrow;

        private Color color = Colors.Orange;
        private double thicknessMm = 15;
        private double lengthCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ray3DVisualizationObject"/> class.
        /// </summary>
        public Ray3DVisualizationObject()
        {
            this.rayArrow = new ArrowVisual3D() { ThetaDiv = ThetaDivDefault, Fill = new SolidColorBrush(this.Color), Diameter = this.ThicknessMm / 1000.0 };

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("The color of the ray.")]
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
        [Description("The ray diameter in millimeters.")]
        public double ThicknessMm
        {
            get { return this.thicknessMm; }
            set { this.Set(nameof(this.ThicknessMm), ref this.thicknessMm, value); }
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        [DataMember]
        [DisplayName("Length (cm)")]
        [Description("The ray length in centimeters.")]
        public double LengthCm
        {
            get { return this.lengthCm; }
            set { this.Set(nameof(this.LengthCm), ref this.lengthCm, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.UpdateRay();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Color))
            {
                this.rayArrow.Fill = new SolidColorBrush(this.Color);
            }
            else if (propertyName == nameof(this.ThicknessMm))
            {
                this.rayArrow.Diameter = this.ThicknessMm / 1000.0;
            }
            else if (propertyName == nameof(this.LengthCm))
            {
                this.UpdateRay();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateRay()
        {
            if (this.CurrentData != null && this.CurrentData.HasValue)
            {
                var endPoint = this.CurrentData.Value.ThroughPoint + ((this.LengthCm / 100.0) * this.CurrentData.Value.Direction);

                this.rayArrow.BeginEdit();
                this.rayArrow.Point1 = new Point3D(this.CurrentData.Value.ThroughPoint.X, this.CurrentData.Value.ThroughPoint.Y, this.CurrentData.Value.ThroughPoint.Z);
                this.rayArrow.Point2 = new Point3D(endPoint.X, endPoint.Y, endPoint.Z);
                this.rayArrow.EndEdit();
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.rayArrow, this.Visible && this.CurrentData != null);
        }
    }
}
