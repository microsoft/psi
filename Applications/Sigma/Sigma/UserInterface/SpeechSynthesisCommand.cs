// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a speech synthesis command.
    /// </summary>
    public class SpeechSynthesisCommand : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechSynthesisCommand"/> class.
        /// </summary>
        public SpeechSynthesisCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechSynthesisCommand"/> class.
        /// </summary>
        /// <param name="synthesisText">The text to synthesize.</param>
        /// <param name="synthesisStop">A value indicating whether to stop synthesis.</param>
        public SpeechSynthesisCommand(string synthesisText, bool synthesisStop = false)
        {
            this.Guid = Guid.NewGuid();
            this.Text = synthesisText;
            this.Stop = synthesisStop;
        }

        /// <summary>
        /// Gets or sets the synthesis command guid.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the synthesis text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to stop synthesis.
        /// </summary>
        public bool Stop { get; set; }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteGuid(this.Guid, writer);
            InteropSerialization.WriteString(this.Text, writer);
            InteropSerialization.WriteBool(this.Stop, writer);
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.Guid = InteropSerialization.ReadGuid(reader);
            this.Text = InteropSerialization.ReadString(reader);
            this.Stop = InteropSerialization.ReadBool(reader);
        }
    }
}
