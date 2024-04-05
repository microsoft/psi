// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a gather step.
    /// </summary>
    public class GatherStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GatherStep"/> class.
        /// </summary>
        public GatherStep()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GatherStep"/> class.
        /// </summary>
        /// <param name="label">The step label.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="noun">The noun.</param>
        /// <param name="objects">The set of objects to gather.</param>
        public GatherStep(string label, string verb, string noun, List<string> objects)
        {
            this.Label = label;
            this.Verb = verb;
            this.Noun = noun;
            this.Objects = objects;
        }

        /// <summary>
        /// Gets or sets the step label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the verb.
        /// </summary>
        public string Verb { get; set; }

        /// <summary>
        /// Gets or sets the noun.
        /// </summary>
        public string Noun { get; set; }

        /// <summary>
        /// Gets or sets the set of objects to gather.
        /// </summary>
        public List<string> Objects { get; set; }

        /// <inheritdoc/>
        public override string GetSpokenInstructions() => $"{this.Verb} the {this.Noun.ToLower()} listed below.";

        /// <inheritdoc/>
        public override string GetDisplayInstructions() => $"{this.Noun}:";

        /// <inheritdoc/>
        public override StepPanel UpdateStepPanel(
            StepPanel stepPanel,
            TaskPanelUserInterfaceCommand taskPanelUserInterfaceCommand,
            TaskPanelUserInterfaceConfiguration taskPanelUserInterfaceConfiguration,
            float maxHeigth,
            string name)
        {
            var gatherStepPanel = (stepPanel as GatherStepPanel) ?? new GatherStepPanel(
                taskPanelUserInterfaceConfiguration.Width,
                taskPanelUserInterfaceConfiguration.Padding,
                taskPanelUserInterfaceConfiguration.AccentColor,
                taskPanelUserInterfaceConfiguration.StepInstructionsTextStyle,
                taskPanelUserInterfaceConfiguration.GatherStepObjectColor,
                taskPanelUserInterfaceConfiguration.GatherStepObjectTextStyle,
                taskPanelUserInterfaceConfiguration.GatherStepHighlightObjectColor,
                taskPanelUserInterfaceConfiguration.GatherStepHighlightObjectTextStyle,
                name);
            gatherStepPanel.Update(this.Label, this.GetDisplayInstructions(), taskPanelUserInterfaceCommand.ObjectsChecklist);
            return gatherStepPanel;
        }

        /// <inheritdoc/>
        public override void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.Label, writer);
            InteropSerialization.WriteString(this.Verb, writer);
            InteropSerialization.WriteString(this.Noun, writer);
            InteropSerialization.WriteCollection(this.Objects, writer, o => InteropSerialization.WriteString(o, writer));
        }

        /// <inheritdoc/>
        public override void ReadFrom(BinaryReader reader)
        {
            this.Label = InteropSerialization.ReadString(reader);
            this.Verb = InteropSerialization.ReadString(reader);
            this.Noun = InteropSerialization.ReadString(reader);
            this.Objects = InteropSerialization.ReadCollection(reader, InteropSerialization.ReadString)?.ToList();
        }
    }
}
