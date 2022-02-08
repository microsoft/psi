// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a labeled 3D point visualization object.
    /// </summary>
    [VisualizationObject("Labeled 3D Sphere")]
    public class LabeledPoint3DVisualizationObject : ModelVisual3DVisualizationObject<Tuple<string, Point3D>>
    {
        private double billboardHeightCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledPoint3DVisualizationObject"/> class.
        /// </summary>
        public LabeledPoint3DVisualizationObject()
        {
            this.Point = new Point3DAsSphereVisualizationObject();
            this.Point.RegisterChildPropertyChangedNotifications(this, nameof(this.Point));

            this.Billboard = new BillboardTextVisualizationObject();
            this.Billboard.RegisterChildPropertyChangedNotifications(this, nameof(this.Billboard));

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets the point 3D visualization object.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(1)]
        [DisplayName("Point Properties")]
        [Description("The point properties.")]
        public Point3DAsSphereVisualizationObject Point { get; private set; }

        /// <summary>
        /// Gets or sets the height at which to draw the billboard (cm).
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Billboard Height (cm)")]
        [Description("Height at which to draw the billboard (cm).")]
        public double BillboardHeightCm
        {
            get { return this.billboardHeightCm; }
            set { this.Set(nameof(this.BillboardHeightCm), ref this.billboardHeightCm, value); }
        }

        /// <summary>
        /// Gets the billboard visualization object for the labeled point.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Billboard Properties")]
        [Description("The billboard properties.")]
        public BillboardTextVisualizationObject Billboard { get; private set; }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                this.UpdateVisuals();
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            // Check if the changed property is one that require updating the lines in the image.
            if (propertyName == nameof(this.BillboardHeightCm))
            {
                this.UpdateBillboard();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            this.UpdatePoint();
            this.UpdateBillboard();
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Item2;
                var pos = new Win3D.Point3D(origin.X, origin.Y, origin.Z + (this.BillboardHeightCm / 100.0));
                var text = this.CurrentData.Item1.ToString();
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, text)));
            }
        }

        private void UpdatePoint()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Item2;
                Win3D.Point3D? point = new Win3D.Point3D(origin.X, origin.Y, origin.Z);
                this.Point.SetCurrentValue(this.SynthesizeMessage(point));
            }
            else
            {
                this.Point.SetCurrentValue(this.SynthesizeMessage(default(Win3D.Point3D?)));
            }
        }

        private void UpdateVisibility()
        {
            this.UpdatePointVisibility();
            this.UpdateBillboardVisibility();
        }

        private void UpdatePointVisibility()
        {
            this.UpdateChildVisibility(this.Point.ModelView, this.Visible && this.CurrentData != default && this.Point.Visible);
        }

        private void UpdateBillboardVisibility()
        {
            this.UpdateChildVisibility(this.Billboard.ModelView, this.Visible && this.CurrentData != default && this.Billboard.Visible);
        }
    }
}
