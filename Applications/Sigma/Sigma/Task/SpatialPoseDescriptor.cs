// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a descriptor for a spatial pose.
    /// </summary>
    public class SpatialPoseDescriptor : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialPoseDescriptor"/> class.
        /// </summary>
        public SpatialPoseDescriptor()
        {
        }

        /// <summary>
        /// Tries to get the world coordinate system for this pose descriptor.
        /// </summary>
        /// <param name="knownSpatialLocations">The set of known spatial locations.</param>
        /// <param name="worldCoordinateSystem">The world coordinate system.</param>
        /// <returns>True if the world coordinate system was found, false otherwise.</returns>
        public virtual bool TryGetWorldCoordinateSystem(Dictionary<string, CoordinateSystem> knownSpatialLocations, out CoordinateSystem worldCoordinateSystem)
        {
            worldCoordinateSystem = default;
            return false;
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
        }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
        }
    }
}
