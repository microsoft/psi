// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a coordinate system visualization object.
    /// </summary>
    [VisualizationObject("Coordinate System")]
    public class CoordinateSystemVisualizationObject : ModelVisual3DVisualizationObject<CoordinateSystem>
    {
        private const int XAxisIndex = 0;
        private const int YAxisIndex = 1;
        private const int ZAxisIndex = 2;
        private const int ThetaDivDefault = 5;

        private readonly ArrowVisual3D[] axes =
        {
            new ArrowVisual3D() { ThetaDiv = ThetaDivDefault, Fill = Brushes.Red },
            new ArrowVisual3D() { ThetaDiv = ThetaDivDefault, Fill = Brushes.Green },
            new ArrowVisual3D() { ThetaDiv = ThetaDivDefault, Fill = Brushes.Blue },
        };

        private double length = .2;
        private double thickness = .015;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVisualizationObject"/> class.
        /// </summary>
        public CoordinateSystemVisualizationObject()
        {
            this.UpdateThickness();
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the length of the coordinate system axes.
        /// </summary>
        [DataMember]
        [DisplayName("Axes length (m)")]
        [Description("Length of coordinate system axes (m).")]
        public double Length
        {
            get { return this.length; }
            set { this.Set(nameof(this.Length), ref this.length, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the coordinate system axes.
        /// </summary>
        [DataMember]
        [DisplayName("Axes thickness (m)")]
        [Description("Thickness of coordinate system axes (m).")]
        public double Thickness
        {
            get { return this.thickness; }
            set { this.Set(nameof(this.Thickness), ref this.thickness, value); }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Length) || propertyName == nameof(this.Thickness))
            {
                this.UpdateThickness();
                this.UpdateData();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                var x = this.CurrentData.Origin + (this.Length * this.CurrentData.XAxis.Normalize());
                var y = this.CurrentData.Origin + (this.Length * this.CurrentData.YAxis.Normalize());
                var z = this.CurrentData.Origin + (this.Length * this.CurrentData.ZAxis.Normalize());

                this.axes[XAxisIndex].BeginEdit();
                this.axes[XAxisIndex].Point1 = new Win3D.Point3D(this.CurrentData.Origin.X, this.CurrentData.Origin.Y, this.CurrentData.Origin.Z);
                this.axes[XAxisIndex].Point2 = new Win3D.Point3D(x.X, x.Y, x.Z);
                this.axes[XAxisIndex].EndEdit();

                this.axes[YAxisIndex].BeginEdit();
                this.axes[YAxisIndex].Point1 = new Win3D.Point3D(this.CurrentData.Origin.X, this.CurrentData.Origin.Y, this.CurrentData.Origin.Z);
                this.axes[YAxisIndex].Point2 = new Win3D.Point3D(y.X, y.Y, y.Z);
                this.axes[YAxisIndex].EndEdit();

                this.axes[ZAxisIndex].BeginEdit();
                this.axes[ZAxisIndex].Point1 = new Win3D.Point3D(this.CurrentData.Origin.X, this.CurrentData.Origin.Y, this.CurrentData.Origin.Z);
                this.axes[ZAxisIndex].Point2 = new Win3D.Point3D(z.X, z.Y, z.Z);
                this.axes[ZAxisIndex].EndEdit();
            }

            this.UpdateVisibility();
        }

        private void UpdateThickness()
        {
            foreach (ArrowVisual3D axis in this.axes)
            {
                axis.Diameter = this.Thickness;
            }
        }

        private void UpdateVisibility()
        {
            foreach (ArrowVisual3D axis in this.axes)
            {
                this.UpdateChildVisibility(axis, this.Visible && this.CurrentData != null);
            }
        }
    }
}