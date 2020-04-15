// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Variable should not use Hungarian notation

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a coordinate system visualization object.
    /// </summary>
    [VisualizationObject("Visualize")]
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

        private CoordinateSystem currentData = null;

        private double size = 35;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemVisualizationObject"/> class.
        /// </summary>
        public CoordinateSystemVisualizationObject()
        {
            this.UpdateDiameters();
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        [DataMember]
        public double Size
        {
            get { return this.size; }
            set { this.Set(nameof(this.Size), ref this.size, value); }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Size))
            {
                this.UpdateDiameters();
                this.UpdateData(this.currentData, default);
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <inheritdoc/>
        public override void UpdateData(CoordinateSystem coordinateSystem, DateTime originatingTime)
        {
            if (!Equals(this.currentData, coordinateSystem))
            {
                this.currentData = coordinateSystem;

                if (this.currentData != null)
                {
                    var length = this.Size / 200.0;
                    var x = this.currentData.Origin + (length * this.currentData.XAxis.Normalize());
                    var y = this.currentData.Origin + (length * this.currentData.YAxis.Normalize());
                    var z = this.currentData.Origin + (length * this.currentData.ZAxis.Normalize());

                    this.axes[XAxisIndex].BeginEdit();
                    this.axes[XAxisIndex].Point1 = new Win3D.Point3D(this.currentData.Origin.X, this.currentData.Origin.Y, this.currentData.Origin.Z);
                    this.axes[XAxisIndex].Point2 = new Win3D.Point3D(x.X, x.Y, x.Z);
                    this.axes[XAxisIndex].EndEdit();

                    this.axes[YAxisIndex].BeginEdit();
                    this.axes[YAxisIndex].Point1 = new Win3D.Point3D(this.currentData.Origin.X, this.currentData.Origin.Y, this.currentData.Origin.Z);
                    this.axes[YAxisIndex].Point2 = new Win3D.Point3D(y.X, y.Y, y.Z);
                    this.axes[YAxisIndex].EndEdit();

                    this.axes[ZAxisIndex].BeginEdit();
                    this.axes[ZAxisIndex].Point1 = new Win3D.Point3D(this.currentData.Origin.X, this.currentData.Origin.Y, this.currentData.Origin.Z);
                    this.axes[ZAxisIndex].Point2 = new Win3D.Point3D(z.X, z.Y, z.Z);
                    this.axes[ZAxisIndex].EndEdit();
                }
            }

            this.UpdateVisibility();
        }

        private void UpdateDiameters()
        {
            double size = this.Size / 1000.0;

            foreach (ArrowVisual3D axis in this.axes)
            {
                axis.Diameter = size;
            }
        }

        private void UpdateVisibility()
        {
            foreach (ArrowVisual3D axis in this.axes)
            {
                this.UpdateChildVisibility(axis, this.Visible && this.currentData != null);
            }
        }
    }
}