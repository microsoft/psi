// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Component that implements the interaction state manager for the Sigma application.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
    /// <typeparam name="TInteractionModel">The state.</typeparam>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TInteractionState">The type of the interaction state.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    public class SigmaInteractionStateManager<TTask, TConfiguration, TInteractionModel, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands> :
        InteractionStateManager<TConfiguration, TInteractionModel, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>
        where TTask : Task, IInteropSerializable, new()
        where TInteractionModel : SigmaInteractionModel<TTask, TConfiguration, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>, new()
        where TPersistentState : SigmaPersistentState<TTask>, new()
        where TInteractionState : SigmaInteractionState<TTask>, new()
        where TUserInterfaceState : SigmaUserInterfaceState, new()
        where TUserInterfaceCommands : SigmaUserInterfaceCommands, new()
        where TConfiguration : SigmaComputeServerPipelineConfiguration, new()
    {
        private readonly Stack<IEnumerator<DialogAction>> plannedDialogActionStack = new ();

        private IInputEvent lastInputEvent = null;
        private bool rejectedLastPartialRecognitionResult = false;
        private TUserInterfaceCommands userInterfaceCommands = new ();

        // Similarly, this is protected to allow derived classes to set this. However the pattern is the same for all
        // three (A, B and C) where it gets set in the constructor (seeds the enumeration with an initial action).
        // Perhaps this could be made cleaner by passing just the initial action into the base constructor as a required
        // parameter, setting it in the base, then making this private
        private IEnumerator<DialogAction> plannedDialogActions = default;

        private string debugInfo = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaInteractionStateManager{TTask, TConfiguration, TInteractionModel, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the interaction state manager.</param>
        /// <param name="persistentState">The persistent state.</param>
        /// <param name="initialDialogState">The initial dialog state.</param>
        /// <param name="name">An optional name for the component.</param>
        public SigmaInteractionStateManager(
            Pipeline pipeline,
            TConfiguration configuration,
            TPersistentState persistentState,
            DialogState<TInteractionModel> initialDialogState,
            string name = nameof(SigmaInteractionStateManager<TTask, TConfiguration, TInteractionModel, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>))
            : base(pipeline, name, configuration.MaxInteractionStateOutputFrequency)
        {
            // Setup the start state to either the specified initial state or to the delayed start state, per configuration.
            this.plannedDialogActions = new List<DialogAction>()
            {
                configuration.DelayedStart != TimeSpan.Zero ?
                    DialogAction.ContinueWith(new DelayedStart<TInteractionModel>(configuration.DelayedStart, initialDialogState)) :
                    DialogAction.ContinueWith(initialDialogState),
            }.GetEnumerator();

            this.UserStateInput = pipeline.CreateReceiver<UserState>(this, this.ReceiveUserState, nameof(this.UserStateInput));
            this.TrackedObjectsLocationsInput = pipeline.CreateReceiver<List<(string, string, Point3D)>>(this, this.ReceiveTrackedObjects, nameof(this.TrackedObjectsLocationsInput));
            this.SpeechRecognitionResultsInput = pipeline.CreateReceiver<IStreamingSpeechRecognitionResult>(this, this.ReceiveSpeechRecognitionResults, nameof(this.SpeechRecognitionResultsInput));
            this.PartialSpeechRecognitionResultsInput = pipeline.CreateReceiver<IStreamingSpeechRecognitionResult>(this, this.ReceivePartialSpeechRecognitionResults, nameof(this.PartialSpeechRecognitionResultsInput));
            this.SpeechSynthesisProgressInput = pipeline.CreateReceiver<SpeechSynthesisProgress>(this, this.ReceiveSpeechSynthesisProgress, nameof(this.SpeechSynthesisProgressInput));
            this.LLMQuery = pipeline.CreateEmitter<(string, string, string[])>(this, nameof(this.LLMQuery));
            this.LLMQueryResultsInput = pipeline.CreateReceiver<string>(this, this.ReceiveLLMQueryResults, nameof(this.LLMQueryResultsInput));
            this.DebugInfo = pipeline.CreateEmitter<string>(this, nameof(this.DebugInfo));

            // Setup the configuration in the internal state
            this.Configuration = configuration;

            // Setup the persistent state
            this.PersistentState = persistentState;

            // Set up the configuration on the dialog states
            pipeline.PipelineRun += (s, e) =>
            {
                this.RetrieveNextPlannedDialogAction();
                this.ExecuteDialogAction();
            };
        }

        /// <summary>
        /// Gets or sets the user state.
        /// </summary>
        public UserState UserState
        {
            get => this.InteractionModel.UserState;
            set => this.InteractionModel.UserState = value;
        }

        /// <summary>
        /// Gets the receiver for tracked object locations.
        /// </summary>
        public Receiver<List<(string, string, Point3D)>> TrackedObjectsLocationsInput { get; }

        /// <summary>
        /// Gets the receiver for the user state.
        /// </summary>
        public Receiver<UserState> UserStateInput { get; }

        /// <summary>
        /// Gets the receiver for speech recognition results.
        /// </summary>
        public Receiver<IStreamingSpeechRecognitionResult> SpeechRecognitionResultsInput { get; }

        /// <summary>
        /// Gets the receiver for partial speech recognition results.
        /// </summary>
        public Receiver<IStreamingSpeechRecognitionResult> PartialSpeechRecognitionResultsInput { get; }

        /// <summary>
        /// Gets the receiver for speech synthesis progress reports.
        /// </summary>
        public Receiver<SpeechSynthesisProgress> SpeechSynthesisProgressInput { get; }

        /// <summary>
        /// Gets the receiver for LLM query results.
        /// </summary>
        public Receiver<string> LLMQueryResultsInput { get; }

        /// <summary>
        /// Gets the emitter for LLM query commands.
        /// </summary>
        public Emitter<(string Query, string Prompt, string[] Parameters)> LLMQuery { get; }

        /// <summary>
        /// Gets the emitter for debug information.
        /// </summary>
        public Emitter<string> DebugInfo { get; }

        /// <summary>
        /// Gets or sets the current dialog state.
        /// </summary>
        protected DialogState<TInteractionModel> CurrentDialogState { get; set; }

        /// <inheritdoc/>
        public override void Write(string prefix, Exporter exporter)
        {
            base.Write(prefix, exporter);
            this.DebugInfo?.Write($"{prefix}.{nameof(this.DebugInfo)}", exporter);
        }

        /// <inheritdoc/>
        protected override void OnReceiveInterfaceState()
        {
            var inputEvent = this.CurrentDialogState?.OnReceiveInterfaceState(this.InteractionModel);
            if (inputEvent != null)
            {
                this.AddInputEvent(inputEvent);
                this.PlanDialogActionsForLastInputEvent();
                this.ExecuteDialogAction();
            }

            if (!string.IsNullOrEmpty(this.debugInfo))
            {
                this.DebugInfo.Post(this.debugInfo, this.DebugInfo.Pipeline.GetCurrentTime());
                this.debugInfo = string.Empty;
            }
        }

        /// <summary>
        /// Adds an input event to the event queue.
        /// </summary>
        /// <param name="inputEvent">The input event.</param>
        protected void AddInputEvent(IInputEvent inputEvent)
            => this.lastInputEvent = inputEvent;

        /// <summary>
        /// Plans the dialog actions for the last input event.
        /// </summary>
        protected void PlanDialogActionsForLastInputEvent()
        {
            this.InteractionState.DialogStatePhase = DialogStatePhase.Transitioning;
            if (!this.plannedDialogActions.MoveNext())
            {
                this.plannedDialogActions = this.CurrentDialogState.GetNextDialogActions(this.lastInputEvent, this.InteractionModel).GetEnumerator();
                this.lastInputEvent = null;
                this.plannedDialogActions.MoveNext();
            }
        }

        /// <summary>
        /// Retrieves the next planned dialog action.
        /// </summary>
        protected void RetrieveNextPlannedDialogAction()
        {
            if (!this.plannedDialogActions.MoveNext())
            {
                if (this.plannedDialogActionStack.Count > 0)
                {
                    this.plannedDialogActions = this.plannedDialogActionStack.Pop();
                }
                else
                {
                    this.plannedDialogActions = this.CurrentDialogState.GetNextDialogActions(null, this.InteractionModel).GetEnumerator();
                }

                this.RetrieveNextPlannedDialogAction();
            }
        }

        /// <summary>
        /// Executes the current dialog action.
        /// </summary>
        protected void ExecuteDialogAction()
        {
            void MoveCurrentPromptToHistory()
            {
                // Add the previous system prompt and user response to the dialog history
                if (!string.IsNullOrEmpty(this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt))
                {
                    this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UtteranceHistory.Insert(0, (this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt, true));
                }

                if (!string.IsNullOrEmpty(this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseInProgress))
                {
                    this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UtteranceHistory.Insert(0, (this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseInProgress, false));
                }

                while (this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UtteranceHistory.Count > 4)
                {
                    this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UtteranceHistory.RemoveAt(this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UtteranceHistory.Count - 1);
                }

                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt = null;
            }

            if (this.plannedDialogActions.Current == null)
            {
                return;
            }
            else if (this.plannedDialogActions.Current.IsExecute(out var dialogActions))
            {
                // The push the current action plan on the stack
                this.plannedDialogActionStack.Push(this.plannedDialogActions);

                // Set the planned dialog actions to point to the set of actions
                this.plannedDialogActions = dialogActions.GetEnumerator();
                this.RetrieveNextPlannedDialogAction();
                this.ExecuteDialogAction();
            }
            else if (this.plannedDialogActions.Current.IsSpeak(out var speakText))
            {
                MoveCurrentPromptToHistory();
                this.InteractionState.DialogStatePhase = DialogStatePhase.Transitioning;
                this.InteractionState.SynthesisCommandTrigger = SynthesisCommandTrigger.SpeakText;
                this.UserInterfaceCommands.SpeechSynthesisCommand = new SpeechSynthesisCommand(speakText);
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt = speakText;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseSet = null;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseInProgress = null;
            }
            else if (this.plannedDialogActions.Current.IsContinueWith<TInteractionModel>(out var nextDialogState, out var noSpeechSynthesis))
            {
                MoveCurrentPromptToHistory();
                this.CurrentDialogState?.OnLeave(this.InteractionModel);
                this.CurrentDialogState = nextDialogState;
                this.CurrentDialogState.OnEnter(this.InteractionModel);
                (this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt, this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseSet) =
                    this.CurrentDialogState.GetSystemPromptAndUserResponseSet(this.InteractionModel);
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseInProgress = null;

                if (!noSpeechSynthesis && !string.IsNullOrEmpty(this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt))
                {
                    this.InteractionState.DialogStatePhase = DialogStatePhase.SystemPrompt;
                    this.InteractionState.SynthesisCommandTrigger = SynthesisCommandTrigger.DialogStateMove;
                    this.UserInterfaceCommands.SpeechSynthesisCommand = new SpeechSynthesisCommand(this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt);
                }
                else
                {
                    this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt = null;
                    this.InteractionState.DialogStatePhase = DialogStatePhase.WaitingForResponse;
                    this.InteractionState.SynthesisCommandTrigger = SynthesisCommandTrigger.None;
                    this.UserInterfaceCommands.SpeechSynthesisCommand = null;
                }
            }
            else if (this.plannedDialogActions.Current.IsRunLLMQuery(out var query, out var parameters))
            {
                MoveCurrentPromptToHistory();
                this.InteractionState.DialogStatePhase = DialogStatePhase.Transitioning;
                this.InteractionState.LLMResult = null;
                this.InteractionState.LastLLMQueryDateTime = DateTime.Now;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.ShowIsThinkingStatus = true;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt = null;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseSet = null;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseInProgress = null;
                this.LLMQuery.Post((query, "Default", parameters), this.LLMQuery.Pipeline.GetCurrentTime());
            }
            else if (this.plannedDialogActions.Current.IsExitCommand())
            {
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UtteranceHistory.Clear();
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.SystemPrompt = null;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseSet = null;
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseInProgress = null;
                this.UserInterfaceCommands.ExitCommand = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnPipelineCompleted(object sender, PipelineCompletedEventArgs eventArgs)
        {
            this.InteractionModel.OnClose();
            base.OnPipelineCompleted(sender, eventArgs);
        }

        /// <inheritdoc/>
        protected override void PersistFinalState(DateTime originatingTime)
        {
            this.PersistentState.WriteToFile(this.Configuration.PersistentStateFilename, originatingTime);
        }

        private void ReceiveTrackedObjects(List<(string Class, string InstanceId, Point3D Location)> objects)
        {
            if (this.InteractionState.IsPaused)
            {
                return;
            }

            if (objects != null)
            {
                this.InteractionState.CurrentObjects = objects.DeepClone();
            }

            // If we're in the right dialog state (executing a gather step)
            if (this.InteractionState.TryGetSelectedStepOfType<GatherStep>(out var gatherStep) &&
                this.InteractionState.DialogStatePhase == DialogStatePhase.WaitingForResponse)
            {
                // Determine if we have any new objects detected since last time an object detected input event was generated
                var alreadyDetectedObjects = this.UserInterfaceCommands.TaskPanelUserInterfaceCommand.ObjectsChecklist.Where(o => o.Check).Select(o => o.ObjectName);
                var currentObjectsToDetect = this.InteractionState.CurrentObjects
                    .Where(o => this.UserInterfaceCommands.TaskPanelUserInterfaceCommand.ObjectsChecklist.Any(t => t.ObjectName == o.Class)).Select(o => o.Class).ToList();

                foreach (var (@class, _, _) in this.InteractionState.CurrentObjects)
                {
                    // For now, switched to only add one object at a time. This is because we can't yet synchronize the gem
                    // pointing action movement with multiple found objects, so we're doing one at a time.
                    // if (!alreadyDetectedObjects.Contains(name) && !this.InteractionState.ObjectsDetectedSinceLastObjectsDetectedInputEvent.Contains(name))
                    if (!alreadyDetectedObjects.Contains(@class) && !this.InteractionState.ObjectsDetectedSinceLastObjectsDetectedInputEvent.Any())
                    {
                        this.InteractionState.ObjectsDetectedSinceLastObjectsDetectedInputEvent.Add(@class);
                    }
                }

                // If we have new objects detected
                if (this.InteractionState.ObjectsDetectedSinceLastObjectsDetectedInputEvent.Count > 0)
                {
                    // Then create a new input event with those objects and continue dialog planning based on that
                    this.AddInputEvent(new ObjectsDetectedInputEvent(this.InteractionState.ObjectsDetectedSinceLastObjectsDetectedInputEvent.DeepClone()));
                    this.InteractionState.ObjectsDetectedSinceLastObjectsDetectedInputEvent.Clear();
                    this.PlanDialogActionsForLastInputEvent();
                    this.ExecuteDialogAction();
                }
            }
        }

        private void ReceiveLLMQueryResults(string results)
        {
            // Continue the execution of dialog actions from where it left off
            this.InteractionState.LLMResult = results?.Trim()?.ToLower();
            this.InteractionState.LastLLMQueryDateTime = DateTime.MinValue;

            // If the please wait for LLM synthesis is no longer in progress, plan the next action
            if (this.InteractionState.SynthesisCommandTrigger != SynthesisCommandTrigger.PleaseWaitForLLM)
            {
                this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.ShowIsThinkingStatus = false;
                this.RetrieveNextPlannedDialogAction();
                this.ExecuteDialogAction();
            }
        }

        private void ReceiveUserState(UserState userState, Envelope envelope)
        {
            if (userState != null)
            {
                this.UserState = userState.DeepClone();
            }

            // Use the user state stream as a clock to check how long since we ran the LLM query
            // (if we did and trigger a synthesis command to say please wait if enough time
            // has elapsed)
            if (this.InteractionState.LastLLMQueryDateTime != DateTime.MinValue &&
                (DateTime.Now - this.InteractionState.LastLLMQueryDateTime) > TimeSpan.FromSeconds(4))
            {
                this.InteractionState.LastLLMQueryDateTime = DateTime.MinValue;
                this.InteractionState.SynthesisCommandTrigger = SynthesisCommandTrigger.PleaseWaitForLLM;
                this.UserInterfaceCommands.SpeechSynthesisCommand = new SpeechSynthesisCommand(Language.ChooseRandom("Give me a second.", "Give me a moment.", "One moment please."));
            }

            var inputEvent = this.CurrentDialogState?.OnReceiveUserState(this.InteractionModel);
            if (inputEvent != null)
            {
                this.AddInputEvent(inputEvent);
                this.PlanDialogActionsForLastInputEvent();
                this.ExecuteDialogAction();
            }
        }

        private void LogDebugInfo(string message)
        {
            var currentTime = this.DebugInfo.Pipeline.GetCurrentTime().ToString("HH:mm:ss.ffff");
            this.debugInfo += $"@[{currentTime}]: {message}" + Environment.NewLine;
        }

        private void ReceiveSpeechRecognitionResults(IStreamingSpeechRecognitionResult speechRecognition)
        {
            var isResumeCommand = speechRecognition != null && speechRecognition.Text.Contains("resume");

            // Ignore the event if in frozen state
            if (this.InteractionState.IsPaused && !isResumeCommand)
            {
                return;
            }

            // If the reco result is empty, ignore it
            if (string.IsNullOrEmpty(speechRecognition?.Text))
            {
                this.LogDebugInfo("ReceiveSpeechRecognitionResults: returning b/c reco result empty.");
                this.rejectedLastPartialRecognitionResult = false;
                return;
            }

            // If we are transitioning, don't allow the barge-in
            if (this.InteractionState.DialogStatePhase == DialogStatePhase.Transitioning)
            {
                this.LogDebugInfo("ReceivePartialSpeechRecognitionResults: returning b/c not allowing barge-in while system is transitioning.");
                this.rejectedLastPartialRecognitionResult = false;
                return;
            }

            // If the last partial result was rejected, continue rejecting.
            if (this.rejectedLastPartialRecognitionResult)
            {
                this.LogDebugInfo("ReceivePartialSpeechRecognitionResults: returning b/c we rejected the last partial speech recognition result.");
                this.rejectedLastPartialRecognitionResult = false;
                return;
            }

            // Stop the synthesis if we allow barge-ins and synthesis is in progress
            if (this.InteractionState.DialogStatePhase == DialogStatePhase.SystemPrompt && this.UserInterfaceCommands.SpeechSynthesisCommand != null)
            {
                this.LogDebugInfo("ReceiveSpeechRecognitionResults: setting SynthesisStop=true.");
                this.UserInterfaceCommands.SpeechSynthesisCommand.Stop = true;
            }

            if (this.UserState.IsPalmUp(out var palmPoint) &&
                speechRecognition != null &&
                speechRecognition.Text.Trim().ToLower() == "come here" &&
                this.UserState.Head.Origin != null)
            {
                this.LogDebugInfo("ReceiveSpeechRecognitionResults: setting TargetUserInterfacePosition.");
                this.InteractionState.MoveUserInterfaceToPose = Operators.GetTargetOrientedCoordinateSystem(palmPoint, this.UserState.Head.Origin);
                this.rejectedLastPartialRecognitionResult = false;
            }
            else
            {
                this.LogDebugInfo("ReceiveSpeechRecognitionResults: planning and executing dialog actions.");
                this.rejectedLastPartialRecognitionResult = false;
                this.AddInputEvent(new SpeechRecognitionInputEvent(speechRecognition.Text));

                // If the system is waiting for a response, plan the dialog actions for the speech recognition input
                // O/w store that input for later planning, once the system is again waiting for a response
                if (this.InteractionState.DialogStatePhase == DialogStatePhase.WaitingForResponse)
                {
                    this.PlanDialogActionsForLastInputEvent();
                    this.ExecuteDialogAction();
                }
            }
        }

        private void ReceivePartialSpeechRecognitionResults(IStreamingSpeechRecognitionResult speechRecognition)
        {
            // Ignore the event if in frozen state
            if (this.InteractionState.IsPaused)
            {
                return;
            }

            // If the reco result is empty, ignore it
            if (string.IsNullOrEmpty(speechRecognition?.Text))
            {
                this.LogDebugInfo("ReceivePartialSpeechRecognitionResults: returning b/c reco result empty.");
                return;
            }

            // If we're transitioning, don't allow barge-ins
            if (this.InteractionState.DialogStatePhase == DialogStatePhase.Transitioning)
            {
                this.LogDebugInfo("ReceivePartialSpeechRecognitionResults: returning b/c not allowing barge-in while system is transitioning.");
                this.rejectedLastPartialRecognitionResult = true;
                return;
            }

            // If the last partial recognition result was rejected, continue rejecting
            if (this.rejectedLastPartialRecognitionResult)
            {
                this.LogDebugInfo("ReceivePartialSpeechRecognitionResults: returning b/c we rejected the last partial speech recognition result.");
                return;
            }

            // Update the user response in progress
            this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseInProgress = speechRecognition.Text.Capitalize();

            // Stop the synthesis if we allow barge-ins and synthesis is in progress
            if (this.InteractionState.DialogStatePhase == DialogStatePhase.SystemPrompt && this.UserInterfaceCommands.SpeechSynthesisCommand != null)
            {
                this.LogDebugInfo("ReceivePartialSpeechRecognitionResults: setting SynthesisStop=true.");
                this.UserInterfaceCommands.SpeechSynthesisCommand.Stop = true;
            }
        }

        private void ReceiveSpeechSynthesisProgress(SpeechSynthesisProgress speechSynthesisProgress)
        {
            if (speechSynthesisProgress.EventType == SpeechSynthesisProgressEventType.SynthesisCompleted ||
                speechSynthesisProgress.EventType == SpeechSynthesisProgressEventType.SynthesisCancelled)
            {
                this.LogDebugInfo($"ReceiveSpeechSynthesisProgress: received {speechSynthesisProgress.EventType}.");
                var lastSynthesisCommandTrigger = this.InteractionState.SynthesisCommandTrigger;
                this.InteractionState.SynthesisCommandTrigger = SynthesisCommandTrigger.None;
                this.UserInterfaceCommands.SpeechSynthesisCommand = null;

                this.LogDebugInfo($"ReceiveSpeechSynthesisProgress: action that triggered synthesis was {lastSynthesisCommandTrigger}.");

                // If the action that triggered the synthesis was a dialog state move
                if (lastSynthesisCommandTrigger == SynthesisCommandTrigger.DialogStateMove)
                {
                    // If we already have an input event queued up
                    if (this.lastInputEvent != null)
                    {
                        // Then continue planning the dialog based on that last input event
                        this.PlanDialogActionsForLastInputEvent();
                        this.ExecuteDialogAction();
                    }

                    // O/w if there is no expected response from this dialog state
                    else if (this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.UserResponseSet == null)
                    {
                        this.LogDebugInfo("ReceiveSpeechSynthesisProgress: no expected response, continuing execution.");

                        // Then we go directly to the transitioning phase, and retrieve the next dialog actions and continue execution
                        this.InteractionState.DialogStatePhase = DialogStatePhase.Transitioning;
                        this.RetrieveNextPlannedDialogAction();
                        this.ExecuteDialogAction();
                    }
                    else
                    {
                        // O/w we are entering the waiting for a response phase
                        this.InteractionState.DialogStatePhase = DialogStatePhase.WaitingForResponse;
                    }
                }

                // O/w if the action that triggered the synthesis was a speak action
                else if (lastSynthesisCommandTrigger == SynthesisCommandTrigger.SpeakText)
                {
                    // Then continue the planning and execution of follow-up actions
                    this.InteractionState.DialogStatePhase = DialogStatePhase.Transitioning;
                    this.RetrieveNextPlannedDialogAction();
                    this.ExecuteDialogAction();
                }

                // O/w if the action that trigger the synthesis was a please-wait-for-LLM and we now have the LLM
                // results
                else if (lastSynthesisCommandTrigger == SynthesisCommandTrigger.PleaseWaitForLLM &&
                    !string.IsNullOrEmpty(this.InteractionState.LLMResult))
                {
                    this.InteractionState.DialogStatePhase = DialogStatePhase.Transitioning;
                    this.UserInterfaceCommands.BubbleDialogUserInterfaceCommand.ShowIsThinkingStatus = false;
                    this.RetrieveNextPlannedDialogAction();
                    this.ExecuteDialogAction();
                }
            }
            else if (speechSynthesisProgress.EventType == SpeechSynthesisProgressEventType.SynthesisStarted ||
                speechSynthesisProgress.EventType == SpeechSynthesisProgressEventType.SynthesisInProgress)
            {
                this.LogDebugInfo("ReceiveSpeechSynthesisProgress: received SynthesisStarted or SynthesisInProgress.");
            }
        }
    }
}