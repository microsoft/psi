// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Static StereoKit transforms which are applied in/out of StereoKit from \psi.
    /// </summary>
    /// <remarks>These transforms are used for converting \psi world coordinates to/from StereoKit coordinates when needed,
    /// including on input from StereoKit -> \psi, and on output (rendering) \psi -> StereoKit.</remarks>
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
        public static Matrix? WorldHierarchy { get; private set; }

        /// <summary>
        /// Gets the transform from StereoKit to the world.
        /// </summary>
        public static CoordinateSystem StereoKitToWorld { get; private set; }

        /// <summary>
        /// Gets the transform from the world to StereoKit.
        /// </summary>
        public static CoordinateSystem WorldToStereoKit { get; private set; }

        /// <summary>
        /// Initialize the various StereoKit transforms according to a given world pose.
        /// </summary>
        /// <param name="worldPose">World pose to initialize the various StereoKit transforms with.
        /// If null, then set all transforms to null.</param>
        internal static void Initialize(Pose? worldPose)
        {
            if (worldPose is null)
            {
                WorldHierarchy = null;
                StereoKitToWorld = null;
                WorldToStereoKit = null;
                System.Diagnostics.Trace.WriteLine($"StereoKit transforms null");
            }
            else
            {
                // Use the world pose for converting from world coordinates to StereoKit coordinates (e.g., for rendering)
                WorldHierarchy = worldPose.Value.ToMatrix();
                WorldToStereoKit = WorldHierarchy.Value.ToCoordinateSystem();

                // Inverting gives us a coordinate system that can be used for transforming from StereoKit to world coordinates.
                StereoKitToWorld = WorldToStereoKit.Invert();

                System.Diagnostics.Trace.WriteLine($"StereoKit transforms initialized: {StereoKitToWorld.Origin.X},{StereoKitToWorld.Origin.Y},{StereoKitToWorld.Origin.Z}");
            }
        }
    }
}
