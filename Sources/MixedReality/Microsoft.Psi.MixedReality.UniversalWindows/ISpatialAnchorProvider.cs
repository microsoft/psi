// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Windows.Perception.Spatial;

    /// <summary>
    /// Defines a provider for spatial anchor operations.
    /// </summary>
    public interface ISpatialAnchorProvider : IDisposable
    {
        /// <summary>
        /// Creates a spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/>.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <returns>The new spatial anchor and its identifier, or null if the creation failed.</returns>
        /// <remarks>
        /// Depending on the implementation, the new spatial anchor may be assigned a new id, so the
        /// returned id should always be used to reference the spatial anchor instead of the supplied id.
        /// </remarks>
        (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem);

        /// <summary>
        /// Creates a spatial anchor at the supplied <see cref="CoordinateSystem"/>.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="coordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <returns>The new spatial anchor and its identifier, or null if the creation failed.</returns>
        /// <remarks>
        /// Depending on the implementation, the new spatial anchor may be assigned a new id, so the
        /// returned id should always be used to reference the spatial anchor instead of the supplied id.
        /// </remarks>
        (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, CoordinateSystem coordinateSystem);

        /// <summary>
        /// Creates a spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/> with a positional offset.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <param name="translation">The rigid positional offset from the coordinate system's origin in HoloLens basis.</param>
        /// <returns>The new spatial anchor and its identifier, or null if the creation failed.</returns>
        /// <remarks>
        /// HoloLens basis means +x right, +y up, -z forward. Depending on the implementation,
        /// the new spatial anchor may be assigned a new id, so the returned id should always
        /// be used to reference the spatial anchor instead of the supplied id.
        /// </remarks>
        (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation);

        /// <summary>
        /// Creates a spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/> with offsets in position and rotation.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <param name="translation">The rigid positional offset from the coordinate system's origin in HoloLens basis.</param>
        /// <param name="rotation">The rigid rotation from the coordinate system's origin in HoloLens basis.</param>
        /// <returns>The new spatial anchor and its identifier, or null if the creation failed.</returns>
        /// <remarks>
        /// HoloLens basis means +x right, +y up, -z forward. Depending on the implementation,
        /// the new spatial anchor may be assigned a new id, so the returned id should always
        /// be used to reference the spatial anchor instead of the supplied id.
        /// </remarks>
        (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation, System.Numerics.Quaternion rotation);

        /// <summary>
        /// Creates a spatial anchor at the supplied <see cref="SpatialCoordinateSystem"/> with a transform offset in \psi basis.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="spatialCoordinateSystem">The coordinate system at which to create the spatial anchor.</param>
        /// <param name="relativeOffset">The amount of relative offset applied to the given coordinate system when creating the spatial anchor.</param>
        /// <returns>The new spatial anchor and its identifier, or null if the creation failed.</returns>
        /// <remarks>
        /// Depending on the implementation, the new spatial anchor may be assigned a new id, so the
        /// returned id should always be used to reference the spatial anchor instead of the supplied id.
        /// </remarks>
        (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, CoordinateSystem relativeOffset);

        /// <summary>
        /// Updates the coordinate system of a persisted spatial anchor.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <param name="coordinateSystem">The new coordinate system of the spatial anchor.</param>
        /// <returns>The updated spatial anchor and its identifier, or null if the update failed.</returns>
        /// <remarks>
        /// If the spatial anchor was not found in the store, a new one will be created. Updating
        /// an existing spatial anchor with a null coordinate system will cause it to be removed.
        /// In this case, the existing spatial anchor is returned. Depending on the implementation,
        /// the spatial anchor may be assigned a new id, so the returned id should always be used
        /// to reference the spatial anchor instead of the supplied id.
        /// </remarks>
        (SpatialAnchor anchor, string id) TryUpdateSpatialAnchor(string id, CoordinateSystem coordinateSystem);

        /// <summary>
        /// Removes the specified spatial anchor from the store.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor to remove.</param>
        void RemoveSpatialAnchor(string id);

        /// <summary>
        /// Gets all spatial anchors in the store.
        /// </summary>
        /// <returns>The map of spatial anchors.</returns>
        Dictionary<string, SpatialAnchor> GetAllSpatialAnchors();

        /// <summary>
        /// Gets the coordinate systems for all spatial anchors in the store.
        /// </summary>
        /// <returns>The map of spatial anchor coordinate systems.</returns>
        Dictionary<string, CoordinateSystem> GetAllSpatialAnchorCoordinateSystems();

        /// <summary>
        /// Gets the specified spatial anchor from the store.
        /// </summary>
        /// <param name="id">The identifier of the spatial anchor.</param>
        /// <returns>The spatial anchor, or null if it was not found.</returns>
        public SpatialAnchor TryGetSpatialAnchor(string id);
    }
}
