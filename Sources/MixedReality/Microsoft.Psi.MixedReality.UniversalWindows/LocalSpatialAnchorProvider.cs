// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Windows.Perception.Spatial;

    /// <summary>
    /// Represents a provider for locally-persisted or in-memory spatial anchors.
    /// </summary>
    public class LocalSpatialAnchorProvider : ISpatialAnchorProvider
    {
        private readonly SpatialAnchorStore spatialAnchorStore;
        private readonly ConcurrentDictionary<string, SpatialAnchor> spatialAnchors;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSpatialAnchorProvider"/> class
        /// which manages non-persistent in-memory spatial anchors.
        /// </summary>
        public LocalSpatialAnchorProvider()
        {
            this.spatialAnchors = new ();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalSpatialAnchorProvider"/> class
        /// which manages locally-persisted spatial anchors.
        /// </summary>
        /// <param name="spatialAnchorStore">The spatial anchor store.</param>
        public LocalSpatialAnchorProvider(SpatialAnchorStore spatialAnchorStore)
        {
            this.spatialAnchorStore = spatialAnchorStore ?? throw new ArgumentException(nameof(spatialAnchorStore));
            this.spatialAnchors = new (this.spatialAnchorStore.GetAllSavedAnchors());
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem)
        {
            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem);

            if (spatialAnchor != null)
            {
                // Try to persist the spatial anchor to the store if available
                if (this.spatialAnchorStore == null || this.spatialAnchorStore.TrySave(id, spatialAnchor))
                {
                    // Save it in the in-memory dictionary of spatial anchors
                    this.spatialAnchors[id] = spatialAnchor;
                }
                else
                {
                    spatialAnchor = null;
                }
            }

            return (spatialAnchor, spatialAnchor != null ? id : null);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, CoordinateSystem coordinateSystem)
        {
            var spatialCoordinateSystem = coordinateSystem.TryConvertPsiCoordinateSystemToSpatialCoordinateSystem();
            if (spatialCoordinateSystem != null)
            {
                return this.TryCreateSpatialAnchor(id, spatialCoordinateSystem);
            }

            return default;
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation)
        {
            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem, translation);

            if (spatialAnchor != null)
            {
                // Try to persist the spatial anchor to the store
                if (this.spatialAnchorStore == null || this.spatialAnchorStore.TrySave(id, spatialAnchor))
                {
                    // Save it in the in-memory dictionary of spatial anchors
                    this.spatialAnchors[id] = spatialAnchor;
                }
                else
                {
                    spatialAnchor = null;
                }
            }

            return (spatialAnchor, spatialAnchor != null ? id : null);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation, System.Numerics.Quaternion rotation)
        {
            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem, translation, rotation);

            if (spatialAnchor != null)
            {
                // Try to persist the spatial anchor to the store
                if (this.spatialAnchorStore == null || this.spatialAnchorStore.TrySave(id, spatialAnchor))
                {
                    // Save it in the in-memory dictionary of spatial anchors
                    this.spatialAnchors[id] = spatialAnchor;
                }
                else
                {
                    spatialAnchor = null;
                }
            }

            return (spatialAnchor, spatialAnchor != null ? id : null);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, CoordinateSystem relativeOffset)
        {
            Matrix4x4.Decompose(relativeOffset.RebaseToHoloLensSystemMatrix(), out _, out var rotation, out var translation);
            return this.TryCreateSpatialAnchor(id, spatialCoordinateSystem, translation, rotation);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryUpdateSpatialAnchor(string id, CoordinateSystem coordinateSystem)
        {
            this.spatialAnchors.TryGetValue(id, out var spatialAnchor);
            if (spatialAnchor?.CoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem() != coordinateSystem)
            {
                this.RemoveSpatialAnchor(id);
                if (coordinateSystem != null)
                {
                    return this.TryCreateSpatialAnchor(id, coordinateSystem);
                }
            }

            return default;
        }

        /// <inheritdoc/>
        public void RemoveSpatialAnchor(string id)
        {
            if (this.spatialAnchors.TryRemove(id, out _))
            {
                this.spatialAnchorStore?.Remove(id);
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, SpatialAnchor> GetAllSpatialAnchors()
        {
            return this.spatialAnchors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <inheritdoc/>
        public Dictionary<string, CoordinateSystem> GetAllSpatialAnchorCoordinateSystems()
        {
            // Spatial anchors may not always be locatable at all points in time, so the result may contain null values
            return new Dictionary<string, CoordinateSystem>(
                this.spatialAnchors
                    .Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.CoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem())));
        }

        /// <inheritdoc/>
        public SpatialAnchor TryGetSpatialAnchor(string id)
        {
            return this.spatialAnchors.TryGetValue(id, out var spatialAnchor) ? spatialAnchor : null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
