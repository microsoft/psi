// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implements the dialog state for the delayed start.
    /// </summary>
    /// <typeparam name="TInteractionModel">The type of the interaction model.</typeparam>
    public class DelayedStart<TInteractionModel> : DialogState<TInteractionModel>
    {
        private readonly DateTime startTime;
        private readonly DialogState<TInteractionModel> startState;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedStart{TInteractionModel}"/> class.
        /// </summary>
        /// <param name="delay">The delay after which to start Sigma.</param>
        /// <param name="startState">The state to start Sigma in.</param>
        public DelayedStart(TimeSpan delay, DialogState<TInteractionModel> startState)
        {
            this.startTime = DateTime.Now + delay;
            this.startState = startState;
        }

        /// <inheritdoc/>
        public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(TInteractionModel interactionModel)
            => (null, null);

        /// <inheritdoc/>
        public override IInputEvent OnReceiveInterfaceState(TInteractionModel interactionModel)
            => (DateTime.Now - this.startTime > TimeSpan.Zero) ? new DelayedStartInputEvent() : null;

        /// <inheritdoc/>
        public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, TInteractionModel interactionModel)
        {
            if (inputEvent is DelayedStartInputEvent)
            {
                yield return DialogAction.ContinueWith(this.startState);
            }
            else
            {
                yield return DialogAction.ContinueWith(this, noSpeechSynthesis: true);
            }
        }

        private class DelayedStartInputEvent : IInputEvent
        {
        }
    }
}