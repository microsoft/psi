// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.OpenXR
{
    using System;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Implements operators.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Interpolates between two <see cref="Hand"/> poses by interpolating the <see cref="CoordinateSystem"/>
        /// poses of each joint. Spherical linear interpolation (<see cref="System.Numerics.Quaternion.Slerp"/>)
        /// is used for rotation, and linear interpolation is used for translation.
        /// </summary>
        /// <param name="hand1">The first <see cref="Hand"/> pose.</param>
        /// <param name="hand2">The second <see cref="Hand"/> pose.</param>
        /// <param name="amount">The amount to interpolate between the two hand poses.
        /// A value between 0 and 1 will return an interpolation between the two values.
        /// A value outside the 0-1 range will generate an extrapolated result.</param>
        /// <returns>The interpolated <see cref="Hand"/> pose.</returns>
        /// <remarks>Returns null if either input hand is null.</remarks>
        public static Hand InterpolateHands(Hand hand1, Hand hand2, double amount)
        {
            if (hand1 is null || hand2 is null)
            {
                return null;
            }

            // Interpolate each joint pose separately
            int numJoints = (int)HandJointIndex.MaxIndex;
            var interpolatedJoints = new CoordinateSystem[numJoints];
            for (int i = 0; i < numJoints; i++)
            {
                interpolatedJoints[i] = Spatial.Euclidean.Operators.InterpolateCoordinateSystems(hand1.Joints[i], hand2.Joints[i], amount);
            }

            // Use values from hand2 for boolean members if the amount we are
            // interpolating is greater than 0.5. Otherwise select from hand1.
            bool[] interpolatedJointsValid = null;
            bool[] interpolatedJointsTracked = null;

            if (hand1.JointsValid is not null && hand2.JointsValid is not null)
            {
                interpolatedJointsValid = new bool[numJoints];
                Array.Copy(amount > 0.5 ? hand2.JointsValid : hand1.JointsValid, interpolatedJointsValid, numJoints);
            }

            if (hand1.JointsTracked is not null && hand2.JointsTracked is not null)
            {
                interpolatedJointsTracked = new bool[numJoints];
                Array.Copy(amount > 0.5 ? hand2.JointsTracked : hand1.JointsTracked, interpolatedJointsTracked, numJoints);
            }

            return new Hand(
                amount > 0.5 ? hand2.IsActive : hand1.IsActive,
                interpolatedJoints,
                interpolatedJointsValid,
                interpolatedJointsTracked);
        }
    }
}
