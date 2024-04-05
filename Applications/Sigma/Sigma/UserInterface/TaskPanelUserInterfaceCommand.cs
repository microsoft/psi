// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents rendering commands for the task panel user interface.
    /// </summary>
    public class TaskPanelUserInterfaceCommand : IInteropSerializable
    {
        /// <summary>
        /// Gets or sets the task panel mode.
        /// </summary>
        public TaskPanelMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the current task.
        /// </summary>
        public Task Task { get; set; }

        /// <summary>
        /// Gets or sets the selected step index.
        /// </summary>
        public int? SelectedStepIndex { get; set; }

        /// <summary>
        /// Gets or sets the selected substep index.
        /// </summary>
        public int? SelectedSubStepIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to only show the selected step.
        /// </summary>
        public bool ShowOnlySelectedStep { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to show complex step objects.
        /// </summary>
        public bool ShowComplexStepObjects { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to show substeps.
        /// </summary>
        public bool ShowSubSteps { get; set; } = false;

        /// <summary>
        /// Gets or sets the object checklist.
        /// </summary>
        public List<(string ObjectName, bool Check, bool Highlight)> ObjectsChecklist { get; set; } = new List<(string, bool, bool)>();

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteInt32((int)this.Mode, writer);
            InteropSerialization.Write(this.Task, writer);
            InteropSerialization.WriteNullable(this.SelectedStepIndex, writer, InteropSerialization.WriteInt32);
            InteropSerialization.WriteNullable(this.SelectedSubStepIndex, writer, InteropSerialization.WriteInt32);
            InteropSerialization.WriteBool(this.ShowOnlySelectedStep, writer);
            InteropSerialization.WriteBool(this.ShowComplexStepObjects, writer);
            InteropSerialization.WriteBool(this.ShowSubSteps, writer);
            InteropSerialization.WriteCollection(
                this.ObjectsChecklist,
                writer,
                s =>
                {
                    InteropSerialization.WriteString(s.ObjectName, writer);
                    InteropSerialization.WriteBool(s.Check, writer);
                    InteropSerialization.WriteBool(s.Highlight, writer);
                });
        }

        /// <inheritdoc/>
        public void ReadFrom(BinaryReader reader)
        {
            this.Mode = (TaskPanelMode)InteropSerialization.ReadInt32(reader);
            this.Task = InteropSerialization.Read<Task>(reader);
            this.SelectedStepIndex = InteropSerialization.ReadNullable(reader, InteropSerialization.ReadInt32);
            this.SelectedSubStepIndex = InteropSerialization.ReadNullable(reader, InteropSerialization.ReadInt32);
            this.ShowOnlySelectedStep = InteropSerialization.ReadBool(reader);
            this.ShowComplexStepObjects = InteropSerialization.ReadBool(reader);
            this.ShowSubSteps = InteropSerialization.ReadBool(reader);
            this.ObjectsChecklist = InteropSerialization.ReadCollection(reader, () => (InteropSerialization.ReadString(reader), InteropSerialization.ReadBool(reader), InteropSerialization.ReadBool(reader)))?.ToList();
        }
    }
}
