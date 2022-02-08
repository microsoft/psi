// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect.Visualization
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using Microsoft.Kinect;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using Win3D = System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a visualization object for Kinect bodies.
    /// </summary>
    [VisualizationObject("Kinect Body")]
    public class KinectBodyVisualizationObject : ModelVisual3DVisualizationObject<KinectBody>
    {
        private readonly UpdatableVisual3DDictionary<JointType, SphereVisual3D> visualJoints;
        private readonly UpdatableVisual3DDictionary<(JointType ChildJoint, JointType ParentJoint), PipeVisual3D> visualBones;

        private Color color = Colors.White;
        private double inferredJointsOpacity = 30;
        private double boneDiameterMm = 20;
        private double jointRadiusMm = 15;
        private bool showBillboard = false;
        private int polygonResolution = 6;
        private double billboardHeightCm = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectBodyVisualizationObject"/> class.
        /// </summary>
        public KinectBodyVisualizationObject()
        {
            this.visualJoints = new UpdatableVisual3DDictionary<JointType, SphereVisual3D>(null);
            this.visualBones = new UpdatableVisual3DDictionary<(JointType ChildJoint, JointType ParentJoint), PipeVisual3D>(null);

            this.Billboard = new BillboardTextVisualizationObject();
            this.Billboard.RegisterChildPropertyChangedNotifications(this, nameof(this.Billboard));

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("Color of the body.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the inferred joints opacity.
        /// </summary>
        [DataMember]
        [Description("Opacity for rendering inferred joints and bones.")]
        public double InferredJointsOpacity
        {
            get { return this.inferredJointsOpacity; }
            set { this.Set(nameof(this.InferredJointsOpacity), ref this.inferredJointsOpacity, value); }
        }

        /// <summary>
        /// Gets or sets the bone diameter.
        /// </summary>
        [DataMember]
        [DisplayName("Bone diameter (mm)")]
        [Description("Diameter of bones (mm).")]
        public double BoneDiameterMm
        {
            get { return this.boneDiameterMm; }
            set { this.Set(nameof(this.BoneDiameterMm), ref this.boneDiameterMm, value); }
        }

        /// <summary>
        /// Gets or sets the joint radius.
        /// </summary>
        [DataMember]
        [DisplayName("Joint radius (mm)")]
        [Description("Radius of joints (mm).")]
        public double JointRadiusMm
        {
            get { return this.jointRadiusMm; }
            set { this.Set(nameof(this.JointRadiusMm), ref this.jointRadiusMm, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show a billboard with information about the body.
        /// </summary>
        [DataMember]
        [PropertyOrder(0)]
        [Description("Show a billboard with information about the body.")]
        public bool ShowBillboard
        {
            get { return this.showBillboard; }
            set { this.Set(nameof(this.ShowBillboard), ref this.showBillboard, value); }
        }

        /// <summary>
        /// Gets or sets the height at which to draw the billboard (cm).
        /// </summary>
        [DataMember]
        [PropertyOrder(1)]
        [DisplayName("Billboard Height (cm)")]
        [Description("Height at which to draw the billboard (cm).")]
        public double BillboardHeightCm
        {
            get { return this.billboardHeightCm; }
            set { this.Set(nameof(this.BillboardHeightCm), ref this.billboardHeightCm, value); }
        }

        /// <summary>
        /// Gets the billboard visualization object for the body.
        /// </summary>
        [ExpandableObject]
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Billboard Properties")]
        [Description("The billboard properties.")]
        public BillboardTextVisualizationObject Billboard { get; private set; }

        /// <summary>
        /// Gets or sets the number of divisions to use when rendering polygons for joints and bones.
        /// </summary>
        [DataMember]
        [Description("Level of resolution at which to render joint and bone polygons (minimum value is 3).")]
        public int PolygonResolution
        {
            get { return this.polygonResolution; }
            set { this.Set(nameof(this.PolygonResolution), ref this.polygonResolution, value < 3 ? 3 : value); }
        }

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
            if (propertyName == nameof(this.Color) ||
                propertyName == nameof(this.InferredJointsOpacity) ||
                propertyName == nameof(this.BoneDiameterMm) ||
                propertyName == nameof(this.JointRadiusMm) ||
                propertyName == nameof(this.PolygonResolution))
            {
                this.UpdateVisuals();
            }
            else if (propertyName == nameof(this.ShowBillboard))
            {
                this.UpdateBillboardVisibility();
            }
            else if (propertyName == nameof(this.BillboardHeightCm))
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
            this.visualJoints.BeginUpdate();
            this.visualBones.BeginUpdate();

            if (this.CurrentData != null)
            {
                var trackedEntitiesBrush = new SolidColorBrush(this.Color);
                var untrackedEntitiesBrush = new SolidColorBrush(
                    Color.FromArgb(
                        (byte)(Math.Max(0, Math.Min(100, this.InferredJointsOpacity)) * 2.55),
                        this.Color.R,
                        this.Color.G,
                        this.Color.B));

                // update the joints
                foreach (var jointType in this.CurrentData.Joints.Keys)
                {
                    var jointState = this.CurrentData.Joints[jointType].TrackingState;
                    var visualJoint = this.visualJoints[jointType];
                    visualJoint.BeginEdit();
                    var isTracked = jointState == TrackingState.Tracked;
                    var visible = jointState != TrackingState.NotTracked && (isTracked || this.InferredJointsOpacity > 0);

                    if (visible)
                    {
                        var jointPosition = this.CurrentData.Joints[jointType].Pose.Origin;

                        if (visualJoint.Radius != this.JointRadiusMm / 1000.0)
                        {
                            visualJoint.Radius = this.JointRadiusMm / 1000.0;
                        }

                        var fill = isTracked ? trackedEntitiesBrush : untrackedEntitiesBrush;
                        if (visualJoint.Fill != fill)
                        {
                            visualJoint.Fill = fill;
                        }

                        visualJoint.Transform = new Win3D.TranslateTransform3D(jointPosition.X, jointPosition.Y, jointPosition.Z);

                        visualJoint.PhiDiv = this.PolygonResolution;
                        visualJoint.ThetaDiv = this.PolygonResolution;

                        visualJoint.Visible = true;
                    }
                    else
                    {
                        visualJoint.Visible = false;
                    }

                    visualJoint.EndEdit();
                }

                // update the bones
                foreach (var bone in KinectBody.Bones)
                {
                    var parentState = this.CurrentData.Joints[bone.ParentJoint].TrackingState;
                    var childState = this.CurrentData.Joints[bone.ChildJoint].TrackingState;
                    var parentIsTracked = parentState == TrackingState.Tracked;
                    var childIsTracked = childState == TrackingState.Tracked;
                    var isTracked = parentIsTracked && childIsTracked;
                    var visible = parentState != TrackingState.NotTracked && childState != TrackingState.NotTracked && (isTracked || this.InferredJointsOpacity > 0);
                    var visualBone = this.visualBones[bone];
                    visualBone.BeginEdit();
                    if (visible)
                    {
                        if (visualBone.Diameter != this.BoneDiameterMm / 1000.0)
                        {
                            visualBone.Diameter = this.BoneDiameterMm / 1000.0;
                        }

                        var joint1Position = this.visualJoints[bone.ParentJoint].Transform.Value;
                        var joint2Position = this.visualJoints[bone.ChildJoint].Transform.Value;

                        visualBone.Point1 = new Win3D.Point3D(joint1Position.OffsetX, joint1Position.OffsetY, joint1Position.OffsetZ);
                        visualBone.Point2 = new Win3D.Point3D(joint2Position.OffsetX, joint2Position.OffsetY, joint2Position.OffsetZ);

                        var fill = isTracked ? trackedEntitiesBrush : untrackedEntitiesBrush;
                        if (visualBone.Fill != fill)
                        {
                            visualBone.Fill = fill;
                        }

                        visualBone.ThetaDiv = this.PolygonResolution;

                        visualBone.Visible = true;
                    }
                    else
                    {
                        visualBone.Visible = false;
                    }

                    visualBone.EndEdit();
                }

                // set billboard position
                this.UpdateBillboard();
            }

            this.visualJoints.EndUpdate();
            this.visualBones.EndUpdate();
        }

        private void UpdateBillboard()
        {
            if (this.CurrentData != null)
            {
                var origin = this.CurrentData.Joints[JointType.SpineBase].Pose.Origin;
                var pos = new Win3D.Point3D(origin.X, origin.Y, origin.Z + (this.BillboardHeightCm / 100.0));
                var text = this.CurrentData.ToString();
                this.Billboard.SetCurrentValue(this.SynthesizeMessage(Tuple.Create(pos, text)));
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.visualJoints, this.Visible && this.CurrentData != default);
            this.UpdateChildVisibility(this.visualBones, this.Visible && this.CurrentData != default);
            this.UpdateBillboardVisibility();
        }

        private void UpdateBillboardVisibility()
        {
            this.UpdateChildVisibility(this.Billboard.ModelView, this.Visible && this.CurrentData != default && this.ShowBillboard);
        }
    }
}