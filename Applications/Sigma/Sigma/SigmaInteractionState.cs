// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// The phase of the dialog state.
    /// </summary>
    public enum DialogStatePhase
    {
        /// <summary>
        /// The system prompt is in progress.
        /// </summary>
        SystemPrompt,

        /// <summary>
        /// The system is waiting for a response.
        /// </summary>
        WaitingForResponse,

        /// <summary>
        /// The system is transitioning to the next dialog state.
        /// </summary>
        Transitioning,
    }

    /// <summary>
    /// The trigger for the synthesis command.
    /// </summary>
    public enum SynthesisCommandTrigger
    {
        /// <summary>
        /// None
        /// </summary>
        None,

        /// <summary>
        /// The synthesis command was triggered by a dialog state move.
        /// </summary>
        DialogStateMove,

        /// <summary>
        /// The synthesis command was triggered by a speak text command.
        /// </summary>
        SpeakText,

        /// <summary>
        /// The systhesis command was triggered by the system informing the user to wait.
        /// </summary>
        PleaseWaitForLLM,
    }

    /// <summary>
    /// Represents the interaction state for the Sigma agent.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    public class SigmaInteractionState<TTask>
        where TTask : Task
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaInteractionState{TTask}"/> class.
        /// </summary>
        public SigmaInteractionState()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether to move the user interface to a specific pose.
        /// </summary>
        /// <remarks>
        /// When the value of this property is not null, it indicates an intention to move the user interface
        /// to the pose specified by the value. However, the actual move will be executed by the user interface
        /// and the current pose of the user interface is available in <see cref="SigmaUserInterfaceState.UserInterfacePose"/>.
        /// </remarks>
        public CoordinateSystem MoveUserInterfaceToPose { get; set; }

        /// <summary>
        /// Gets or sets the gem state.
        /// </summary>
        public GemState GemState { get; set; } = GemState.AtUserInterface();

        /// <summary>
        /// Gets or sets the set of timers to display.
        /// </summary>
        public Dictionary<Guid, (object Tag, Point3D Location, DateTime ExpiryTime)> Timers { get; set; } = new ();

        /// <summary>
        /// Gets or sets the set of text billboards to display.
        /// </summary>
        public Dictionary<Guid, (object Tag, Point3D Location, string Text)> TextBillboards { get; set; } = new ();

        /// <summary>
        /// Gets or sets the current task panel mode.
        /// </summary>
        public TaskPanelMode TaskPanelMode { get; set; } = TaskPanelMode.None;

        /// <summary>
        /// Gets or sets the selected step index.
        /// </summary>
        public int? SelectedStepIndex { get; set; }

        /// <summary>
        /// Gets the selected step.
        /// </summary>
        /// <returns>The selected step.</returns>
        public Step SelectedStep => this.Task != null && this.SelectedStepIndex.HasValue ? this.Task.Steps[this.SelectedStepIndex.Value] : default;

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
        /// Gets or sets a value indicating whether to show substep models.
        /// </summary>
        public bool ShowSubStepModels { get; set; } = false;

        /// <summary>
        /// Gets or sets the selected substep index.
        /// </summary>
        public int? SelectedSubStepIndex { get; set; }

        /// <summary>
        /// Gets or sets the LLM result.
        /// </summary>
        public string LLMResult { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the phrase "I see you already have" has already been spoken.
        /// </summary>
        public bool ISeeYouAlreadyHaveSpoken { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the interaction is paused.
        /// </summary>
        public bool IsPaused { get; set; } = false;

        /// <summary>
        /// Gets or sets the set of objects that have been detected since the last objects detected events was fired.
        /// </summary>
        public HashSet<string> ObjectsDetectedSinceLastObjectsDetectedInputEvent { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the list of current object locations.
        /// </summary>
        public List<(string Class, string InstanceId, Point3D Location)> CurrentObjects { get; set; } = new List<(string Class, string InstanceId, Point3D Location)>();

        /// <summary>
        /// Gets or sets the dialog state phase.
        /// </summary>
        public DialogStatePhase DialogStatePhase { get; set; }

        /// <summary>
        /// Gets or sets the synthesis command trigger.
        /// </summary>
        public SynthesisCommandTrigger SynthesisCommandTrigger { get; set; }

        /// <summary>
        /// Gets or sets the date-time for the last LLM query.
        /// </summary>
        public DateTime LastLLMQueryDateTime { get; set; }

        /// <summary>
        /// Gets or sets the top level intent.
        /// </summary>
        public string TopLevelIntent { get; set; }

        /// <summary>
        /// Gets or sets the task name.
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// Gets or sets the selected task.
        /// </summary>
        public TTask Task { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Sigma should move to a glanceable position.
        /// </summary>
        public bool MovedToGlanceable { get; set; }

        /// <summary>
        /// Gets or sets the set of context questions.
        /// </summary>
        public List<string> ContextQuestions { get; set; }

        /// <summary>
        /// Gets or sets the set of context answers.
        /// </summary>
        public List<string> ContextAnswers { get; set; }

        /// <summary>
        /// Gets or sets the list of found objects.
        /// </summary>
        public HashSet<string> FoundObjects { get; set; } = new HashSet<string>();

        /// <summary>
        /// Gets or sets the list of object detection classes.
        /// </summary>
        public List<string> ObjectClasses { get; set; } = new List<string>();

        /// <summary>
        /// Try to get the selected step as a step of the specified type.
        /// </summary>
        /// <typeparam name="TStep">The type of the step.</typeparam>
        /// <param name="step">The selected step.</param>
        /// <returns>True if the selected step is of the specified type.</returns>
        public bool TryGetSelectedStepOfType<TStep>(out TStep step)
            where TStep : Step
        {
            step = this.Task != null && this.SelectedStepIndex.HasValue ? this.Task.Steps[this.SelectedStepIndex.Value] as TStep : default;
            return step != null;
        }
    }
}
