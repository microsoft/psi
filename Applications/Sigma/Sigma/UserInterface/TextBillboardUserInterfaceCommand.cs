// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a text billboard user interface command.
    /// </summary>
    public class TextBillboardUserInterfaceCommand : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextBillboardUserInterfaceCommand"/> class.
        /// </summary>
        public TextBillboardUserInterfaceCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextBillboardUserInterfaceCommand"/> class.
        /// </summary>
        /// <param name="location">The 3D point at which to display the text billboard.</param>
        /// <param name="text">The text to display.</param>
        public TextBillboardUserInterfaceCommand(Point3D location, string text)
        {
            this.Location = location;
            this.Text = text;
        }

        /// <summary>
        /// Gets or sets the location at which to display the text billboard.
        /// </summary>
        public Point3D Location { get; set; }

        /// <summary>
        /// Gets or sets the text to display.
        /// </summary>
        public string Text { get; set; }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            Serialization.WritePoint3D(this.Location, writer);
            InteropSerialization.WriteString(this.Text, writer);
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.Location = Serialization.ReadPoint3D(reader);
            this.Text = InteropSerialization.ReadString(reader);
        }
    }
}
