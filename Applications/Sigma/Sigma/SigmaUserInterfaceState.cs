// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents the user interface state for the Sigma app.
    /// </summary>
    public class SigmaUserInterfaceState : IInteropSerializable
    {
        /// <summary>
        /// Gets or sets the user interface pose.
        /// </summary>
        public CoordinateSystem UserInterfacePose { get; set; }

        /// <summary>
        /// Gets or sets the gem pose.
        /// </summary>
        public CoordinateSystem GemPose { get; set; }

        /// <summary>
        /// Gets or sets the list of planar user interface elements.
        /// </summary>
        public List<Rectangle3DUserInterfaceState> Rectangle3DUserInterfaces { get; set; } = new ();

        /// <summary>
        /// Gets or sets the list of models.
        /// </summary>
        public Dictionary<string, ModelUserInterfaceState> ModelUserInterfaces { get; set; }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            Serialization.WriteCoordinateSystem(this.UserInterfacePose, writer);
            Serialization.WriteCoordinateSystem(this.GemPose, writer);
            InteropSerialization.WriteCollection(this.Rectangle3DUserInterfaces, writer);
            InteropSerialization.WriteDictionary(this.ModelUserInterfaces, writer, name => InteropSerialization.WriteString(name, writer));
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.UserInterfacePose = Serialization.ReadCoordinateSystem(reader);
            this.GemPose = Serialization.ReadCoordinateSystem(reader);
            this.Rectangle3DUserInterfaces = InteropSerialization.ReadCollection<Rectangle3DUserInterfaceState>(reader)?.ToList();
            this.ModelUserInterfaces = InteropSerialization.ReadDictionary<string, ModelUserInterfaceState>(reader, () => InteropSerialization.ReadString(reader));
        }
    }
}
