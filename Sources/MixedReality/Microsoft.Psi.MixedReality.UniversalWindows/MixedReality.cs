// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using global::StereoKit;
    using global::StereoKit.Framework;
    using Microsoft.Psi.MixedReality.StereoKit;
    using Windows.Perception.Spatial;

    /// <summary>
    /// Represents mixed reality utility functions.
    /// </summary>
    public static class MixedReality
    {
        private const string DefaultWorldSpatialAnchorId = "_world";

        /// <summary>
        /// Gets the world coordinate system.
        /// </summary>
        public static SpatialCoordinateSystem WorldSpatialCoordinateSystem { get; private set; }

        /// <summary>
        /// Gets the spatial anchor helper.
        /// </summary>
        public static SpatialAnchorHelper SpatialAnchorHelper { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="MixedReality"/> class.
        /// Attempts to initialize the world coordinate system from a given spatial anchor.
        /// If no spatial anchor is given, it attempts to load a default spatial anchor.
        /// If the default spatial anchor is not found (e.g., if the app is being run for the first time),
        /// a stationary frame of reference for the world is created at the current location.
        /// </summary>
        /// <param name="worldSpatialAnchor">A spatial anchor to use for the world (optional).</param>
        /// <param name="regenerateDefaultWorldSpatialAnchorIfNeeded">Optional flag indicating whether to regenerate and persist default world spatial anchor if currently persisted anchor fails to localize in the current environment (default: false).</param>
        /// <remarks>
        /// This method should be called after SK.Initialize.
        /// </remarks>
        public static void Initialize(SpatialAnchor worldSpatialAnchor = null, bool regenerateDefaultWorldSpatialAnchorIfNeeded = false)
        {
            if (!SK.IsInitialized)
            {
                throw new InvalidOperationException($"StereoKit is not initialized. Call SK.Initialize before calling MixedReality.{nameof(Initialize)}.");
            }

            // Create the spatial anchor helper
            SpatialAnchorHelper = new SpatialAnchorHelper(SpatialAnchorManager.RequestStoreAsync().AsTask().GetAwaiter().GetResult());

            InitializeWorldCoordinateSystem(worldSpatialAnchor, regenerateDefaultWorldSpatialAnchorIfNeeded);

            // By default, don't render the hands or register with StereoKit physics system.
            Input.HandVisible(Handed.Max, false);
            Input.HandSolid(Handed.Max, false);
        }

        /// <summary>
        /// Initializes the world coordinate system for the application using a pre-defined spatial anchor,
        /// or creates it at a stationary frame of reference if it does not exist. Once initialized, the
        /// world coordinate system will be consistent across application sessions, unless the associated
        /// spatial anchor is modified or deleted.
        /// <param name="worldSpatialAnchor">A spatial anchor to use for the world (may be null).</param>
        /// <param name="regenerateDefaultWorldSpatialAnchorIfNeeded">Flag indicating whether to regenerate and persist default world spatial anchor if currently persisted anchor fails to localize in the current environment.</param>
        /// </summary>
        private static void InitializeWorldCoordinateSystem(SpatialAnchor worldSpatialAnchor, bool regenerateDefaultWorldSpatialAnchorIfNeeded)
        {
            static SpatialAnchor TryCreateDefaultWorldSpatialAnchor(SpatialStationaryFrameOfReference world)
            {
                // Save the world spatial coordinate system
                WorldSpatialCoordinateSystem = world.CoordinateSystem;

                // Create a spatial anchor to represent the world origin and persist it to the spatial
                // anchor store to ensure that the origin remains coherent between sessions.
                return SpatialAnchorHelper.TryCreateSpatialAnchor(DefaultWorldSpatialAnchorId, WorldSpatialCoordinateSystem);
            }

            // If no world anchor was given, try to load the default world spatial anchor if it was previously persisted
            worldSpatialAnchor ??= SpatialAnchorHelper.TryGetSpatialAnchor(DefaultWorldSpatialAnchorId);

            if (worldSpatialAnchor != null)
            {
                // Set the world spatial coordinate system using the spatial anchor
                WorldSpatialCoordinateSystem = worldSpatialAnchor.CoordinateSystem;
                if (regenerateDefaultWorldSpatialAnchorIfNeeded)
                {
                    var locator = SpatialLocator.GetDefault();
                    if (locator == null)
                    {
                        throw new Exception($"Could not get spatial locator.");
                    }

                    // determine whether we can localize in the current environment
                    var world = locator.CreateStationaryFrameOfReferenceAtCurrentLocation();
                    var success = world.CoordinateSystem.TryGetTransformTo(WorldSpatialCoordinateSystem) != null;
                    if (!success)
                    {
                        SpatialAnchorHelper.RemoveSpatialAnchor(DefaultWorldSpatialAnchorId);
                        worldSpatialAnchor = TryCreateDefaultWorldSpatialAnchor(world);
                        if (worldSpatialAnchor == null)
                        {
                            throw new Exception("Could not create the persistent world spatial anchor.");
                        }
                    }
                }
            }
            else
            {
                // Generate and persist the default world spatial anchor
                var locator = SpatialLocator.GetDefault();

                if (locator != null)
                {
                    // This creates a stationary frame of reference which we will use as our world origin
                    var world = locator.CreateStationaryFrameOfReferenceAtCurrentLocation();
                    worldSpatialAnchor = TryCreateDefaultWorldSpatialAnchor(world);

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

                // Query the pose of the world anchor. We use this pose for rendering correctly in the world,
                // and for transforming from world coordinates to StereoKit coordinates.
                StereoKitTransforms.WorldHierarchy = World.FromPerceptionAnchor(worldSpatialAnchor).ToMatrix();
                StereoKitTransforms.WorldToStereoKit = StereoKitTransforms.WorldHierarchy.Value.ToCoordinateSystem();

                // Inverting gives us a coordinate system that can be used for transforming from StereoKit to world coordinates.
                StereoKitTransforms.StereoKitToWorld = StereoKitTransforms.WorldToStereoKit.Invert();

                System.Diagnostics.Trace.WriteLine($"StereoKit origin: {StereoKitTransforms.StereoKitToWorld.Origin.X},{StereoKitTransforms.StereoKitToWorld.Origin.Y},{StereoKitTransforms.StereoKitToWorld.Origin.Z}");
                SK.AddStepper(new SpatialTransformsUpdater(worldSpatialAnchor));

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

        private class SpatialTransformsUpdater : IStepper
        {
            private readonly SpatialAnchor worldSpatialAnchor;
            private SpatialCoordinateSystem stereoKitSpatialCoordinateSystem;

            public SpatialTransformsUpdater(SpatialAnchor worldSpatialAnchor)
            {
                this.worldSpatialAnchor = worldSpatialAnchor;
                this.stereoKitSpatialCoordinateSystem = StereoKitTransforms.StereoKitToWorld.TryConvertPsiCoordinateSystemToSpatialCoordinateSystem();
            }

            /// <inheritdoc />
            public bool Enabled => true;

            /// <inheritdoc />
            public bool Initialize() => true;

            /// <inheritdoc />
            public void Step()
            {
                this.stereoKitSpatialCoordinateSystem ??= StereoKitTransforms.StereoKitToWorld.TryConvertPsiCoordinateSystemToSpatialCoordinateSystem();

                if (this.stereoKitSpatialCoordinateSystem is not null)
                {
                    if (this.stereoKitSpatialCoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem() is null)
                    {
                        StereoKitTransforms.StereoKitToWorld = null;
                        StereoKitTransforms.WorldToStereoKit = null;
                        StereoKitTransforms.WorldHierarchy = null;
                    }
                    else
                    {
                        // Query the pose of the world anchor. We use this pose for rendering correctly in the world,
                        // and for transforming from world coordinates to StereoKit coordinates.
                        StereoKitTransforms.WorldHierarchy = World.FromPerceptionAnchor(this.worldSpatialAnchor).ToMatrix();
                        StereoKitTransforms.WorldToStereoKit = StereoKitTransforms.WorldHierarchy.Value.ToCoordinateSystem();

                        // Inverting gives us a coordinate system that can be used for transforming from StereoKit to world coordinates.
                        StereoKitTransforms.StereoKitToWorld = StereoKitTransforms.WorldToStereoKit.Invert();
                    }
                }
            }

            /// <inheritdoc />
            public void Shutdown()
            {
            }
        }
    }
}
