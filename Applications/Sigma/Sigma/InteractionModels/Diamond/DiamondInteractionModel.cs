// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the interaction model for the Diamond version of the Sigma app.
    /// </summary>
    public class DiamondInteractionModel : SigmaInteractionModel<
        DiamondTask,
        DiamondConfiguration,
        DiamondPersistentState,
        DiamondInteractionState,
        DiamondUserInterfaceState,
        DiamondUserInterfaceCommands>
    {
        /// <summary>
        /// Abandons the task.
        /// </summary>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> AbandonTask()
            => this.AbandonTask(string.IsNullOrEmpty(this.Configuration.AutoStartTaskName) ? new DiamondDialogStates.WhatAreWeDoing() : null);

        /// <summary>
        /// Continues with the selected step.
        /// </summary>
        /// <returns>The set of corresponding dialog actions.</returns>
        /// <exception cref="Exception">An exception is thrown if a step is unexpected.</exception>
        public virtual IEnumerable<DialogAction> ContinueWithSelectedStep()
        {
            if (this.InteractionState.TryGetSelectedStepOfType<DoStep>(out var _) || this.InteractionState.TryGetSelectedStepOfType<GatherStep>(out var _))
            {
                yield return DialogAction.ContinueWith<DiamondDialogStates.ExecuteStep>();
            }
            else if (this.InteractionState.TryGetSelectedStepOfType<ComplexStep>(out var _))
            {
                yield return DialogAction.ContinueWith<DiamondDialogStates.ExecuteComplexStep>();
            }
            else
            {
                throw new Exception("Unexpected step type");
            }
        }
    }
}
