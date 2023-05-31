// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents one of the user's hands, as produced by the StereoKit-based <see cref="HandsSensor"/> component.
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
        /// Gets an empty Hand instance.
        /// </summary>
        public static Hand Empty => new (
            false,
            false,
            false,
            new CoordinateSystem[(int)HandJointIndex.MaxIndex]);

        /// <summary>
        /// Gets the definition of bones connecting the hand joints.
        /// </summary>
        public static List<(HandJointIndex Start, HandJointIndex End)> Bones => new ()
        {
            (HandJointIndex.Wrist, HandJointIndex.Palm),

            (HandJointIndex.Wrist, HandJointIndex.ThumbMetacarpal),
            (HandJointIndex.ThumbMetacarpal, HandJointIndex.ThumbProximal),
            (HandJointIndex.ThumbProximal, HandJointIndex.ThumbDistal),
            (HandJointIndex.ThumbDistal, HandJointIndex.ThumbTip),

            (HandJointIndex.Wrist, HandJointIndex.IndexMetacarpal),
            (HandJointIndex.IndexMetacarpal, HandJointIndex.IndexProximal),
            (HandJointIndex.IndexProximal, HandJointIndex.IndexIntermediate),
            (HandJointIndex.IndexIntermediate, HandJointIndex.IndexDistal),
            (HandJointIndex.IndexDistal, HandJointIndex.IndexTip),

            (HandJointIndex.Wrist, HandJointIndex.MiddleMetacarpal),
            (HandJointIndex.MiddleMetacarpal, HandJointIndex.MiddleProximal),
            (HandJointIndex.MiddleProximal, HandJointIndex.MiddleIntermediate),
            (HandJointIndex.MiddleIntermediate, HandJointIndex.MiddleDistal),
            (HandJointIndex.MiddleDistal, HandJointIndex.MiddleTip),

            (HandJointIndex.Wrist, HandJointIndex.RingMetacarpal),
            (HandJointIndex.RingMetacarpal, HandJointIndex.RingProximal),
            (HandJointIndex.RingProximal, HandJointIndex.RingIntermediate),
            (HandJointIndex.RingIntermediate, HandJointIndex.RingDistal),
            (HandJointIndex.RingDistal, HandJointIndex.RingTip),

            (HandJointIndex.Wrist, HandJointIndex.PinkyMetacarpal),
            (HandJointIndex.PinkyMetacarpal, HandJointIndex.PinkyProximal),
            (HandJointIndex.PinkyProximal, HandJointIndex.PinkyIntermediate),
            (HandJointIndex.PinkyIntermediate, HandJointIndex.PinkyDistal),
            (HandJointIndex.PinkyDistal, HandJointIndex.PinkyTip),
        };

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
    }
}
