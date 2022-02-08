// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Static StereoKit transforms which are applied in/out of StereoKit from \psi.
    /// </summary>
    public static class StereoKitTransforms
    {
        /// <summary>
        /// Gets or sets the starting pose of StereoKit (the headset) in the world (in \psi basis).
        /// </summary>
        public static CoordinateSystem StereoKitStartingPose { get; set; } = new CoordinateSystem();

        /// <summary>
        /// Gets or sets the inverse of the StereoKit starting pose in the world.
        /// </summary>
        public static CoordinateSystem StereoKitStartingPoseInverse { get; set; } = new CoordinateSystem();
    }
}
