// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean.Visualization
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a linear velocity visualization object.
    /// </summary>
    [VisualizationObject("Linear 3D-velocity")]
    public class LinearVelocity3DVisualizationObject : ModelVisual3DVisualizationObject<LinearVelocity3D>
    {
        private const int ThetaDivDefault = 5;

        private readonly ArrowVisual3D velocityArrow;

        private Color color = Colors.Orange;
        private double thicknessMm = 15;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearVelocity3DVisualizationObject"/> class.
        /// </summary>
        public LinearVelocity3DVisualizationObject()
        {
            this.velocityArrow = new ArrowVisual3D() { ThetaDiv = ThetaDivDefault, Fill = new SolidColorBrush(this.Color), Diameter = this.ThicknessMm / 1000.0 };

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("The color of the velocity vector.")]
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
        [Description("The vector diameter in millimeters.")]
        public double ThicknessMm
        {
            get { return this.thicknessMm; }
            set { this.Set(nameof(this.ThicknessMm), ref this.thicknessMm, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.UpdateVelocityVector();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Color))
            {
                this.velocityArrow.Fill = new SolidColorBrush(this.Color);
            }
            else if (propertyName == nameof(this.ThicknessMm))
            {
                this.velocityArrow.Diameter = this.ThicknessMm / 1000.0;
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVelocityVector()
        {
            if (this.CurrentData != null)
            {
                if (this.CurrentData.Magnitude > 0)
                {
                    var endPoint = this.CurrentData.Origin + this.CurrentData.Direction;

                    this.velocityArrow.BeginEdit();
                    this.velocityArrow.Point1 = new Win3D.Point3D(this.CurrentData.Origin.X, this.CurrentData.Origin.Y, this.CurrentData.Origin.Z);
                    this.velocityArrow.Point2 = new Win3D.Point3D(endPoint.X, endPoint.Y, endPoint.Z);
                    this.velocityArrow.EndEdit();
                }
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.velocityArrow, this.Visible && this.CurrentData != null && this.CurrentData.Magnitude > 0);
        }
    }
}
