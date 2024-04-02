// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;

    /// <summary>
    /// Base abstract class for dialog states.
    /// </summary>
    /// <typeparam name="TInteractionModel">The type of the interaction model.</typeparam>
    public abstract class DialogState<TInteractionModel>
    {
        /// <summary>
        /// Gets the system prompt and set of possible user responses for this dialog state.
        /// </summary>
        /// <param name="interactionModel">The interaction model.</param>
        /// <returns>A tuple containing the system prompt and set of possible user resposes.</returns>
        public abstract (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(TInteractionModel interactionModel);

        /// <summary>
        /// Gets an enumeration of the next dialog actions to be performed.
        /// </summary>
        /// <param name="inputEvent">The current input event.</param>
        /// <param name="interactionModel">The interaction model.</param>
        /// <returns>The enumeration of next dialog states to be performed.</returns>
        public abstract IEnumerable<DialogAction> GetNextDialogActions(
            IInputEvent inputEvent, TInteractionModel interactionModel);

        /// <summary>
        /// Updates the interaction state and interface commands upon entering the dialog state.
        /// </summary>
        /// <param name="interactionModel">The interaction model.</param>
        public virtual void OnEnter(TInteractionModel interactionModel)
        {
        }

        /// <summary>
        /// Updates the interaction state and interface commands upon leaving the dialog state.
        /// </summary>
        /// <param name="interactionModel">The interaction model.</param>
        public virtual void OnLeave(TInteractionModel interactionModel)
        {
        }

        /// <summary>
        /// Updates the interaction state and interface commands upon receiving the user state.
        /// </summary>
        /// <param name="interactionModel">The interaction model.</param>
        /// <returns>The input event if one is detected based on the observed interaction and user state.</returns>
        public virtual IInputEvent OnReceiveUserState(TInteractionModel interactionModel) => null;

        /// <summary>
        /// Updates the interaction state and interface commands upon receiving the interface state.
        /// </summary>
        /// <param name="interactionModel">The interaction model.</param>
        /// <returns>The input event if one is detected based on the observed interaction and interface state.</returns>
        public virtual IInputEvent OnReceiveInterfaceState(TInteractionModel interactionModel) => null;
    }
}
