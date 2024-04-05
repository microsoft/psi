// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents the gem user interface command.
    /// </summary>
    public class GemUserInterfaceCommand : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GemUserInterfaceCommand"/> class.
        /// </summary>
        public GemUserInterfaceCommand()
        {
        }

        /// <summary>
        /// Gets or sets the target gem pose.
        /// </summary>
        public CoordinateSystem GemPose { get; set; } = default;

        /// <summary>
        /// Gets or sets the target gem size.
        /// </summary>
        public double GemSize { get; set; } = GemUserInterfaceConfiguration.DefaultGemSize;

        /// <summary>
        /// Gets or sets a value indicating whether the gem is rotating.
        /// </summary>
        public bool GemIsRotating { get; set; } = false;

        /// <inheritdoc/>
        public void ReadFrom(BinaryReader reader)
        {
            this.GemPose = Serialization.ReadCoordinateSystem(reader);
            this.GemIsRotating = InteropSerialization.ReadBool(reader);
            this.GemSize = reader.ReadDouble();
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            Serialization.WriteCoordinateSystem(this.GemPose, writer);
            InteropSerialization.WriteBool(this.GemIsRotating, writer);
            writer.Write(this.GemSize);
        }
    }
}
