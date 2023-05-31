// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Windows.Perception.Spatial;

    /// <summary>
    /// Represents a helper for spatial anchor operations.
    /// </summary>
    public class SpatialAnchorHelper
    {
        private readonly SpatialAnchorStore spatialAnchorStore;
        private readonly ConcurrentDictionary<string, SpatialAnchor> spatialAnchors = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialAnchorHelper"/> class.
        /// </summary>
        /// <param name="spatialAnchorStore">The spatial anchor store.</param>
        public SpatialAnchorHelper(SpatialAnchorStore spatialAnchorStore)
        {
            this.spatialAnchorStore = spatialAnchorStore;
            var persistedAnchors = spatialAnchorStore.GetAllSavedAnchors();
            this.spatialAnchors = new ConcurrentDictionary<string, SpatialAnchor>(persistedAnchors);
        }

        /// <summary>
        /// Creates a persisted spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/>.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <returns>The new spatial anchor, or null if the creation failed.</returns>
        public SpatialAnchor TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem)
        {
            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem);

            if (spatialAnchor != null)
            {
                // Try to persist the spatial anchor to the store
                if (this.spatialAnchorStore.TrySave(id, spatialAnchor))
                {
                    // Save it in the in-memory dictionary of spatial anchors
                    this.spatialAnchors[id] = spatialAnchor;
                }
                else
                {
                    spatialAnchor = null;
                }
            }

            return spatialAnchor;
        }

        /// <summary>
        /// Creates a persisted spatial anchor at the supplied <see cref="CoordinateSystem"/>.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="coordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <returns>The new spatial anchor, or null if the creation failed.</returns>
        public SpatialAnchor TryCreateSpatialAnchor(string id, CoordinateSystem coordinateSystem)
        {
            SpatialAnchor spatialAnchor = null;
            var spatialCoordinateSystem = coordinateSystem.TryConvertPsiCoordinateSystemToSpatialCoordinateSystem();
            if (spatialCoordinateSystem != null)
            {
                spatialAnchor = this.TryCreateSpatialAnchor(id, spatialCoordinateSystem);
            }

            return spatialAnchor;
        }

        /// <summary>
        /// Creates a persisted spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/> with a positional offset.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <param name="translation">The rigid positional offset from the coordinate system's origin in HoloLens basis.</param>
        /// <returns>The new spatial anchor, or null if the creation failed.</returns>
        /// <remarks>HoloLens basis means +x right, +y up, -z forward.</remarks>
        public SpatialAnchor TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation)
        {
            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem, translation);

            if (spatialAnchor != null)
            {
                // Try to persist the spatial anchor to the store
                if (this.spatialAnchorStore.TrySave(id, spatialAnchor))
                {
                    // Save it in the in-memory dictionary of spatial anchors
                    this.spatialAnchors[id] = spatialAnchor;
                }
                else
                {
                    spatialAnchor = null;
                }
            }

            return spatialAnchor;
        }

        /// <summary>
        /// Creates a persisted spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/> with offsets in position and rotation.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <param name="translation">The rigid positional offset from the coordinate system's origin in HoloLens basis.</param>
        /// <param name="rotation">The rigid rotation from the coordinate system's origin in HoloLens basis.</param>
        /// <returns>The new spatial anchor, or null if the creation failed.</returns>
        /// <remarks>HoloLens basis means +x right, +y up, -z forward.</remarks>
        public SpatialAnchor TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation, System.Numerics.Quaternion rotation)
        {
            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem, translation, rotation);

            if (spatialAnchor != null)
            {
                // Try to persist the spatial anchor to the store
                if (this.spatialAnchorStore.TrySave(id, spatialAnchor))
                {
                    // Save it in the in-memory dictionary of spatial anchors
                    this.spatialAnchors[id] = spatialAnchor;
                }
                else
                {
                    spatialAnchor = null;
                }
            }

            return spatialAnchor;
        }

        /// <summary>
        /// Creates a persisted spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/> with a transform offset in \psi basis.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <param name="relativeOffset">The amount of relative offset applied to the given coordinate system when creating the spatial anchor.</param>
        /// <returns>The new spatial anchor, or null if the creation failed.</returns>
        public SpatialAnchor TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, CoordinateSystem relativeOffset)
        {
            Matrix4x4.Decompose(relativeOffset.RebaseToHoloLensSystemMatrix(), out _, out var rotation, out var translation);
            return this.TryCreateSpatialAnchor(id, spatialCoordinateSystem, translation, rotation);
        }

        /// <summary>
        /// Updates the coordinate system of a persisted spatial anchor.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="coordinateSystem">The new coordinate system of the spatial anchor.</param>
        /// <returns>The updated spatial anchor, or null if the update failed.</returns>
        /// <remarks>
        /// If the spatial anchor was not found in the store, a new one will be created. Updating
        /// an existing spatial anchor with a null coordinate system will cause it to be removed.
        /// In this case, the existing spatial anchor is returned.
        /// </remarks>
        public SpatialAnchor TryUpdateSpatialAnchor(string id, CoordinateSystem coordinateSystem)
        {
            this.spatialAnchors.TryGetValue(id, out var spatialAnchor);
            if (spatialAnchor?.CoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem() != coordinateSystem)
            {
                this.RemoveSpatialAnchor(id);
                if (coordinateSystem != null)
                {
                    spatialAnchor = this.TryCreateSpatialAnchor(id, coordinateSystem);
                }
            }

            return spatialAnchor;
        }

        /// <summary>
        /// Removes the specified spatial anchor from the store.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor to remove.</param>
        public void RemoveSpatialAnchor(string id)
        {
            if (this.spatialAnchors.TryRemove(id, out _))
            {
                this.spatialAnchorStore.Remove(id);
            }
        }

        /// <summary>
        /// Gets all spatial anchors in the store.
        /// </summary>
        /// <returns>The map of spatial anchors.</returns>
        public Dictionary<string, SpatialAnchor> GetAllSpatialAnchors()
        {
            return this.spatialAnchors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Gets the coordinate systems for all spatial anchors in the store.
        /// </summary>
        /// <returns>The map of spatial anchor coordinate systems.</returns>
        public Dictionary<string, CoordinateSystem> GetAllSpatialAnchorCoordinateSystems()
        {
            // Spatial anchors may not always be locatable at all points in time, so the result may contain null values
            return new Dictionary<string, CoordinateSystem>(
                this.spatialAnchors
                    .Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.CoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem())));
        }

        /// <summary>
        /// Gets the specified spatial anchor from the store.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <returns>The spatial anchor, or null if it was not found.</returns>
        public SpatialAnchor TryGetSpatialAnchor(string id)
        {
            return this.spatialAnchors.TryGetValue(id, out var spatialAnchor) ? spatialAnchor : null;
        }
    }
}
