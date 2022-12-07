// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit.Visualization
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.MixedReality.StereoKit;
    using Microsoft.Psi.Visualization.DataTypes;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a visualization object for <see cref="Hand"/>.
    /// </summary>
    [VisualizationObject("Hand")]
    public class HandVisualizationObject : ModelVisual3DVisualizationObject<Hand>
    {
        private static readonly Dictionary<(HandJointIndex, HandJointIndex), bool> BoneEdges = Hand.Bones.ToDictionary(j => j, j => true);

        private bool showTrackedOnly = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandVisualizationObject"/> class.
        /// </summary>
        public HandVisualizationObject()
        {
            this.Joints = new ()
            {
                EdgeDiameterMm = 10,
                NodeRadiusMm = 7,
                NodeColor = Colors.Silver,
                EdgeColor = Colors.Gray,
            };

            this.Joints.RegisterChildPropertyChangedNotifications(this, nameof(this.Joints));
            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to only show the hand when it is tracked.
        /// </summary>
        [DataMember]
        [Description("Indicates whether to only show tracked hands.")]
        public bool ShowTrackedOnly
        {
            get { return this.showTrackedOnly; }
            set { this.Set(nameof(this.ShowTrackedOnly), ref this.showTrackedOnly, value); }
        }

        /// <summary>
        /// Gets the graph visualization object for the joints.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [Description("Properties of the hand's joints.")]
        public Point3DGraphVisualizationObject<HandJointIndex> Joints { get; }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Visible) ||
                propertyName == nameof(this.ShowTrackedOnly))
            {
                this.UpdateVisibility();
            }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.UpdateJoints();
            this.UpdateVisibility();
        }

        private static Graph<HandJointIndex, Point3D, bool> CreateJointGraph(Hand hand)
        {
            if (hand is null)
            {
                return null;
            }

            var jointNodes = hand.Joints
                .Select<CoordinateSystem, (int JointIndex, Point3D? JointPosition)>((j, i) => (i, j?.Origin))
                .Where(tuple => tuple.JointPosition.HasValue)
                .ToDictionary(tuple => (HandJointIndex)tuple.JointIndex, tuple => tuple.JointPosition.Value);

            return new Graph<HandJointIndex, Point3D, bool>(jointNodes, BoneEdges);
        }

        private void UpdateJoints()
        {
            this.Joints.SetCurrentValue(this.SynthesizeMessage(CreateJointGraph(this.CurrentData)));
        }

        private void UpdateVisibility()
        {
            var visible = this.Visible && this.CurrentData is not null;
            this.UpdateChildVisibility(this.Joints.ModelView, this.ShowTrackedOnly ? visible && this.CurrentData.IsTracked : visible);
        }
    }
}
