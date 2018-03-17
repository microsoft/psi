// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Microsoft.Kinect;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a Kinect bodies visual.
    /// </summary>
    public class KinectBodiesVisual : ModelVisual3D
    {
        private static readonly int SphereDiv = 5;
        private static readonly int PipeDiv = 7;

        private KinectBodies3DVisualizationObject visualizationObject;
        private List<List<SphereVisual3D>> bodyJoints = new List<List<SphereVisual3D>>();
        private List<Dictionary<Tuple<JointType, JointType>, PipeVisual3D>> bodyBones = new List<Dictionary<Tuple<JointType, JointType>, PipeVisual3D>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectBodiesVisual"/> class.
        ///
        /// </summary>
        /// <param name="visualizationObject">The Kinect bodies 3D visualization object.</param>
        public KinectBodiesVisual(KinectBodies3DVisualizationObject visualizationObject)
        {
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
            this.visualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
        }

        private void ClearAll()
        {
            this.bodyJoints.Clear();
            this.bodyBones.Clear();
            this.Children.Clear();
        }

        private void AddBody(int bodyIndex)
        {
            var regularBrush = new SolidColorBrush(this.visualizationObject.Configuration.Color);

            List<SphereVisual3D> joints = new List<SphereVisual3D>();
            for (int i = 0; i < Body.JointCount; i++)
            {
                var bodyPart = new SphereVisual3D()
                {
                    ThetaDiv = SphereDiv,
                    PhiDiv = SphereDiv
                };

                joints.Add(bodyPart);
                this.Children.Add(bodyPart);
            }

            this.bodyJoints.Add(joints);

            var bones = new Dictionary<Tuple<JointType, JointType>, PipeVisual3D>
            {
                { Tuple.Create(JointType.HandTipLeft, JointType.HandLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.ThumbLeft, JointType.HandLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.HandLeft, JointType.WristLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.WristLeft, JointType.ElbowLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.ElbowLeft, JointType.ShoulderLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.ShoulderLeft, JointType.SpineShoulder), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.HandTipRight, JointType.HandRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.ThumbRight, JointType.HandRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.HandRight, JointType.WristRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.WristRight, JointType.ElbowRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.ElbowRight, JointType.ShoulderRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.ShoulderRight, JointType.SpineShoulder), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.Head, JointType.Neck), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.Neck, JointType.SpineShoulder), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.SpineShoulder, JointType.SpineBase), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.SpineBase, JointType.HipRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.HipRight, JointType.KneeRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.KneeRight, JointType.AnkleRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.AnkleRight, JointType.FootRight), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.SpineBase, JointType.HipLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.HipLeft, JointType.KneeLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.KneeLeft, JointType.AnkleLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
                { Tuple.Create(JointType.AnkleLeft, JointType.FootLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } }
            };

            foreach (var b in bones)
            {
                this.Children.Add(b.Value);
            }

            this.bodyBones.Add(bones);
            this.UpdateProperties();
        }

        private void UpdateProperties()
        {
            var regularBrush = new SolidColorBrush(this.visualizationObject.Configuration.Color);
            foreach (var body in this.bodyJoints)
            {
                foreach (var joint in body)
                {
                    joint.Radius = this.visualizationObject.Configuration.Size;
                    joint.Fill = regularBrush;
                }
            }

            foreach (var body in this.bodyBones)
            {
                foreach (var bone in body.Values)
                {
                    bone.Diameter = this.visualizationObject.Configuration.Size;
                    bone.Fill = regularBrush;
                }
            }
        }

        private void UpdateBodies(List<Microsoft.Psi.Kinect.KinectBody> kinectBodies)
        {
            if (kinectBodies == null)
            {
                this.ClearAll();
                return;
            }

            // if we have more bodies than we need, clear all
            if (kinectBodies.Count < this.bodyJoints.Count)
            {
                this.ClearAll();
            }

            // add bodies if we don't have enough
            for (int body = this.bodyJoints.Count; body < kinectBodies.Count; body++)
            {
                this.AddBody(body);
            }

            // populate the bodies with information
            for (int body = 0; body < kinectBodies.Count; body++)
            {
                var bodyTracked = kinectBodies[body].IsTracked;
                for (int joint = 0; joint < Body.JointCount; joint++)
                {
                    var jointTracked =
                        kinectBodies[body].Joints.ContainsKey((JointType)joint) && kinectBodies[body].Joints[(JointType)joint].TrackingState != TrackingState.NotTracked;
                    if (body < kinectBodies.Count && bodyTracked && jointTracked)
                    {
                        var jointPosition = kinectBodies[body].Joints[(JointType)joint].Position;
                        this.bodyJoints[body][joint].Transform = new TranslateTransform3D(jointPosition.X, jointPosition.Y, jointPosition.Z);
                        if (kinectBodies[body].Joints[(JointType)joint].TrackingState == TrackingState.Tracked)
                        {
                            this.bodyJoints[body][joint].Visible = true;
                        }
                        else
                        {
                            this.bodyJoints[body][joint].Visible = false;
                        }
                    }
                    else
                    {
                        this.bodyJoints[body][joint].Visible = false;
                    }
                }

                foreach (var bone in this.bodyBones[body].Keys)
                {
                    var boneTracked = kinectBodies[body].Joints[bone.Item1].TrackingState != TrackingState.NotTracked && kinectBodies[body].Joints[bone.Item2].TrackingState != TrackingState.NotTracked;
                    if (body < kinectBodies.Count && bodyTracked && boneTracked)
                    {
                        var joint1Position = kinectBodies[body].Joints[bone.Item1].Position;
                        var joint2Position = kinectBodies[body].Joints[bone.Item2].Position;
                        this.bodyBones[body][bone].Point1 = new Point3D(joint1Position.X, joint1Position.Y, joint1Position.Z);
                        this.bodyBones[body][bone].Point2 = new Point3D(joint2Position.X, joint2Position.Y, joint2Position.Z);
                        if (kinectBodies[body].Joints[bone.Item1].TrackingState == TrackingState.Tracked && kinectBodies[body].Joints[bone.Item2].TrackingState == TrackingState.Tracked)
                        {
                            this.bodyBones[body][bone].Visible = true;
                        }
                        else
                        {
                            this.bodyBones[body][bone].Visible = false;
                        }
                    }
                    else
                    {
                        this.bodyBones[body][bone].Visible = false;
                    }
                }
            }
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KinectBodies3DVisualizationObjectConfiguration.Size) ||
                e.PropertyName == nameof(KinectBodies3DVisualizationObjectConfiguration.Color) ||
                e.PropertyName == nameof(KinectBodies3DVisualizationObjectConfiguration.InferredJointsOpacity))
            {
                this.UpdateProperties();
            }
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KinectBodies3DVisualizationObject.Configuration))
            {
                this.visualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
                this.UpdateProperties();
            }
            else if (e.PropertyName == nameof(KinectBodies3DVisualizationObject.CurrentValue))
            {
                this.UpdateBodies(this.visualizationObject.CurrentValue.Data);
            }
        }
    }
}
