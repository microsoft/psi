// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals3D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Kinect;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a Kinect bodies visual.
    /// </summary>
    public class KinectBodiesVisual : ModelVisual3D
    {
        private static readonly int SphereDiv = 5;
        private static readonly int PipeDiv = 7;
        private Plane horizontalPlane = new Plane(new MathNet.Spatial.Euclidean.Point3D(0, 0, 0), UnitVector3D.Create(0, 0, 1));
        private int numBodies = 0;

        private KinectBodies3DVisualizationObject visualizationObject;
        private Dictionary<ulong, List<SphereVisual3D>> bodyJoints = new Dictionary<ulong, List<SphereVisual3D>>();
        private Dictionary<ulong, List<bool>> bodyJointsTracked = new Dictionary<ulong, List<bool>>();
        private Dictionary<ulong, Dictionary<Tuple<JointType, JointType>, PipeVisual3D>> bodyBones = new Dictionary<ulong, Dictionary<Tuple<JointType, JointType>, PipeVisual3D>>();
        private Dictionary<ulong, Dictionary<Tuple<JointType, JointType>, bool>> bodyBonesTracked = new Dictionary<ulong, Dictionary<Tuple<JointType, JointType>, bool>>();
        private Dictionary<ulong, BillboardTextVisual3D> trackingIdBillboards = new Dictionary<ulong, BillboardTextVisual3D>();

        private Brush trackedEntitiesBrush = new SolidColorBrush();
        private Brush untrackedEntitiesBrush = new SolidColorBrush();

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectBodiesVisual"/> class.
        ///
        /// </summary>
        /// <param name="visualizationObject">The Kinect bodies 3D visualization object.</param>
        public KinectBodiesVisual(KinectBodies3DVisualizationObject visualizationObject)
        {
            this.visualizationObject = visualizationObject;
            this.visualizationObject.PropertyChanged += this.VisualizationObject_PropertyChanged;
        }

        private void ClearAll()
        {
            this.bodyJoints.Clear();
            this.bodyJointsTracked.Clear();
            this.bodyBones.Clear();
            this.bodyBonesTracked.Clear();
            this.trackingIdBillboards.Clear();
            this.Children.Clear();
        }

        private void AddBody(ulong trackingId)
        {
            List<SphereVisual3D> joints = new List<SphereVisual3D>();
            List<bool> jointsTracked = new List<bool>();
            for (int i = 0; i < Body.JointCount; i++)
            {
                var bodyPart = new SphereVisual3D()
                {
                    ThetaDiv = SphereDiv,
                    PhiDiv = SphereDiv,
                };

                joints.Add(bodyPart);
                jointsTracked.Add(true);
                this.Children.Add(bodyPart);
            }

            this.bodyJoints.Add(trackingId, joints);
            this.bodyJointsTracked.Add(trackingId, jointsTracked);

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
                { Tuple.Create(JointType.AnkleLeft, JointType.FootLeft), new PipeVisual3D() { ThetaDiv = PipeDiv } },
            };
            var bonesTracked = new Dictionary<Tuple<JointType, JointType>, bool>
            {
                { Tuple.Create(JointType.HandTipLeft, JointType.HandLeft), true },
                { Tuple.Create(JointType.ThumbLeft, JointType.HandLeft), true },
                { Tuple.Create(JointType.HandLeft, JointType.WristLeft), true },
                { Tuple.Create(JointType.WristLeft, JointType.ElbowLeft), true },
                { Tuple.Create(JointType.ElbowLeft, JointType.ShoulderLeft), true },
                { Tuple.Create(JointType.ShoulderLeft, JointType.SpineShoulder), true },
                { Tuple.Create(JointType.HandTipRight, JointType.HandRight), true },
                { Tuple.Create(JointType.ThumbRight, JointType.HandRight), true },
                { Tuple.Create(JointType.HandRight, JointType.WristRight), true },
                { Tuple.Create(JointType.WristRight, JointType.ElbowRight), true },
                { Tuple.Create(JointType.ElbowRight, JointType.ShoulderRight), true },
                { Tuple.Create(JointType.ShoulderRight, JointType.SpineShoulder), true },
                { Tuple.Create(JointType.Head, JointType.Neck), true },
                { Tuple.Create(JointType.Neck, JointType.SpineShoulder), true },
                { Tuple.Create(JointType.SpineShoulder, JointType.SpineBase), true },
                { Tuple.Create(JointType.SpineBase, JointType.HipRight), true },
                { Tuple.Create(JointType.HipRight, JointType.KneeRight), true },
                { Tuple.Create(JointType.KneeRight, JointType.AnkleRight), true },
                { Tuple.Create(JointType.AnkleRight, JointType.FootRight), true },
                { Tuple.Create(JointType.SpineBase, JointType.HipLeft), true },
                { Tuple.Create(JointType.HipLeft, JointType.KneeLeft), true },
                { Tuple.Create(JointType.KneeLeft, JointType.AnkleLeft), true },
                { Tuple.Create(JointType.AnkleLeft, JointType.FootLeft), true },
            };

            foreach (var b in bones)
            {
                this.Children.Add(b.Value);
            }

            this.bodyBones.Add(trackingId, bones);
            this.bodyBonesTracked.Add(trackingId, bonesTracked);

            // add the billboard
            var billboard = new BillboardTextVisual3D()
            {
                // Background = new SolidColorBrush(Color.FromArgb(255, 70, 85, 198)),
                Background = Brushes.Gray,
                Foreground = new SolidColorBrush(Colors.White),
                Padding = new System.Windows.Thickness(5),
                Text = $"Kinect Id: {this.numBodies++}",
            };
            this.trackingIdBillboards.Add(trackingId, billboard);
            this.Children.Add(billboard);

            this.UpdateProperties();
        }

        private void RemoveBody(ulong trackingId)
        {
            // remove joints
            foreach (var joint in this.bodyJoints[trackingId])
            {
                this.Children.Remove(joint);
            }

            // remove bones
            foreach (var bone in this.bodyBones[trackingId].Values)
            {
                this.Children.Remove(bone);
            }

            this.Children.Remove(this.trackingIdBillboards[trackingId]);

            this.bodyJoints.Remove(trackingId);
            this.bodyJointsTracked.Remove(trackingId);
            this.bodyBones.Remove(trackingId);
            this.bodyBonesTracked.Remove(trackingId);
            this.trackingIdBillboards.Remove(trackingId);
        }

        private void UpdateProperties()
        {
            this.trackedEntitiesBrush = new SolidColorBrush(this.visualizationObject.Color);
            var alphaColor = Color.FromArgb(
                (byte)(this.visualizationObject.InferredJointsOpacity * 255),
                this.visualizationObject.Color.R,
                this.visualizationObject.Color.G,
                this.visualizationObject.Color.B);
            this.untrackedEntitiesBrush = new SolidColorBrush(alphaColor);

            foreach (var body in this.bodyJoints)
            {
                for (int i = 0; i < body.Value.Count; i++)
                {
                    body.Value[i].Radius = this.visualizationObject.Size;
                    body.Value[i].Fill = this.bodyJointsTracked[body.Key][i] ? this.trackedEntitiesBrush : this.untrackedEntitiesBrush;
                }
            }

            foreach (var body in this.bodyBones)
            {
                foreach (var bone in body.Value)
                {
                    bone.Value.Diameter = this.visualizationObject.Size;
                    bone.Value.Fill = this.bodyBonesTracked[body.Key][bone.Key] ? this.trackedEntitiesBrush : this.untrackedEntitiesBrush;
                }
            }

            foreach (var billboard in this.trackingIdBillboards)
            {
                if (this.visualizationObject.ShowTrackingBillboards)
                {
                    if (!this.Children.Contains(billboard.Value))
                    {
                        this.Children.Add(billboard.Value);
                    }
                }
                else
                {
                    if (this.Children.Contains(billboard.Value))
                    {
                        this.Children.Remove(billboard.Value);
                    }
                }
            }
        }

        private void UpdateBodies(List<KinectBody> kinectBodies)
        {
            if (kinectBodies == null)
            {
                this.ClearAll();
                return;
            }

            // add any missing bodies
            foreach (var body in kinectBodies)
            {
                if (!this.bodyJoints.ContainsKey(body.TrackingId))
                {
                    this.AddBody(body.TrackingId);
                }
            }

            // remove any non-necessary bodies
            var currentIds = this.bodyJoints.Select(kvp => kvp.Key).ToArray();
            foreach (var id in currentIds)
            {
                if (!kinectBodies.Any(b => b.TrackingId == id))
                {
                    this.RemoveBody(id);
                }
            }

            // populate the bodies with information
            for (int body = 0; body < kinectBodies.Count; body++)
            {
                var trackingId = kinectBodies[body].TrackingId;
                var bodyTracked = kinectBodies[body].IsTracked;
                for (int joint = 0; joint < Body.JointCount; joint++)
                {
                    var jointTracked =
                        kinectBodies[body].Joints.ContainsKey((JointType)joint) && kinectBodies[body].Joints[(JointType)joint].TrackingState != TrackingState.NotTracked;
                    if (body < kinectBodies.Count && bodyTracked && jointTracked)
                    {
                        var jointPosition = kinectBodies[body].Joints[(JointType)joint].Position;
                        this.bodyJoints[trackingId][joint].Transform = new TranslateTransform3D(jointPosition.X, jointPosition.Y, jointPosition.Z);
                        this.bodyJointsTracked[trackingId][joint] = kinectBodies[body].Joints[(JointType)joint].TrackingState == TrackingState.Tracked;
                    }
                    else
                    {
                        this.bodyJointsTracked[trackingId][joint] = false;
                    }
                }

                foreach (var bone in this.bodyBones[trackingId].Keys)
                {
                    var boneTracked = kinectBodies[body].Joints[bone.Item1].TrackingState != TrackingState.NotTracked && kinectBodies[body].Joints[bone.Item2].TrackingState != TrackingState.NotTracked;
                    if (body < kinectBodies.Count && bodyTracked && boneTracked)
                    {
                        var joint1Position = kinectBodies[body].Joints[bone.Item1].Position;
                        var joint2Position = kinectBodies[body].Joints[bone.Item2].Position;
                        this.bodyBones[trackingId][bone].Point1 = new System.Windows.Media.Media3D.Point3D(joint1Position.X, joint1Position.Y, joint1Position.Z);
                        this.bodyBones[trackingId][bone].Point2 = new System.Windows.Media.Media3D.Point3D(joint2Position.X, joint2Position.Y, joint2Position.Z);
                        this.bodyBonesTracked[trackingId][bone] = kinectBodies[body].Joints[bone.Item1].TrackingState == TrackingState.Tracked && kinectBodies[body].Joints[bone.Item2].TrackingState == TrackingState.Tracked;
                    }
                    else
                    {
                        this.bodyBonesTracked[trackingId][bone] = false;
                    }
                }

                // set billboard position
                var spineBasePosition = kinectBodies[body].Joints[JointType.SpineBase].Position;
                this.trackingIdBillboards[trackingId].Position = new System.Windows.Media.Media3D.Point3D(
                    spineBasePosition.X,
                    spineBasePosition.Y,
                    spineBasePosition.Z + 1);
            }
        }

        private void VisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KinectBodies3DVisualizationObject.CurrentValue))
            {
                this.UpdateBodies(this.visualizationObject.CurrentValue.GetValueOrDefault().Data);
            }
            else if (e.PropertyName == nameof(this.visualizationObject.Size) ||
                e.PropertyName == nameof(this.visualizationObject.Color) ||
                e.PropertyName == nameof(this.visualizationObject.InferredJointsOpacity))
            {
                this.UpdateProperties();
            }
        }
    }
}
