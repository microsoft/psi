// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a descriptor for a virtual object.
    /// </summary>
    public class VirtualObjectDescriptor : IInteropSerializable
    {
        /// <summary>
        /// Gets or sets the name of the virtual object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the model type for the virtual object.
        /// </summary>
        public string ModelType { get; set; }

        /// <summary>
        /// Gets or sets the spatial pose for the virtual object.
        /// </summary>
        public SpatialPoseDescriptor SpatialPose { get; set; }

        /// <inheritdoc/>
        public void ReadFrom(BinaryReader reader)
        {
            this.Name = InteropSerialization.ReadString(reader);
            this.ModelType = InteropSerialization.ReadString(reader);
            this.SpatialPose = InteropSerialization.Read<SpatialPoseDescriptor>(reader);
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.Name, writer);
            InteropSerialization.WriteString(this.ModelType, writer);
            InteropSerialization.Write(this.SpatialPose, writer);
        }
    }
}