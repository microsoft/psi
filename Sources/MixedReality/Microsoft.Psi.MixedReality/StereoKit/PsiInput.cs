// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements static properties for accessing inputs (head, eyes, and hands) with \psi conventions and types.
    /// </summary>
    public static class PsiInput
    {
        /// <summary>
        /// Gets the current head pose as a \psi coordinate system.
        /// </summary>
        /// <remarks>This input is also emitted on a stream by the <see cref="HeadSensor"/> component.</remarks>
        public static CoordinateSystem Head => Input.Head.ToPsi();

        /// <summary>
        /// Gets the current pose of the eyes as a <see cref="Ray3D"/> expressed in \psi coordinates.
        /// </summary>
        /// <remarks>This input is also emitted on a stream by the <see cref="EyesSensor"/> component.</remarks>
        public static Ray3D Eyes
        {
            get
            {
                var cs = Input.Eyes.ToPsi();
                return cs is null ? default : new Ray3D(cs.Origin, cs.XAxis);
            }
        }

        /// <summary>
        /// Gets the left hand with \psi coordinate systems for all joint poses.
        /// </summary>
        /// <remarks>This input is also emitted on a stream by the <see cref="HandsSensor"/> component.</remarks>
        public static Hand LeftHand => Input.Hand(Handed.Left).ToPsi();

        /// <summary>
        /// Gets the right hand with \psi coordinate systems for all joint poses.
        /// </summary>
        /// <remarks>This input is also emitted on a stream by the <see cref="HandsSensor"/> component.</remarks>
        public static Hand RightHand => Input.Hand(Handed.Right).ToPsi();

        private static Hand ToPsi(this global::StereoKit.Hand stereoKitHand)
        {
            var joints = new CoordinateSystem[(int)HandJointIndex.MaxIndex];

            // All joint poses will be null if the hand is not tracked.
            if (stereoKitHand.IsTracked)
            {
                // note: StereoKit thumbs have no Root, but \psi thumbs have no Intermediate
                joints[(int)HandJointIndex.Palm] = stereoKitHand.palm.ToPsi();
                joints[(int)HandJointIndex.Wrist] = stereoKitHand.wrist.ToPsi();
                joints[(int)HandJointIndex.ThumbMetacarpal] = stereoKitHand[FingerId.Thumb, JointId.KnuckleMajor].Pose.ToPsi(); // treating as proximal
                joints[(int)HandJointIndex.ThumbProximal] = stereoKitHand[FingerId.Thumb, JointId.KnuckleMid].Pose.ToPsi(); // treating as intermediate
                joints[(int)HandJointIndex.ThumbDistal] = stereoKitHand[FingerId.Thumb, JointId.KnuckleMinor].Pose.ToPsi();
                joints[(int)HandJointIndex.ThumbTip] = stereoKitHand[FingerId.Thumb, JointId.Tip].Pose.ToPsi();
                joints[(int)HandJointIndex.IndexMetacarpal] = stereoKitHand[FingerId.Index, JointId.Root].Pose.ToPsi();
                joints[(int)HandJointIndex.IndexProximal] = stereoKitHand[FingerId.Index, JointId.KnuckleMajor].Pose.ToPsi();
                joints[(int)HandJointIndex.IndexIntermediate] = stereoKitHand[FingerId.Index, JointId.KnuckleMid].Pose.ToPsi();
                joints[(int)HandJointIndex.IndexDistal] = stereoKitHand[FingerId.Index, JointId.KnuckleMinor].Pose.ToPsi();
                joints[(int)HandJointIndex.IndexTip] = stereoKitHand[FingerId.Index, JointId.Tip].Pose.ToPsi();
                joints[(int)HandJointIndex.MiddleMetacarpal] = stereoKitHand[FingerId.Middle, JointId.Root].Pose.ToPsi();
                joints[(int)HandJointIndex.MiddleProximal] = stereoKitHand[FingerId.Middle, JointId.KnuckleMajor].Pose.ToPsi();
                joints[(int)HandJointIndex.MiddleIntermediate] = stereoKitHand[FingerId.Middle, JointId.KnuckleMid].Pose.ToPsi();
                joints[(int)HandJointIndex.MiddleDistal] = stereoKitHand[FingerId.Middle, JointId.KnuckleMinor].Pose.ToPsi();
                joints[(int)HandJointIndex.MiddleTip] = stereoKitHand[FingerId.Middle, JointId.Tip].Pose.ToPsi();
                joints[(int)HandJointIndex.RingMetacarpal] = stereoKitHand[FingerId.Ring, JointId.Root].Pose.ToPsi();
                joints[(int)HandJointIndex.RingProximal] = stereoKitHand[FingerId.Ring, JointId.KnuckleMajor].Pose.ToPsi();
                joints[(int)HandJointIndex.RingIntermediate] = stereoKitHand[FingerId.Ring, JointId.KnuckleMid].Pose.ToPsi();
                joints[(int)HandJointIndex.RingDistal] = stereoKitHand[FingerId.Ring, JointId.KnuckleMinor].Pose.ToPsi();
                joints[(int)HandJointIndex.RingTip] = stereoKitHand[FingerId.Ring, JointId.Tip].Pose.ToPsi();
                joints[(int)HandJointIndex.PinkyMetacarpal] = stereoKitHand[FingerId.Little, JointId.Root].Pose.ToPsi();
                joints[(int)HandJointIndex.PinkyProximal] = stereoKitHand[FingerId.Little, JointId.KnuckleMajor].Pose.ToPsi();
                joints[(int)HandJointIndex.PinkyIntermediate] = stereoKitHand[FingerId.Little, JointId.KnuckleMid].Pose.ToPsi();
                joints[(int)HandJointIndex.PinkyDistal] = stereoKitHand[FingerId.Little, JointId.KnuckleMinor].Pose.ToPsi();
                joints[(int)HandJointIndex.PinkyTip] = stereoKitHand[FingerId.Little, JointId.Tip].Pose.ToPsi();
            }

            return new Hand(stereoKitHand.IsTracked, stereoKitHand.IsPinched, stereoKitHand.IsGripped, joints);
        }

        private static CoordinateSystem ToPsi(this Pose pose) =>
            StereoKitTransforms.StereoKitToWorld is null ? null : pose.ToCoordinateSystem().TransformBy(StereoKitTransforms.StereoKitToWorld);
    }
}
