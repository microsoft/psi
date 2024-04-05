// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a spatial pose descriptor for a known spatial location.
    /// </summary>
    public class AtKnownSpatialLocation : SpatialPoseDescriptor, IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AtKnownSpatialLocation"/> class.
        /// </summary>
        public AtKnownSpatialLocation()
        {
        }

        /// <summary>
        /// Gets or sets the name of the spatial location.
        /// </summary>
        public string SpatialLocationName { get; set; }

        /// <inheritdoc/>
        public override bool TryGetWorldCoordinateSystem(Dictionary<string, CoordinateSystem> knownSpatialLocations, out CoordinateSystem worldCoordinateSystem)
            => knownSpatialLocations.TryGetValue(this.SpatialLocationName, out worldCoordinateSystem);

        /// <inheritdoc/>
        public override void Write(BinaryWriter writer)
        {
            base.Write(writer);
            InteropSerialization.WriteString(this.SpatialLocationName, writer);
        }

        /// <inheritdoc/>
        public override void ReadFrom(BinaryReader reader)
        {
            base.ReadFrom(reader);
            this.SpatialLocationName = InteropSerialization.ReadString(reader);
        }
    }
}
