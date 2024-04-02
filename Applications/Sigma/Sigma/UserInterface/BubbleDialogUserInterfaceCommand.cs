// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a command for the bubble dialog user interface.
    /// </summary>
    public class BubbleDialogUserInterfaceCommand : IInteropSerializable
    {
        /// <summary>
        /// Gets or sets the system prompt.
        /// </summary>
        public string SystemPrompt { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the set of user responses.
        /// </summary>
        public string[] UserResponseSet { get; set; } = new string[] { };

        /// <summary>
        /// Gets or sets the user response in progress.
        /// </summary>
        public string UserResponseInProgress { get; set; } = null;

        /// <summary>
        /// Gets or sets the utterance history.
        /// </summary>
        public List<(string Utterance, bool SystemUtterance)> UtteranceHistory { get; set; } = new ();

        /// <summary>
        /// Gets or sets a value indicating whether to show the thinking status.
        /// </summary>
        public bool ShowIsThinkingStatus { get; set; }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteBool(this.ShowIsThinkingStatus, writer);
            InteropSerialization.WriteString(this.SystemPrompt, writer);
            InteropSerialization.WriteCollection(this.UserResponseSet, writer, s => InteropSerialization.WriteString(s, writer));
            InteropSerialization.WriteString(this.UserResponseInProgress, writer);
            InteropSerialization.WriteCollection(
                this.UtteranceHistory,
                writer,
                t =>
                {
                    InteropSerialization.WriteString(t.Utterance, writer);
                    InteropSerialization.WriteBool(t.SystemUtterance, writer);
                });
        }

        /// <inheritdoc/>
        public void ReadFrom(BinaryReader reader)
        {
            this.ShowIsThinkingStatus = InteropSerialization.ReadBool(reader);
            this.SystemPrompt = InteropSerialization.ReadString(reader);
            this.UserResponseSet = InteropSerialization.ReadCollection(reader, () => InteropSerialization.ReadString(reader))?.ToArray();
            this.UserResponseInProgress = InteropSerialization.ReadString(reader);
            this.UtteranceHistory = InteropSerialization.ReadCollection(reader, () => (InteropSerialization.ReadString(reader), InteropSerialization.ReadBool(reader)))?.ToList();
        }
    }
}
