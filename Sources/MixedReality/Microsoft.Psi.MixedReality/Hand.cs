// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using MathNet.Spatial.Euclidean;
    using StereoKit;

    /// <summary>
    /// Represents a tracked hand.
    /// </summary>
    public class Hand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hand"/> class.
        /// </summary>
        /// <param name="isTracked">Whether hand is being tracked.</param>
        /// <param name="isPinched">Whether fingers are pinched.</param>
        /// <param name="isGripped">Whether fingers are gripped.</param>
        /// <param name="joints">Finger joints (index by <see cref="HandJointIndex"/>).</param>
        public Hand(bool isTracked, bool isPinched, bool isGripped, CoordinateSystem[] joints)
        {
            this.IsTracked = isTracked;
            this.IsPinched = isPinched;
            this.IsGripped = isGripped;
            this.Joints = joints;
        }

        /// <summary>
        /// Gets a value indicating whether hand is being tracked.
        /// </summary>
        public bool IsTracked { get; private set; }

        /// <summary>
        /// Gets a value indicating whether fingers are pitched.
        /// </summary>
        public bool IsPinched { get; private set; }

        /// <summary>
        /// Gets a value indicating whether fingers are gripped.
        /// </summary>
        public bool IsGripped { get; private set; }

        /// <summary>
        /// Gets finger joints in \psi basis (indexed by <see cref="HandJointIndex"/>).
        /// </summary>
        public CoordinateSystem[] Joints { get; private set; }

        /// <summary>
        /// Gets the joint in \psi basis specified by a <see cref="HandJointIndex"/>.
        /// </summary>
        /// <param name="handJointIndex">The joint index.</param>
        /// <returns>The corresponding joint.</returns>
        public CoordinateSystem this[HandJointIndex handJointIndex] => this.Joints[(int)handJointIndex];

        /// <summary>
        /// Constructs a <see cref="Hand"/> object from a StereoKit hand.
        /// </summary>
        /// <param name="hand">The StereoKit hand.</param>
        /// <returns>The constructed <see cref="Hand"/> object.</returns>
        public static Hand FromStereoKitHand(StereoKit.Hand hand)
        {
            // note: StereoKit thumbs have no Root, but \psi thumbs have no Intermediate
            var joints = new CoordinateSystem[(int)HandJointIndex.MaxIndex];
            joints[(int)HandJointIndex.Palm] = hand.palm.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.Wrist] = hand.wrist.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.ThumbMetacarpal] = hand[FingerId.Thumb, JointId.KnuckleMajor].Pose.ToPsiCoordinateSystem(); // treating as proximal
            joints[(int)HandJointIndex.ThumbProximal] = hand[FingerId.Thumb, JointId.KnuckleMid].Pose.ToPsiCoordinateSystem(); // treating as intermediate
            joints[(int)HandJointIndex.ThumbDistal] = hand[FingerId.Thumb, JointId.KnuckleMinor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.ThumbTip] = hand[FingerId.Thumb, JointId.Tip].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.IndexMetacarpal] = hand[FingerId.Index, JointId.Root].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.IndexProximal] = hand[FingerId.Index, JointId.KnuckleMajor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.IndexIntermediate] = hand[FingerId.Index, JointId.KnuckleMid].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.IndexDistal] = hand[FingerId.Index, JointId.KnuckleMinor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.IndexTip] = hand[FingerId.Index, JointId.Tip].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.MiddleMetacarpal] = hand[FingerId.Middle, JointId.Root].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.MiddleProximal] = hand[FingerId.Middle, JointId.KnuckleMajor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.MiddleIntermediate] = hand[FingerId.Middle, JointId.KnuckleMid].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.MiddleDistal] = hand[FingerId.Middle, JointId.KnuckleMinor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.MiddleTip] = hand[FingerId.Middle, JointId.Tip].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.RingMetacarpal] = hand[FingerId.Ring, JointId.Root].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.RingProximal] = hand[FingerId.Ring, JointId.KnuckleMajor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.RingIntermediate] = hand[FingerId.Ring, JointId.KnuckleMid].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.RingDistal] = hand[FingerId.Ring, JointId.KnuckleMinor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.RingTip] = hand[FingerId.Ring, JointId.Tip].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.PinkyMetacarpal] = hand[FingerId.Little, JointId.Root].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.PinkyProximal] = hand[FingerId.Little, JointId.KnuckleMajor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.PinkyIntermediate] = hand[FingerId.Little, JointId.KnuckleMid].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.PinkyDistal] = hand[FingerId.Little, JointId.KnuckleMinor].Pose.ToPsiCoordinateSystem();
            joints[(int)HandJointIndex.PinkyTip] = hand[FingerId.Little, JointId.Tip].Pose.ToPsiCoordinateSystem();

            return new Hand(
                hand.IsTracked,
                hand.IsPinched,
                hand.IsGripped,
                joints);
        }
    }
}
