// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using HoloLensCaptureInterop;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Represents the state of a planar (rectangle 3D) user interface.
    /// </summary>
    public class Rectangle3DUserInterfaceState : IInteropSerializable
    {
        /// <summary>
        /// Gets or sets the user interface name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the 3d rectangle at which the user interface is located.
        /// </summary>
        public Rectangle3D Rectangle3D { get; set; }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.Name, writer);
            Serialization.WriteRectangle3D(this.Rectangle3D, writer);
        }

        /// <inheritdoc/>
        public void ReadFrom(BinaryReader reader)
        {
            this.Name = InteropSerialization.ReadString(reader);
            this.Rectangle3D = Serialization.ReadRectangle3D(reader);
        }
    }
}
