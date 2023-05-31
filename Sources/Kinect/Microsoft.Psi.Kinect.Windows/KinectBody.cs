// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System.Collections.Generic;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Kinect;

    /// <summary>
    /// KinectBody holds information about a body detected by the Kinect.
    /// </summary>
    public class KinectBody
    {
        private static readonly CoordinateSystem KinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis, UnitVector3D.YAxis);
        private static readonly CoordinateSystem KinectBasisInverse = KinectBasis.Invert();

        /// <summary>
        /// Gets the bone relationships.
        /// </summary>
        public static List<(JointType ChildJoint, JointType ParentJoint)> Bones { get; } = new List<(JointType, JointType)>
        {
            // Spine and head
            (JointType.SpineMid, JointType.SpineBase),
            (JointType.SpineShoulder, JointType.SpineMid),
            (JointType.Neck, JointType.SpineShoulder),
            (JointType.Head, JointType.Neck),

            // Left arm
            (JointType.ShoulderLeft, JointType.SpineShoulder),
            (JointType.ElbowLeft, JointType.ShoulderLeft),
            (JointType.WristLeft, JointType.ElbowLeft),
            (JointType.HandLeft, JointType.WristLeft),
            (JointType.HandTipLeft, JointType.HandLeft),
            (JointType.ThumbLeft, JointType.WristLeft),

            // Right arm
            (JointType.ShoulderRight, JointType.SpineShoulder),
            (JointType.ElbowRight, JointType.ShoulderRight),
            (JointType.WristRight, JointType.ElbowRight),
            (JointType.HandRight, JointType.WristRight),
            (JointType.HandTipRight, JointType.HandRight),
            (JointType.ThumbRight, JointType.WristRight),

            // Left leg
            (JointType.HipLeft, JointType.SpineBase),
            (JointType.KneeLeft, JointType.HipLeft),
            (JointType.AnkleLeft, JointType.KneeLeft),
            (JointType.FootLeft, JointType.AnkleLeft),

            // Right leg
            (JointType.HipRight, JointType.SpineBase),
            (JointType.KneeRight, JointType.HipRight),
            (JointType.AnkleRight, JointType.KneeRight),
            (JointType.FootRight, JointType.AnkleRight),
        };

        /// <summary>
        /// Gets the clipped edges.
        /// </summary>
        public FrameEdges ClippedEdges { get; private set; }

        /// <summary>
        /// Gets or sets the floor's clip plane.
        /// </summary>
        public Microsoft.Kinect.Vector4 FloorClipPlane { get; set; }

        /// <summary>
        /// Gets confidence in position/pose of left hand.
        /// </summary>
        public TrackingConfidence HandLeftConfidence { get; private set; }

        /// <summary>
        /// Gets state of left hand.
        /// </summary>
        public HandState HandLeftState { get; private set; }

        /// <summary>
        /// Gets confidence in position/pose of right hand.
        /// </summary>
        public TrackingConfidence HandRightConfidence { get; private set; }

        /// <summary>
        /// Gets state of right hand.
        /// </summary>
        public HandState HandRightState { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the body is restricted.
        /// </summary>
        public bool IsRestricted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the body is tracked.
        /// </summary>
        public bool IsTracked { get; private set; }

        /// <summary>
        /// Gets the joint information.
        /// </summary>
        public Dictionary<JointType, (CoordinateSystem Pose, TrackingState TrackingState)> Joints { get; } =
            new Dictionary<JointType, (CoordinateSystem Pose, TrackingState TrackingState)>();

        /// <summary>
        /// Gets the lean point.
        /// </summary>
        public Point2D Lean { get; private set; }

        /// <summary>
        /// Gets the lean tracking state.
        /// </summary>
        public TrackingState LeanTrackingState { get; private set; }

        /// <summary>
        /// Gets the body's tracking ID.
        /// </summary>
        public ulong TrackingId { get; private set; }

        /// <summary>
        /// Populate this body representation with new joint and tracking information.
        /// </summary>
        /// <param name="body">The body from the Kinect sensor from which to populate this body with information.</param>
        public void UpdateFrom(Body body)
        {
            this.TrackingId = body.TrackingId;
            this.ClippedEdges = body.ClippedEdges;
            this.HandLeftConfidence = body.HandLeftConfidence;
            this.HandLeftState = body.HandLeftState;
            this.HandRightConfidence = body.HandRightConfidence;
            this.HandRightState = body.HandRightState;
            this.IsRestricted = body.IsRestricted;
            this.IsTracked = body.IsTracked;
            this.Lean = new Point2D(body.Lean.X, body.Lean.Y);
            this.LeanTrackingState = body.LeanTrackingState;

            foreach (var jointType in body.Joints.Keys)
            {
                var joint = body.Joints[jointType];

                CoordinateSystem kinectJointCS;

                if (body.JointOrientations.ContainsKey(jointType))
                {
                    kinectJointCS = this.CreateCoordinateSystem(joint.Position, body.JointOrientations[jointType].Orientation);
                }
                else
                {
                    kinectJointCS = this.CreateCoordinateSystem(joint.Position, default);
                }

                this.Joints[jointType] = (kinectJointCS, joint.TrackingState);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ID: {this.TrackingId}";
        }

        private CoordinateSystem CreateCoordinateSystem(CameraSpacePoint position, Microsoft.Kinect.Vector4 quaternion)
        {
            Matrix<double> kinectJointMatrix;

            if (quaternion == default)
            {
                kinectJointMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { 1, 0, 0, position.X },
                    { 0, 1, 0, position.Y },
                    { 0, 0, 1, position.Z },
                    { 0, 0, 0, 1 },
                });
            }
            else
            {
                // Convert the quaternion into a System.Numerics matrix, and then create a MathNet matrix while including the position.
                var jointRotation = Matrix4x4.CreateFromQuaternion(new System.Numerics.Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W));
                kinectJointMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { jointRotation.M11, jointRotation.M21, jointRotation.M31, position.X },
                    { jointRotation.M12, jointRotation.M22, jointRotation.M32, position.Y },
                    { jointRotation.M13, jointRotation.M23, jointRotation.M33, position.Z },
                    { 0,                  0,                  0,                 1 },
                });
            }

            // Convert from Kinect to MathNet basis.
            return new CoordinateSystem(KinectBasisInverse * kinectJointMatrix * KinectBasis);
        }
    }
}
