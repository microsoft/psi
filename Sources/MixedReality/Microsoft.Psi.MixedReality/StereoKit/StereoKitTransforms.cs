// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Static StereoKit transforms which are applied in/out of StereoKit from \psi.
    /// </summary>
    public static class StereoKitTransforms
    {
        /// <summary>
        /// Gets the "world hierarchy" for rendering.
        /// Push this matrix onto StereoKit's <see cref="Hierarchy"/> stack to render content coherently in the world.
        /// </summary>
        /// <remarks>
        /// This matrix is pushed automatically by the <see cref="StereoKitRenderer"/> base class for new rendering components.
        /// The value is null when the HoloLens loses localization.
        /// </remarks>
        public static Matrix? WorldHierarchy { get; internal set; } = Matrix.Identity;

        /// <summary>
        /// Gets or sets the transform from StereoKit to the world.
        /// </summary>
        internal static CoordinateSystem StereoKitToWorld { get; set; } = new CoordinateSystem();

        /// <summary>
        /// Gets or sets the the transform from the world to StereoKit.
        /// </summary>
        internal static CoordinateSystem WorldToStereoKit { get; set; } = new CoordinateSystem();
    }
}
