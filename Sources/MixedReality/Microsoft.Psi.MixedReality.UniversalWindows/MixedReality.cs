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
    /// <remarks>SK.Initialize must be called before using this class.</remarks>
    public static class MixedReality
    {
        private const string DefaultWorldSpatialAnchorId = "_world";
        private static readonly SpatialLocator DeviceLocator = SpatialLocator.GetDefault();

        /// <summary>
        /// Gets the world coordinate system.
        /// </summary>
        public static SpatialCoordinateSystem WorldSpatialCoordinateSystem { get; private set; }

        /// <summary>
        /// Gets the id of the world spatial anchor.
        /// </summary>
        public static string WorldSpatialAnchorId { get; private set; }

        /// <summary>
        /// Gets the spatial anchor provider.
        /// </summary>
        public static ISpatialAnchorProvider SpatialAnchorProvider { get; private set; }

        /// <summary>
        /// Gets the current localization state.
        /// </summary>
        public static LocalizationState LocalizationState { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="MixedReality"/> class.
        /// Attempts to initialize the world coordinate system from a given spatial anchor.
        /// If no spatial anchor is given, it attempts to load a default spatial anchor.
        /// If the default spatial anchor is not found (e.g., if the app is being run for the first time),
        /// a stationary frame of reference for the world is created at the current location.
        /// </summary>
        /// <param name="worldSpatialAnchor">An already created spatial anchor to use for the world root (optional).</param>
        /// <param name="regenerateDefaultWorldSpatialAnchorIfNeeded">Optional flag indicating whether to regenerate and persist
        /// the world spatial anchor if the current world anchor cannot be localized in the current environment (default: false).</param>
        /// <remarks>
        /// This method should be called after SK.Initialize.
        /// </remarks>
        public static void Initialize(SpatialAnchor worldSpatialAnchor = null, bool regenerateDefaultWorldSpatialAnchorIfNeeded = false)
        {
            if (!SK.IsInitialized)
            {
                throw new InvalidOperationException($"StereoKit is not initialized. Call SK.Initialize before accessing the {nameof(MixedReality)} class.");
            }

            // By default, don't render the hands or register with StereoKit physics system.
            Input.HandVisible(Handed.Max, false);
            Input.HandSolid(Handed.Max, false);

            // Set the default world coordinate system
            SetWorldCoordinateSystem(
                new LocalSpatialAnchorProvider(SpatialAnchorManager.RequestStoreAsync().AsTask().GetAwaiter().GetResult()),
                worldSpatialAnchor,
                DefaultWorldSpatialAnchorId,
                createWorldSpatialAnchorIfNeeded: true,
                regenerateWorldSpatialAnchorIfNeeded: regenerateDefaultWorldSpatialAnchorIfNeeded);
        }

        /// <summary>
        /// Sets the world coordinate system for the application using a spatial anchor. The spatial anchor to use
        /// may be explicitly provided or loaded from a spatial anchor provider by providing the unique identifier
        /// of the spatial anchor. If no spatial anchor provider is specified, the app-local spatial anchor store
        /// will be used. If no spatial anchor is provided or the specified anchor identifier cannot be found, one
        /// may optionally be created at the current location.
        /// </summary>
        /// <param name="spatialAnchorProvider">The spatial anchor provider to use.</param>
        /// <param name="worldSpatialAnchor">An already created spatial anchor to use for the world root (optional).</param>
        /// <param name="worldSpatialAnchorId">The id of the world spatial anchor.</param>
        /// <param name="createWorldSpatialAnchorIfNeeded">
        /// Optional flag indicating whether to create a new world spatial anchor if no world anchor was supplied or
        /// the specified world anchor could not be found (default: true).
        /// </param>
        /// <param name="regenerateWorldSpatialAnchorIfNeeded">
        /// Optional flag indicating whether to regenerate and persist the world spatial anchor if the current world
        /// anchor cannot be localized in the current environment (default: false).
        /// </param>
        /// <remarks>
        /// This method should be called after SK.Initialize.
        /// </remarks>
        public static void SetWorldCoordinateSystem(
            ISpatialAnchorProvider spatialAnchorProvider = null,
            SpatialAnchor worldSpatialAnchor = null,
            string worldSpatialAnchorId = DefaultWorldSpatialAnchorId,
            bool createWorldSpatialAnchorIfNeeded = true,
            bool regenerateWorldSpatialAnchorIfNeeded = false)
        {
            SpatialAnchorProvider = spatialAnchorProvider ?? new LocalSpatialAnchorProvider(SpatialAnchorManager.RequestStoreAsync().AsTask().GetAwaiter().GetResult());
            WorldSpatialAnchorId = worldSpatialAnchorId;
            LocalizationState = LocalizationState.NotLocalized;

            // If using a supplied spatial anchor for the world, create it using the spatial anchor provider
            if (worldSpatialAnchor?.CoordinateSystem != null)
            {
                (worldSpatialAnchor, worldSpatialAnchorId) = SpatialAnchorProvider.TryCreateSpatialAnchor(WorldSpatialAnchorId, worldSpatialAnchor.CoordinateSystem);
                if (worldSpatialAnchorId != null)
                {
                    WorldSpatialAnchorId = worldSpatialAnchorId;
                }
            }
            else
            {
                try
                {
                    // Try to lookup the world spatial anchor by id using the spatial anchor provider
                    worldSpatialAnchor = SpatialAnchorProvider.TryGetSpatialAnchor(WorldSpatialAnchorId);
                }
                catch when (createWorldSpatialAnchorIfNeeded)
                {
                    // We will create a new world spatial anchor if the specified one was not found, so don't throw an exception
                }
            }

            Pose? worldAnchorPose = null;

            // Check the current pose of the device
            var currentDevicePose = DeviceLocator.CreateStationaryFrameOfReferenceAtCurrentLocation().CoordinateSystem;

            // If we need to create a world spatial anchor or we need to regenerate it because we cannot localize it
            if ((worldSpatialAnchor == null && createWorldSpatialAnchorIfNeeded) ||
                (worldSpatialAnchor != null && currentDevicePose?.TryGetTransformTo(worldSpatialAnchor.CoordinateSystem) == null && regenerateWorldSpatialAnchorIfNeeded))
            {
                if (worldSpatialAnchor != null)
                {
                    // If we need to regenerate the world spatial anchor, delete the existing one first
                    SpatialAnchorProvider.RemoveSpatialAnchor(WorldSpatialAnchorId);
                }

                // Create a new world spatial anchor at the current pose of the device
                (worldSpatialAnchor, worldSpatialAnchorId) = SpatialAnchorProvider.TryCreateSpatialAnchor(WorldSpatialAnchorId, currentDevicePose);

                // Throw an exception if that did not work
                if (worldSpatialAnchor == null)
                {
                    throw new Exception($"Could not create the world spatial anchor.");
                }

                WorldSpatialAnchorId = worldSpatialAnchorId;
            }

            if (worldSpatialAnchor != null)
            {
                // Initialize the various static transforms we use for ensuring all 3d information is represented in the same world coordinate system.
                WorldSpatialCoordinateSystem = worldSpatialAnchor.CoordinateSystem;
                worldAnchorPose = World.FromPerceptionAnchor(worldSpatialAnchor);
                StereoKitTransforms.Initialize(worldAnchorPose);
                LocalizationState = LocalizationState.Localized;
            }

            // Register a step function that checks for when StereoKit's "World" has changed or become invalid due to lost localization.
            // Remove any existing step function that may have been registered in a previous call.
            SK.RemoveStepper<StereoKitTransformsUpdater>();
            SK.AddStepper(new StereoKitTransformsUpdater(worldAnchorPose));

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

        /// <summary>
        /// An internal StereoKit stepper that checks at each frame whether the world spatial anchor has
        /// changed or become invalid due to lost localization and updates the transforms accordingly.
        /// </summary>
        private class StereoKitTransformsUpdater : IStepper
        {
            private Pose? currentWorldAnchorPose;

            /// <summary>
            /// Initializes a new instance of the <see cref="StereoKitTransformsUpdater"/> class.
            /// </summary>
            /// <param name="worldAnchorPose">The pose of the world anchor in StereoKit terms.</param>
            public StereoKitTransformsUpdater(Pose? worldAnchorPose)
            {
                this.currentWorldAnchorPose = worldAnchorPose;
            }

            /// <inheritdoc />
            public bool Enabled => true;

            /// <inheritdoc />
            public bool Initialize() => true;

            /// <inheritdoc />
            public void Step()
            {
                try
                {
                    var worldSpatialAnchor = SpatialAnchorProvider.TryGetSpatialAnchor(WorldSpatialAnchorId);

                    if (worldSpatialAnchor != null)
                    {
                        var worldAnchorPose = World.FromPerceptionAnchor(worldSpatialAnchor);

                        // Check if the world anchor pose is valid (to be a valid orientation, the length of the quaternion vector should be ~1)
                        var quaternionLength = worldAnchorPose.orientation.q.Length();
                        var validWorldAnchorPose = !PosesEqual(worldAnchorPose, Pose.Identity, 0) && quaternionLength >= 0.9f && quaternionLength <= 1.1f;

                        if (validWorldAnchorPose)
                        {
                            if (!PosesEqual(this.currentWorldAnchorPose, worldAnchorPose, 0.1f))
                            {
                                // If the pose of the world anchor has changed sufficiently, it usually means that StereoKit has refreshed its own
                                // "World" coordinate system, so we need to update our transforms accordingly.
                                WorldSpatialCoordinateSystem = worldSpatialAnchor.CoordinateSystem;
                                StereoKitTransforms.Initialize(worldAnchorPose);
                                this.currentWorldAnchorPose = worldAnchorPose;
                            }

                            LocalizationState = LocalizationState.Localized;
                        }
                        else if (this.currentWorldAnchorPose != null)
                        {
                            // Failure to localize the world anchor can be indicated by StereoKit returning an invalid pose
                            this.currentWorldAnchorPose = null;
                            StereoKitTransforms.Initialize(null);

                            // World anchor was found but is invalid in the current state
                            LocalizationState = LocalizationState.Invalidated;
                        }
                        else
                        {
                            // World anchor was previously invalidated and is still not valid
                            LocalizationState = LocalizationState.Localizing;
                        }
                    }
                    else
                    {
                        // Still trying to locate the world anchor
                        LocalizationState = LocalizationState.Localizing;
                    }
                }
                catch
                {
                    // If an exception occurs, remove this stepper
                    SK.RemoveStepper(this);
                    throw;
                }
            }

            /// <inheritdoc />
            public void Shutdown()
            {
            }

            private static bool PosesEqual(Pose? pose1, Pose? pose2, float epsilon)
            {
                if (pose1 == null || pose2 == null)
                {
                    return false;
                }

                return Math.Abs(pose1.Value.position.x - pose2.Value.position.x) <= epsilon &&
                    Math.Abs(pose1.Value.position.y - pose2.Value.position.y) <= epsilon &&
                    Math.Abs(pose1.Value.position.z - pose2.Value.position.z) <= epsilon &&
                    Math.Abs(pose1.Value.orientation.x - pose2.Value.orientation.x) <= epsilon &&
                    Math.Abs(pose1.Value.orientation.y - pose2.Value.orientation.y) <= epsilon &&
                    Math.Abs(pose1.Value.orientation.z - pose2.Value.orientation.z) <= epsilon &&
                    Math.Abs(pose1.Value.orientation.w - pose2.Value.orientation.w) <= epsilon;
            }
        }
    }
}
