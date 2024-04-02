// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a model user interface command.
    /// </summary>
    public class ModelUserInterfaceCommand : IInteropSerializable
    {
        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the model type.
        /// </summary>
        public string ModelType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model is visible.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model can be moved by the user.
        /// </summary>
        public bool CanBeMovedByUser { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model can be scaled by the user.
        /// </summary>
        public bool CanBeScaledByUser { get; set; }

        /// <summary>
        /// Gets or sets the pose of the model.
        /// </summary>
        public CoordinateSystem Pose { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model is rendered as a wireframe.
        /// </summary>
        public bool Wireframe { get; set; }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.ModelName, writer);
            InteropSerialization.WriteString(this.ModelType, writer);
            InteropSerialization.WriteBool(this.Visible, writer);
            InteropSerialization.WriteBool(this.CanBeMovedByUser, writer);
            InteropSerialization.WriteBool(this.CanBeScaledByUser, writer);
            Serialization.WriteCoordinateSystem(this.Pose, writer);
            InteropSerialization.WriteBool(this.Wireframe, writer);
        }

        /// <inheritdoc/>
        public void ReadFrom(BinaryReader reader)
        {
            this.ModelName = InteropSerialization.ReadString(reader);
            this.ModelType = InteropSerialization.ReadString(reader);
            this.Visible = InteropSerialization.ReadBool(reader);
            this.CanBeMovedByUser = InteropSerialization.ReadBool(reader);
            this.CanBeScaledByUser = InteropSerialization.ReadBool(reader);
            this.Pose = Serialization.ReadCoordinateSystem(reader);
            this.Wireframe = InteropSerialization.ReadBool(reader);
        }
    }
}
