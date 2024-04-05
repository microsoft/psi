// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a step in which the user is asked to do something.
    /// </summary>
    public class DoStep : Step
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoStep"/> class.
        /// </summary>
        public DoStep()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoStep"/> class.
        /// </summary>
        /// <param name="label">The step label.</param>
        /// <param name="description">The step description.</param>
        public DoStep(string label, string description)
            : this(label, description, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DoStep"/> class.
        /// </summary>
        /// <param name="label">The step label.</param>
        /// <param name="description">The step description.</param>
        /// <param name="timerDuration">The timer duration.</param>
        public DoStep(string label, string description, TimeSpan timerDuration)
        {
            this.Label = label;
            this.Description = description;
            this.TimerDuration = timerDuration;
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
        /// Gets or sets the timer duration.
        /// </summary>
        public TimeSpan TimerDuration { get; set; }

        /// <inheritdoc/>
        public override string GetSpokenInstructions() => this.Description.TrimEnd('.');

        /// <inheritdoc/>
        public override string GetDisplayInstructions() => this.Description.TrimEnd('.');

        /// <inheritdoc/>
        public override StepPanel UpdateStepPanel(
            StepPanel stepPanel,
            TaskPanelUserInterfaceCommand taskPanelUserInterfaceComman,
            TaskPanelUserInterfaceConfiguration taskPanelUserInterfaceConfiguration,
            float maxHeight,
            string name)
        {
            var doStepPanel = stepPanel as DoStepPanel ?? new DoStepPanel(
                taskPanelUserInterfaceConfiguration.Width,
                taskPanelUserInterfaceConfiguration.Padding,
                taskPanelUserInterfaceConfiguration.AccentColor,
                taskPanelUserInterfaceConfiguration.StepInstructionsTextStyle,
                name);
            doStepPanel.Update(this.Label, this.GetDisplayInstructions());
            return doStepPanel;
        }

        /// <inheritdoc/>
        public override void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.Label, writer);
            InteropSerialization.WriteString(this.Description, writer);
            InteropSerialization.WriteTimeSpan(this.TimerDuration, writer);
        }

        /// <inheritdoc/>
        public override void ReadFrom(BinaryReader reader)
        {
            this.Label = InteropSerialization.ReadString(reader);
            this.Description = InteropSerialization.ReadString(reader);
            this.TimerDuration = InteropSerialization.ReadTimeSpan(reader);
        }
    }
}
