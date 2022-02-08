// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Numerics;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using StereoKit;
    using Windows.Perception.Spatial;

    /// <summary>
    /// Represents mixed reality utility functions.
    /// </summary>
    public static class MixedReality
    {
        private const string WorldSpatialAnchorId = "_world";

        /// <summary>
        /// Gets the world coordinate system.
        /// </summary>
        public static SpatialCoordinateSystem WorldSpatialCoordinateSystem { get; private set; }

        /// <summary>
        /// Gets the spatial anchor helper.
        /// </summary>
        public static SpatialAnchorHelper SpatialAnchorHelper { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="MixedReality"/> class. Attempts to initialize
        /// the world coordinate system from a persisted spatial anchor. If one is not found, a stationary
        /// frame of reference is created at the current location and its position is used as the world
        /// coordinate system.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method should be called after SK.Initialize.
        /// </remarks>
        public static async Task InitializeAsync()
        {
            if (!SK.IsInitialized)
            {
                throw new InvalidOperationException("StereoKit is not initialized. Call SK.Initialize before calling MixedReality.InitializeAsync.");
            }

            // Create the spatial anchor helper
            SpatialAnchorHelper = new SpatialAnchorHelper(await SpatialAnchorManager.RequestStoreAsync());

            InitializeWorldCoordinateSystem();
        }

        /// <summary>
        /// Initializes the world coordinate system for the application using a pre-defined spatial anchor,
        /// or creates it at a stationary frame of reference if it does not exist. Once initialized, the
        /// world coordinate system will be consistent across application sessions, unless the associated
        /// spatial anchor is modified or deleted.
        /// </summary>
        private static void InitializeWorldCoordinateSystem()
        {
            // Try to get a previously saved world spatial anchor
            var worldSpatialAnchor = SpatialAnchorHelper.TryGetSpatialAnchor(WorldSpatialAnchorId);

            if (worldSpatialAnchor != null)
            {
                // Set the world spatial coordinate system using the spatial anchor
                WorldSpatialCoordinateSystem = worldSpatialAnchor.CoordinateSystem;
            }
            else
            {
                var locator = SpatialLocator.GetDefault();

                if (locator != null)
                {
                    // This creates a stationary frame of reference which we will use as our world origin
                    var world = locator.CreateStationaryFrameOfReferenceAtCurrentLocation();

                    // Save the world spatial coordinate system
                    WorldSpatialCoordinateSystem = world.CoordinateSystem;

                    // Create a spatial anchor to represent the world origin and persist it to the spatial
                    // anchor store to ensure that the origin remains coherent between sessions.
                    worldSpatialAnchor = SpatialAnchorHelper.TryCreateSpatialAnchor(WorldSpatialAnchorId, WorldSpatialCoordinateSystem);

                    if (worldSpatialAnchor == null)
                    {
                        System.Diagnostics.Trace.WriteLine($"WARNING: Could not create the persistent world spatial anchor.");
                    }
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine($"WARNING: Could not get spatial locator (expected in StereoKit on desktop).");
                }
            }

            if (worldSpatialAnchor != null)
            {
                // At startup, we need to capture the pose of StereoKit with respect to the world anchor, and vice versa.
                // These transforms will allow us to convert world coordinates to/from StereoKit coordinates where needed:
                // on input from StereoKit -> \psi, and on output (rendering) \psi -> StereoKit

                // The pose of world anchor is essentially the inverse of the startup pose of StereoKit with respect to the world.
                Matrix4x4 worldStereoKitMatrix = World.FromPerceptionAnchor(worldSpatialAnchor).ToMatrix();
                StereoKitTransforms.StereoKitStartingPoseInverse = new CoordinateSystem(worldStereoKitMatrix.ToMathNetMatrix().ChangeBasisHoloLensToPsi());

                // Inverting then gives us the starting pose of StereoKit in the "world" (relative to the world anchor).
                StereoKitTransforms.StereoKitStartingPose = StereoKitTransforms.StereoKitStartingPoseInverse.Invert();

                System.Diagnostics.Trace.WriteLine($"StereoKit origin: {StereoKitTransforms.StereoKitStartingPose.Origin.X},{StereoKitTransforms.StereoKitStartingPose.Origin.Y},{StereoKitTransforms.StereoKitStartingPose.Origin.Z}");

                // TODO: It would be nice if we could actually just shift the origin coordinate system in StereoKit
                // to the pose currently defined in StereoKitTransforms.WorldPose.
                // There's currently an open issue for this: https://github.com/maluoi/StereoKit/issues/189

                // Simply setting the renderer camera root does not work, as its transform appears to be applied in the wrong order.
                // E.g., if the starting StereoKit pose is at a yaw rotation of 180 degrees, we would want to apply that transform
                // first, then apply the transform of the headset pose (perhaps pitching up). Instead, it appears that the headset
                // pose is applied first (e.g., pitching up), and *then* the Renderer.CameraRoot transform is applied (yaw of 180 degrees)
                // which in this example manifests as the pitch going down, opposite of what we desired.
                ////Renderer.CameraRoot = stereoKitTransform.Inverse;
            }
        }
    }
}
