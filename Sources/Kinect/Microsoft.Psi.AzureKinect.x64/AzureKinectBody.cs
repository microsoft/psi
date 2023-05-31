// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using System.Collections.Generic;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;

    /// <summary>
    /// Represents a body detected by the Azure Kinect.
    /// </summary>
    public class AzureKinectBody
    {
        private static readonly CoordinateSystem KinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis.Negate());
        private static readonly CoordinateSystem KinectBasisInverted = KinectBasis.Invert();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKinectBody"/> class.
        /// </summary>
        public AzureKinectBody()
        {
            for (int i = 0; i < Skeleton.JointCount; i++)
            {
                this.Joints.Add((JointId)i, (null, JointConfidenceLevel.None));
            }
        }

        /// <summary>
        /// Gets the bone relationships.
        /// </summary>
        /// <remarks>
        /// Bone connections defined here: https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints.
        /// </remarks>
        public static List<(JointId ChildJoint, JointId ParentJoint)> Bones { get; } = new List<(JointId, JointId)>
        {
            // Spine
            (JointId.SpineNavel, JointId.Pelvis),
            (JointId.SpineChest, JointId.SpineNavel),
            (JointId.Neck, JointId.SpineChest),

            // Left arm
            (JointId.ClavicleLeft, JointId.SpineChest),
            (JointId.ShoulderLeft, JointId.ClavicleLeft),
            (JointId.ElbowLeft, JointId.ShoulderLeft),
            (JointId.WristLeft, JointId.ElbowLeft),
            (JointId.HandLeft, JointId.WristLeft),
            (JointId.HandTipLeft, JointId.HandLeft),
            (JointId.ThumbLeft, JointId.WristLeft),

            // Right arm
            (JointId.ClavicleRight, JointId.SpineChest),
            (JointId.ShoulderRight, JointId.ClavicleRight),
            (JointId.ElbowRight, JointId.ShoulderRight),
            (JointId.WristRight, JointId.ElbowRight),
            (JointId.HandRight, JointId.WristRight),
            (JointId.HandTipRight, JointId.HandRight),
            (JointId.ThumbRight, JointId.WristRight),

            // Left leg
            (JointId.HipLeft, JointId.Pelvis),
            (JointId.KneeLeft, JointId.HipLeft),
            (JointId.AnkleLeft, JointId.KneeLeft),
            (JointId.FootLeft, JointId.AnkleLeft),

            // Right leg
            (JointId.HipRight, JointId.Pelvis),
            (JointId.KneeRight, JointId.HipRight),
            (JointId.AnkleRight, JointId.KneeRight),
            (JointId.FootRight, JointId.AnkleRight),

            // Head
            (JointId.Head, JointId.Neck),
            (JointId.Nose, JointId.Head),
            (JointId.EyeLeft, JointId.Head),
            (JointId.EarLeft, JointId.Head),
            (JointId.EyeRight, JointId.Head),
            (JointId.EarRight, JointId.Head),
        };

        /// <summary>
        /// Gets the joint information.
        /// </summary>
        public Dictionary<JointId, (CoordinateSystem Pose, JointConfidenceLevel Confidence)> Joints { get; } = new Dictionary<JointId, (CoordinateSystem, JointConfidenceLevel)>();

        /// <summary>
        /// Gets the body's tracking ID.
        /// </summary>
        public uint TrackingId { get; private set; }

        /// <summary>
        /// Copies new joint and tracking information for this body from a Microsoft.Azure.Kinect body instance.
        /// </summary>
        /// <param name="body">The Microsoft.Azure.Kinect body instance to populate this body from.</param>
        public void CopyFrom(Body body)
        {
            this.TrackingId = body.Id;

            for (int i = 0; i < Skeleton.JointCount; i++)
            {
                var joint = body.Skeleton.GetJoint(i);
                var position = joint.Position;
                var orientation = joint.Quaternion;
                var confidence = joint.ConfidenceLevel;
                this.Joints[(JointId)i] = (this.CreateCoordinateSystem(position, orientation), confidence);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ID: {this.TrackingId}";
        }

        private CoordinateSystem CreateCoordinateSystem(Vector3 position, System.Numerics.Quaternion orientation)
        {
            // Convert the quaternion to a rotation matrix (System.Numerics)
            var jointRotation = Matrix4x4.CreateFromQuaternion(orientation);

            // In the System.Numerics.Matrix4x4 joint rotation above, the axes of rotation are defined as follows:
            // X: [M11; M12; M13]
            // Y: [M21; M22; M23]
            // Z: [M31; M32; M33]
            // However, joint rotation axes are defined differently from the Azure Kinect sensor axes, as defined here:
            // https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints
            // and here:
            // https://docs.microsoft.com/en-us/azure/Kinect-dk/coordinate-systems
            // Joint Axes:
            //        X
            //        |   Y
            //        |  /
            //        | /
            // Z <----+
            // Azure Kinect Axes:
            //           Z
            //          /
            //         /
            //        +---->X
            //        |
            //        |
            //        |
            //        Y
            // Therefore we first create a transformation matrix in Azure Kinect basis by converting axes:
            // X (Azure Kinect) = -Z (Joint)
            // Y (Azure Kinect) = -X (Joint)
            // Z (Azure Kinect) =  Y (Joint)
            // and converting from millimeters to meters.
            var transformationMatrix = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { -jointRotation.M31, -jointRotation.M11, jointRotation.M21, position.X / 1000.0 },
                { -jointRotation.M32, -jointRotation.M12, jointRotation.M22, position.Y / 1000.0 },
                { -jointRotation.M33, -jointRotation.M13, jointRotation.M23, position.Z / 1000.0 },
                { 0,                  0,                  0,                 1 },
            });

            // Finally, convert from Azure Kinect's basis to MathNet's basis:
            return new CoordinateSystem(KinectBasisInverted * transformationMatrix * KinectBasis);
        }
    }
}