// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a substep in a complex step.
    /// </summary>
    public class SubStep : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubStep"/> class.
        /// </summary>
        public SubStep()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubStep"/> class.
        /// </summary>
        /// <param name="label">The step label.</param>
        /// <param name="description">The step description.</param>
        /// <param name="displayVirtualObjects">The virtual objects to display.</param>
        public SubStep(
            string label,
            string description,
            List<VirtualObjectDescriptor> displayVirtualObjects)
        {
            this.Label = label;
            this.Description = description;
            this.VirtualObjects = displayVirtualObjects;
        }

        /// <summary>
        /// Gets or sets the step label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the step description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the virtual objects to display.
        /// </summary>
        public List<VirtualObjectDescriptor> VirtualObjects { get; set; }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.Label, writer);
            InteropSerialization.WriteString(this.Description, writer);
            InteropSerialization.WriteCollection(this.VirtualObjects, writer);
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.Label = InteropSerialization.ReadString(reader);
            this.Description = InteropSerialization.ReadString(reader);
            this.VirtualObjects = InteropSerialization.ReadCollection<VirtualObjectDescriptor>(reader)?.ToList();
        }
    }
}
