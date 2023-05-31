// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.OpenXR
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents one of the user's hands, as produced by the OpenXR-based <see cref="HandsSensor"/> component.
    /// </summary>
    public class Hand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hand"/> class.
        /// </summary>
        /// <param name="isActive">Value indicating whether or not the hand tracker was active for this hand result.</param>
        /// <param name="joints">Finger joint poses (index by <see cref="HandJointIndex"/>).</param>
        /// <param name="jointsValid">Values indicating whether or not the pose for each joint is valid.</param>
        /// <param name="jointsTracked">Values indicating whether or not the pose for each joint was tracked. (If false, the pose was inferred).</param>
        public Hand(bool isActive, CoordinateSystem[] joints, bool[] jointsValid, bool[] jointsTracked)
        {
            this.IsActive = isActive;
            this.Joints = joints;
            this.JointsValid = jointsValid;
            this.JointsTracked = jointsTracked;
        }

        /// <summary>
        /// Gets an empty HandXR instance.
        /// </summary>
        public static Hand Empty => new (
            false,
            new CoordinateSystem[(int)HandJointIndex.MaxIndex],
            new bool[(int)HandJointIndex.MaxIndex],
            new bool[(int)HandJointIndex.MaxIndex]);

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
        /// Gets a value indicating whether or not the hand tracker was active for this hand result.
        /// </summary>
        /// <remarks>
        /// If false, it indicates the hand tracker did not detect the hand input, and all joint poses are invalid.</remarks>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets finger joint poses in \psi basis (indexed by <see cref="HandJointIndex"/>).
        /// </summary>
        public CoordinateSystem[] Joints { get; private set; }

        /// <summary>
        /// Gets values indicating whether or not the pose for each joint is valid.
        /// </summary>
        public bool[] JointsValid { get; private set; }

        /// <summary>
        /// Gets values indicating whether or not the pose for each joint was tracked. (If false, the pose was inferred).
        /// </summary>
        public bool[] JointsTracked { get; private set; }

        /// <summary>
        /// Gets the joint in \psi basis specified by a <see cref="HandJointIndex"/>.
        /// </summary>
        /// <param name="handJointIndex">The joint index.</param>
        /// <returns>The corresponding joint.</returns>
        public CoordinateSystem this[HandJointIndex handJointIndex] => this.Joints[(int)handJointIndex];
    }
}
