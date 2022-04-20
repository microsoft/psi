// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Represents one of the user's hands.
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
    }
}
