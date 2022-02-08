// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a 3D skeleton visualization object.
    /// </summary>
    /// <typeparam name="TJoint">The type of joints in the skeleton.</typeparam>
    public class SkeletonVisualizationObject<TJoint> : Point3DGraphVisualizationObject<TJoint>
    {
        private double inferredJointsOpacity = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkeletonVisualizationObject{TJoint}"/> class.
        /// </summary>
        /// <param name="nodeVisibilityFunc">An optional function that computes node visibility.</param>
        /// <param name="nodeFillFunc">An optional function that computes the node fill brush.</param>
        /// <param name="edgeVisibilityFunc">An optional function that computes edge visibility.</param>
        /// <param name="edgeFillFunc">An optional function that computes the edge fill brush.</param>
        public SkeletonVisualizationObject(
            Func<TJoint, bool> nodeVisibilityFunc = null,
            Func<TJoint, Brush> nodeFillFunc = null,
            Func<(TJoint, TJoint), bool> edgeVisibilityFunc = null,
            Func<(TJoint, TJoint), Brush> edgeFillFunc = null)
            : base(nodeVisibilityFunc, nodeFillFunc, edgeVisibilityFunc, edgeFillFunc)
        {
        }

        /// <inheritdoc/>
        [PropertyOrder(1)]
        [DisplayName("Color")]
        [Description("Color of the skeleton.")]
        public override Color NodeColor
        {
            get => base.NodeColor;
            set
            {
                base.NodeColor = value;
                base.EdgeColor = value;
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        public override Color EdgeColor { get => base.EdgeColor; set => base.EdgeColor = value; }

        /// <inheritdoc/>
        [PropertyOrder(2)]
        [DisplayName("Bone diameter (mm)")]
        [Description("Diameter of bones (mm).")]
        public override double EdgeDiameterMm { get => base.EdgeDiameterMm; set => base.EdgeDiameterMm = value; }

        /// <inheritdoc/>
        [PropertyOrder(3)]
        [DisplayName("Joint radius (mm)")]
        [Description("Radius of joints (mm).")]
        public override double NodeRadiusMm { get => base.NodeRadiusMm; set => base.NodeRadiusMm = value; }

        /// <summary>
        /// Gets or sets the inferred joints opacity.
        /// </summary>
        [DataMember]
        [PropertyOrder(4)]
        [Description("Opacity for rendering inferred joints and bones.")]
        public double InferredJointsOpacity
        {
            get { return this.inferredJointsOpacity; }
            set { this.Set(nameof(this.InferredJointsOpacity), ref this.inferredJointsOpacity, value); }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.InferredJointsOpacity))
            {
                this.UpdateVisuals();
            }
            else
            {
                base.NotifyPropertyChanged(propertyName);
            }
        }
    }
}
