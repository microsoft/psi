// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents the user interface commands for the Sigma app.
    /// </summary>
    public class SigmaUserInterfaceCommands : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaUserInterfaceCommands"/> class.
        /// </summary>
        public SigmaUserInterfaceCommands()
        {
        }

        /// <summary>
        /// Gets or sets the speech synthesis command.
        /// </summary>
        public SpeechSynthesisCommand SpeechSynthesisCommand { get; set; }

        /// <summary>
        /// Gets or sets the target user interface pose.
        /// </summary>
        public CoordinateSystem MoveUserInterfaceToPoseCommand { get; set; }

        /// <summary>
        /// Gets or sets the target gem user interface command.
        /// </summary>
        public GemUserInterfaceCommand GemUserInterfaceCommand { get; set; } = new ();

        /// <summary>
        /// Gets or sets the bubble dialog user interface command.
        /// </summary>
        public BubbleDialogUserInterfaceCommand BubbleDialogUserInterfaceCommand { get; set; } = new ();

        /// <summary>
        /// Gets or sets the task panel user interface command.
        /// </summary>
        public TaskPanelUserInterfaceCommand TaskPanelUserInterfaceCommand { get; set; } = null;

        /// <summary>
        /// Gets or sets the model user interface commands.
        /// </summary>
        public Dictionary<string, ModelUserInterfaceCommand> ModelsUserInterfaceCommand { get; set; } = new ();

        /// <summary>
        /// Gets or sets the timer user interface commands.
        /// </summary>
        public Dictionary<Guid, TimerUserInterfaceCommand> TimersUserInterfaceCommand { get; set; } = new ();

        /// <summary>
        /// Gets or sets the text specs.
        /// </summary>
        public List<TextBillboardUserInterfaceCommand> TextBillboardsUserInterfaceCommand { get; set; } = new ();

        /// <summary>
        /// Gets or sets a value indicating whether to show the recording frame.
        /// </summary>
        public bool ShowRecordingFrameCommand { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the app should exit.
        /// </summary>
        public bool ExitCommand { get; set; } = false;

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            InteropSerialization.Write(this.SpeechSynthesisCommand, writer);
            InteropSerialization.Write(this.BubbleDialogUserInterfaceCommand, writer);
            InteropSerialization.Write(this.TaskPanelUserInterfaceCommand, writer);
            InteropSerialization.WriteBool(this.ShowRecordingFrameCommand, writer);
            Serialization.WriteCoordinateSystem(this.MoveUserInterfaceToPoseCommand, writer);
            InteropSerialization.Write(this.GemUserInterfaceCommand, writer);
            InteropSerialization.WriteDictionary(
                this.ModelsUserInterfaceCommand,
                writer,
                name => InteropSerialization.WriteString(name, writer));
            InteropSerialization.WriteDictionary(
                this.TimersUserInterfaceCommand,
                writer,
                id => InteropSerialization.WriteGuid(id, writer));
            InteropSerialization.WriteCollection(this.TextBillboardsUserInterfaceCommand, writer);
            InteropSerialization.WriteBool(this.ExitCommand, writer);
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.SpeechSynthesisCommand = InteropSerialization.Read<SpeechSynthesisCommand>(reader);
            this.BubbleDialogUserInterfaceCommand = InteropSerialization.Read<BubbleDialogUserInterfaceCommand>(reader);
            this.TaskPanelUserInterfaceCommand = InteropSerialization.Read<TaskPanelUserInterfaceCommand>(reader);
            this.ShowRecordingFrameCommand = InteropSerialization.ReadBool(reader);
            this.MoveUserInterfaceToPoseCommand = Serialization.ReadCoordinateSystem(reader);
            this.GemUserInterfaceCommand = InteropSerialization.Read<GemUserInterfaceCommand>(reader);
            this.ModelsUserInterfaceCommand = InteropSerialization.ReadDictionary<string, ModelUserInterfaceCommand>(reader, () => InteropSerialization.ReadString(reader));
            this.TimersUserInterfaceCommand = InteropSerialization.ReadDictionary<Guid, TimerUserInterfaceCommand>(reader, () => InteropSerialization.ReadGuid(reader));
            this.TextBillboardsUserInterfaceCommand = InteropSerialization.ReadCollection<TextBillboardUserInterfaceCommand>(reader)?.ToList();
            this.ExitCommand = InteropSerialization.ReadBool(reader);
        }
    }
}
