// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a complex step with substeps.
    /// </summary>
    public class ComplexStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexStep"/> class.
        /// </summary>
        public ComplexStep()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexStep"/> class.
        /// </summary>
        /// <param name="label">The step label.</param>
        /// <param name="description">The step description.</param>
        public ComplexStep(string label, string description)
        {
            this.Label = label;
            this.Description = description;
        }

        /// <summary>
        /// Gets or sets the step label.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the step description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the substeps.
        /// </summary>
        public List<SubStep> SubSteps { get; set; }

        /// <inheritdoc/>
        public override string GetDisplayInstructions() => this.Description.TrimEnd('.');

        /// <inheritdoc/>
        public override string GetSpokenInstructions() => this.Description.TrimEnd('.');

        /// <inheritdoc/>
        public override StepPanel UpdateStepPanel(
            StepPanel stepPanel,
            TaskPanelUserInterfaceCommand taskPanelUserInterfaceCommand,
            TaskPanelUserInterfaceConfiguration taskPanelUserInterfaceConfiguration,
            float maxHeight,
            string name)
        {
            // If it's a complex step panel, update
            var showObjectsChecklist = new List<(string Name, bool Checked, bool Highlight)>();
            var subSteps = new List<(string Label, string Description)>();
            var selectedStep = taskPanelUserInterfaceCommand.Task != null && taskPanelUserInterfaceCommand.SelectedStepIndex.HasValue ? taskPanelUserInterfaceCommand.Task.Steps[taskPanelUserInterfaceCommand.SelectedStepIndex.Value] : default;

            if (taskPanelUserInterfaceCommand.ShowComplexStepObjects && selectedStep == this)
            {
                showObjectsChecklist = taskPanelUserInterfaceCommand.ObjectsChecklist;
            }
            else if (taskPanelUserInterfaceCommand.ShowSubSteps && selectedStep == this)
            {
                subSteps = this.SubSteps.Select(ss => (ss.Label, ss.Description)).ToList();
            }

            var complexStepPanel = stepPanel as ComplexStepPanel ?? new ComplexStepPanel(
                taskPanelUserInterfaceConfiguration.Width,
                taskPanelUserInterfaceConfiguration.Padding,
                taskPanelUserInterfaceConfiguration.AccentColor,
                taskPanelUserInterfaceConfiguration.StepInstructionsTextStyle,
                taskPanelUserInterfaceConfiguration.ComplexStepObjectTextStyle,
                taskPanelUserInterfaceConfiguration.ComplexStepTaughtObjectTextStyle,
                taskPanelUserInterfaceConfiguration.SelectionColor,
                name);

            complexStepPanel.Update(
                this.Label,
                this.GetDisplayInstructions(),
                taskPanelUserInterfaceCommand.SelectedSubStepIndex,
                showObjectsChecklist,
                subSteps,
                maxHeight);
            return complexStepPanel;
        }

        /// <inheritdoc/>
        public override void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.Label, writer);
            InteropSerialization.WriteString(this.Description, writer);
            InteropSerialization.WriteCollection(this.SubSteps, writer);
        }

        /// <inheritdoc/>
        public override void ReadFrom(BinaryReader reader)
        {
            this.Label = InteropSerialization.ReadString(reader);
            this.Description = InteropSerialization.ReadString(reader);
            this.SubSteps = InteropSerialization.ReadCollection<SubStep>(reader)?.ToList();
        }

        /// <summary>
        /// Gets the list of substeps of a specified type.
        /// </summary>
        /// <typeparam name="T">The substep type.</typeparam>
        /// <returns>The enumeration of substeps of a specified type.</returns>
        public IEnumerable<T> GetSubStepsOfType<T>()
            where T : SubStep
            => this.SubSteps.Where(s => s is T).Select(s => s as T);
    }
}