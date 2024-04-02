// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents an abstract step in a task.
    /// </summary>
    public class Step : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Step"/> class.
        /// </summary>
        public Step()
        {
        }

        /// <summary>
        /// Gets the spoken instructions for the step.
        /// </summary>
        /// <returns>The spoken instructions for the step.</returns>
        public virtual string GetSpokenInstructions() => string.Empty;

        /// <summary>
        /// Gets the display instructions.
        /// </summary>
        /// <returns>The display instructions for the step.</returns>
        public virtual string GetDisplayInstructions() => string.Empty;

        /// <summary>
        /// Updates the step panel user interface.
        /// </summary>
        /// <param name="stepPanel">The step panel to update.</param>
        /// <param name="taskPanelUserInterfaceCommand">The task panel user interface command.</param>
        /// <param name="taskPanelUserInterfaceConfiguration">The task panel configuration options.</param>
        /// <param name="maxHeight">The maximum height for the step panel user interface.</param>
        /// <param name="name">The name for the user interface.</param>
        /// <returns>The user interface for the step.</returns>
        public virtual StepPanel UpdateStepPanel(
            StepPanel stepPanel,
            TaskPanelUserInterfaceCommand taskPanelUserInterfaceCommand,
            TaskPanelUserInterfaceConfiguration taskPanelUserInterfaceConfiguration,
            float maxHeight,
            string name)
            => stepPanel;

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
        }
    }
}
