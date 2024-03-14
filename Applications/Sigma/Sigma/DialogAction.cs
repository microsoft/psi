// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a dialog action performed by the Sigma agent.
    /// </summary>
    /// <remarks>
    /// A dialog action might be executing a piece of code, performing a dialog state
    /// transition or triggering speech synthesis.
    /// </remarks>
    public class DialogAction
    {
        private readonly object nextDialogState;
        private readonly bool dialogStateTransitionNoSpeechSynthesis;
        private readonly string speakText;
        private readonly string llmQuery;
        private readonly string[] llmQueryParameters;
        private readonly IEnumerable<DialogAction> dialogActions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogAction"/> class.
        /// </summary>
        public DialogAction()
        {
        }

        private DialogAction(
            object nextDialogState,
            bool dialogStateTransitionNoSpeechSynthesis,
            string speakText,
            string llmQuery,
            string[] llmQueryParameters,
            IEnumerable<DialogAction> dialogActions)
        {
            this.nextDialogState = nextDialogState;
            this.dialogStateTransitionNoSpeechSynthesis = dialogStateTransitionNoSpeechSynthesis;
            this.speakText = speakText;
            this.llmQuery = llmQuery;
            this.llmQueryParameters = llmQueryParameters;
            this.dialogActions = dialogActions;
        }

        /// <summary>
        /// Constructs a dialog action that continues the interaction with a new dialog state.
        /// </summary>
        /// <typeparam name="TInteractionModel">The type of the interaction model.</typeparam>
        /// <param name="dialogState">The dialog state to continue the interaction with.</param>
        /// <param name="noSpeechSynthesis">Indicates whether to perform speech synthesis when transitioning to the specified dialog state.</param>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction ContinueWith<TInteractionModel>(DialogState<TInteractionModel> dialogState, bool noSpeechSynthesis = false)
            => new (dialogState, noSpeechSynthesis, null, null, null, null);

        /// <summary>
        /// Constructs a dialog action that continues the interaction with a new dialog state.
        /// </summary>
        /// <typeparam name="TDialogState">The type of the dialog state.</typeparam>
        /// <param name="noSpeechSynthesis">Indicates whether to perform speech synthesis when transitioning to the specified dialog state.</param>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction ContinueWith<TDialogState>(bool noSpeechSynthesis = false)
            where TDialogState : new()
            => new (new TDialogState(), noSpeechSynthesis, null, null, null, null);

        /// <summary>
        /// Constructs a dialog action that forces the application to exit.
        /// </summary>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction ExitCommand()
            => new (null, false, null, null, null, null);

        /// <summary>
        /// Constructs a dialog action that continues the interaction by speaking a specified text.
        /// </summary>
        /// <param name="text">The text to speak.</param>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction Speak(string text)
            => new (null, false, text, null, null, null);

        /// <summary>
        /// Constructs a dialog action that continues the interaction by running an LLM query.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction RunLLMQuery(string query, params string[] parameters)
            => new (null, false, null, query, parameters, null);

        /// <summary>
        /// Constructs a dialog action that continues the interaction by executing a specified dialog action.
        /// </summary>
        /// <param name="dialogAction">The dialog action to run.</param>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction Execute(DialogAction dialogAction)
            => dialogAction;

        /// <summary>
        /// Constructs a dialog action that continues the interaction by sequentially executing a set of dialog actions.
        /// </summary>
        /// <param name="dialogActions">The set of dialog actions to run.</param>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction Execute(
            IEnumerable<DialogAction> dialogActions)
            => new (null, false, null, null, null, dialogActions);

        /// <summary>
        /// Constructs a dialog action that continues the interaction by sequentially executing a set of dialog actions.
        /// </summary>
        /// <param name="dialogActionsConstructor">The function that generates a set of dialog actions to run.</param>
        /// <returns>The corresponding dialog action.</returns>
        public static DialogAction Execute(
            Func<IEnumerable<DialogAction>> dialogActionsConstructor)
            => new (null, false, null, null, null, dialogActionsConstructor.Invoke());

        /// <summary>
        /// Gets a value indicating whether the dialog action continues the interaction with a new dialog state.
        /// </summary>
        /// <typeparam name="TInteractionModel">The type of the interaction model.</typeparam>
        /// <param name="dialogState">The dialog state to continue the interaction with.</param>
        /// <param name="noSpeechSynthesis">Indicates whether to perform speech synthesis when transitioning to the specified dialog state.</param>
        /// <returns>True if the dialog action continues the interaction with a new dialog state.</returns>
        public bool IsContinueWith<TInteractionModel>(out DialogState<TInteractionModel> dialogState, out bool noSpeechSynthesis)
        {
            dialogState = this.nextDialogState as DialogState<TInteractionModel>;
            noSpeechSynthesis = this.dialogStateTransitionNoSpeechSynthesis;
            return dialogState != null;
        }

        /// <summary>
        /// Gets a value indicating whether the app should exit.
        /// </summary>
        /// <returns>True if the app should exit.</returns>
        public bool IsExitCommand() =>
            this.nextDialogState == null &&
            this.dialogStateTransitionNoSpeechSynthesis == false &&
            this.speakText == null &&
            this.llmQuery == null &&
            this.llmQueryParameters == null &&
            this.dialogActions == null;

        /// <summary>
        /// Gets a value indicating whether the dialog action continues the interaction by speaking a specified text.
        /// </summary>
        /// <param name="text">The text to speak.</param>
        /// <returns>True if the dialog action continues the interaction by speaking a specified text.</returns>
        public bool IsSpeak(out string text)
        {
            text = this.speakText;
            return text != null;
        }

        /// <summary>
        /// Constructs a dialog action that continues the interaction by running an LLM query.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <returns>True if the dialog action continues the interaction by running an LLM query.</returns>
        public bool IsRunLLMQuery(out string query, out string[] parameters)
        {
            query = this.llmQuery;
            parameters = this.llmQueryParameters;
            return query != null;
        }

        /// <summary>
        /// Gets a value indicating whether the dialog action continues the interaction by sequentially executing a set of dialog actions.
        /// </summary>
        /// <param name="dialogActions">The set of dialog actions to run.</param>
        /// <returns>True if the dialog action continues the interaction by sequentially executing a set of dialog actions.</returns>
        public bool IsExecute(out IEnumerable<DialogAction> dialogActions)
        {
            dialogActions = this.dialogActions;
            return dialogActions != null;
        }
    }
}
