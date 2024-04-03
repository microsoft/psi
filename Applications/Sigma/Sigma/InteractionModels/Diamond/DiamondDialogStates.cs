// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;

    /// <summary>
    /// Implements the set of dialog states used for dialog management in Diamond version of the Sigma app.
    /// </summary>
    public static class DiamondDialogStates
    {
        /// <summary>
        /// Implements the dialog state for the introduction.
        /// </summary>
        public class Intro : DialogState<DiamondInteractionModel>
        {
            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => ("Hi! This is Sigma, your mixed-reality task assistant.", null);

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                if (string.IsNullOrEmpty(interactionModel.Configuration.AutoStartTaskName))
                {
                    yield return DialogAction.ContinueWith<WhatAreWeDoing>();
                }
                else
                {
                    interactionModel.InteractionState.TopLevelIntent = "guide";
                    interactionModel.InteractionState.TaskName = interactionModel.Configuration.AutoStartTaskName;
                    if (interactionModel.TryGetKnownTask())
                    {
                        yield return DialogAction.Speak($"Today I'm here to help you {interactionModel.Configuration.AutoStartTaskName.ToLower().TrimEnd('.')}.");

                        // Move to a glanceable position
                        yield return DialogAction.Execute(interactionModel.MoveToGlanceablePosition);

                        // Then wait for the user to be ready
                        yield return DialogAction.ContinueWith<WaitForReadyToStart>();
                    }
                    else
                    {
                        interactionModel.InteractionState.TopLevelIntent = null;
                        interactionModel.InteractionState.TaskName = null;
                        yield return DialogAction.Speak("I'm sorry, there was an auto-start task specified in my configuration, but I'm not familiar with it.");
                        yield return DialogAction.ContinueWith<WhatAreWeDoing>();
                    }
                }
            }
        }

        /// <summary>
        /// Implements the dialog state for ready-to-start.
        /// </summary>
        public class WaitForReadyToStart : DialogState<DiamondInteractionModel>
        {
            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => ("Let me know when you're ready to start.", new[] { "I'm ready." });

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                if (inputEvent is SpeechRecognitionInputEvent speechRecognitionInputEvent)
                {
                    if (speechRecognitionInputEvent.SpeechRecognitionResult.Contains("ready"))
                    {
                        var isKnownTask = interactionModel.TryGetKnownTask();
                        yield return DialogAction.Execute(interactionModel.BeginTask(isKnownTask));
                    }
                    else
                    {
                        yield return DialogAction.Speak(Language.Nonunderstanding);
                        yield return DialogAction.ContinueWith<WaitForReadyToStart>(noSpeechSynthesis: true);
                    }
                }
            }
        }

        /// <summary>
        /// Implements the dialog state for eliciting what the user is doing.
        /// </summary>
        public class WhatAreWeDoing : DialogState<DiamondInteractionModel>
        {
            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => ("What are we doing today?",
                    new[]
                    {
                        "Can you help me ...",
                        "What can you help me with?",
                    });

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                if (inputEvent is SpeechRecognitionInputEvent speechRecognitionInputEvent)
                {
                    // Handle pause interaction
                    if (speechRecognitionInputEvent.SpeechRecognitionResult.ContainsOneOf("pause", "freeze"))
                    {
                        yield return DialogAction.ContinueWith(new PauseInteraction(this));
                        yield break;
                    }

                    // Get the top level intent
                    yield return DialogAction.Execute(interactionModel.GetTopLevelIntent(speechRecognitionInputEvent.SpeechRecognitionResult));

                    if (interactionModel.InteractionState.TopLevelIntent == "guide")
                    {
                        // Figure out if this is a known task (if it's known the method sets up the task in the internal state)
                        var isKnownTask = interactionModel.TryGetKnownTask();

                        if (isKnownTask)
                        {
                            yield return DialogAction.Speak($"Sure. I can help you {interactionModel.InteractionState.TaskName.ToLower().TrimEnd('.')}.");
                        }
                        else
                        {
                            yield return DialogAction.Speak($"Let's see ...");
                        }

                        // Move to a glanceable position
                        yield return DialogAction.Execute(interactionModel.MoveToGlanceablePosition);

                        // And begin the task
                        yield return DialogAction.Execute(interactionModel.BeginTask(isKnownTask));
                    }
                    else if (interactionModel.InteractionState.TopLevelIntent == "list")
                    {
                        interactionModel.InteractionState.TaskPanelMode = TaskPanelMode.TaskList;
                        yield return DialogAction.Speak($"Here's the list of tasks I can help you with.");
                        yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                        yield break;
                    }
                    else
                    {
                        yield return DialogAction.Speak(Language.Nonunderstanding);
                        yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                    }
                }
                else
                {
                    yield return DialogAction.Speak(Language.Nonunderstanding);
                    yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                }
            }
        }

        /// <summary>
        /// Implements the dialog state for executing a step.
        /// </summary>
        public class ExecuteStep : DialogState<DiamondInteractionModel>
        {
            private readonly bool usePreamble = true;

            /// <summary>
            /// Initializes a new instance of the <see cref="ExecuteStep"/> class.
            /// </summary>
            public ExecuteStep()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ExecuteStep"/> class.
            /// </summary>
            /// <param name="usePreamble">Indicates whether to use a preamble for the step (e.g., first, next, finally...)</param>
            public ExecuteStep(bool usePreamble)
            {
                this.usePreamble = usePreamble;
            }

            /// <inheritdoc/>
            public override void OnEnter(DiamondInteractionModel interactionModel)
            {
                if (interactionModel.InteractionState.SelectedStep is GatherStep)
                {
                    interactionModel.RemoveAllTextBillboards();
                    foreach (var (@class, _, location) in interactionModel.InteractionState.CurrentObjects)
                    {
                        interactionModel.AddTextBillboard(location, @class);
                    }
                }
            }

            /// <inheritdoc/>
            public override void OnLeave(DiamondInteractionModel interactionModel) => interactionModel.RemoveAllTextBillboards();

            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
            {
                var isFirstStep =
                    (interactionModel.InteractionState.TryGetSelectedStepOfType<GatherStep>(out var gatherStep) && gatherStep.Label == "1") ||
                    (interactionModel.InteractionState.TryGetSelectedStepOfType<DoStep>(out var doStep) && doStep.Label == "1");

                var systemPrompt = isFirstStep ?
                    $"{interactionModel.InteractionState.SelectedStep.GetSpokenInstructions().Trim('.')}." :
                    $"{interactionModel.InteractionState.SelectedStep.GetSpokenInstructions().Trim('.')}.";

                if (this.usePreamble)
                {
                    systemPrompt = isFirstStep ?
                        $"The first step is to {systemPrompt.ToLower()}" :
                        $"Next, {systemPrompt.ToLower()}";
                }

                var userResponseSet = new List<string>()
                {
                    "Next step.",
                    "Go to the previous step.",
                    "Start the timer.",
                    "Let's abandon this task.",
                    "Take a note.",
                };

                if (interactionModel.InteractionState.SelectedStep is GatherStep gatherStep2)
                {
                    var youCanSay = gatherStep2.Objects.Count() > 1 ?
                        $"'I have the {gatherStep2.Objects.First().ToLower()}', or I've found the {gatherStep2.Objects.First().ToLower()} and the {gatherStep2.Objects.Skip(1).First().ToLower()}." :
                        $"'I have the {gatherStep2.Objects.First().ToLower()}'";
                    systemPrompt += $" Let me know once you've found each of the {gatherStep2.Noun.ToLower()}. For instance, you can say something like {youCanSay}.";
                    userResponseSet.Insert(0, "I have the ...");
                }

                return (systemPrompt, userResponseSet.ToArray());
            }

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                if (inputEvent is SpeechRecognitionInputEvent speechRecognitionInputEvent &&
                    speechRecognitionInputEvent.SpeechRecognitionResult != null)
                {
                    var speechRecognition = speechRecognitionInputEvent.SpeechRecognitionResult;

                    if (speechRecognition.Contains("next"))
                    {
                        if (interactionModel.InteractionState.SelectedStepIndex < interactionModel.InteractionState.Task.Steps.Count - 1)
                        {
                            interactionModel.InteractionState.GemState = GemState.AtUserInterface();
                            interactionModel.InteractionState.SelectedStepIndex++;
                            yield return DialogAction.Execute(interactionModel.ContinueWithSelectedStep);
                        }
                        else
                        {
                            yield return DialogAction.Speak("That was the last step of the task.");
                            yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                        }
                    }
                    else if (speechRecognition.Contains("previous") && speechRecognition.Contains("step"))
                    {
                        if (interactionModel.InteractionState.SelectedStepIndex > 0)
                        {
                            interactionModel.InteractionState.SelectedStepIndex--;
                            if (interactionModel.InteractionState.TryGetSelectedStepOfType<ComplexStep>(out var complexStep))
                            {
                                interactionModel.InteractionState.SelectedSubStepIndex = complexStep.SubSteps.Count - 1;
                                yield return DialogAction.ContinueWith(new ExecuteSubStep(usePreamble: false));
                            }
                            else
                            {
                                yield return DialogAction.ContinueWith(new ExecuteStep(usePreamble: false));
                            }
                        }
                        else
                        {
                            yield return DialogAction.Speak("We are already at the first step of the task.");
                            yield return DialogAction.ContinueWith<ExecuteSubStep>(noSpeechSynthesis: true);
                        }
                    }
                    else if (speechRecognition.Contains("start") && speechRecognition.Contains("timer") && interactionModel.InteractionState.SelectedStep is DoStep doStep)
                    {
                        interactionModel.StartTimer(doStep, interactionModel.UserState.Head.Origin + interactionModel.UserState.Head.XAxis.ScaleBy(0.7));
                        yield return DialogAction.Speak("Okay, I've started the timer.");
                        yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                    }
                    else if (speechRecognition.Contains("stop") && speechRecognition.Contains("timer"))
                    {
                        interactionModel.RemoveAllTimers();
                        yield return DialogAction.Speak("Okay, I've stopped the timer.");
                        yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                    }
                    else if (speechRecognition.Contains("freeze") || speechRecognition.Contains("pause"))
                    {
                        yield return DialogAction.ContinueWith(new PauseInteraction(this));
                    }
                    else if (speechRecognition.Contains("abandon"))
                    {
                        yield return DialogAction.Execute(interactionModel.AbandonTask());
                    }
                    else if (speechRecognition.Contains("note"))
                    {
                        yield return DialogAction.Speak("Sure. Go ahead.");
                        yield return DialogAction.ContinueWith(new TakeANote(this));
                    }
                    else if (speechRecognition.Contains("step"))
                    {
                        for (int i = 0; i < interactionModel.InteractionState.Task.Steps.Count; i++)
                        {
                            if (speechRecognition.Contains($"step {Language.GetNumber(i + 1)}"))
                            {
                                interactionModel.InteractionState.GemState = GemState.AtUserInterface();
                                interactionModel.InteractionState.SelectedStepIndex = i;
                                yield return DialogAction.Execute(interactionModel.ContinueWithSelectedStep);
                                yield break;
                            }
                        }

                        yield return DialogAction.Speak(Language.Nonunderstanding);
                        yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                    }
                    else
                    {
                        // If we are on a gather step, see if the user specified that they found new object
                        if (interactionModel.InteractionState.SelectedStep is GatherStep gatherStep)
                        {
                            // Recall how many objects we had previously
                            var previouslyFoundObjectsCount = interactionModel.InteractionState.FoundObjects.Count;

                            // Invoke the actions to update the objects from speech reco
                            yield return DialogAction.Execute(interactionModel.UpdateFoundObjectNamesFromSpeechReco(speechRecognition, gatherStep.Objects));

                            // If the user specified that we identified new objects
                            if (interactionModel.InteractionState.FoundObjects.Count > previouslyFoundObjectsCount)
                            {
                                yield return DialogAction.Speak(Language.GotIt);
                                if (gatherStep.Objects.All(interactionModel.InteractionState.FoundObjects.Contains))
                                {
                                    interactionModel.InteractionState.GemState = GemState.AtUserInterface();
                                    yield return DialogAction.Speak("Now that you have everything we need, let's move on to the next step.");
                                    interactionModel.InteractionState.SelectedStepIndex++;
                                    yield return DialogAction.Execute(interactionModel.ContinueWithSelectedStep);
                                    yield break;
                                }
                                else
                                {
                                    yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                                    yield break;
                                }
                            }
                            else
                            {
                                yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                            }
                        }
                        else if (interactionModel.Configuration.UsesLLMQueryLibrary)
                        {
                            yield return DialogAction.Execute(interactionModel.AnswerOpenEndedQuestion(speechRecognition));
                            yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                        }
                        else
                        {
                            yield return DialogAction.Speak(Language.Nonunderstanding);
                            yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                        }
                    }
                }
                else if (inputEvent is ObjectsDetectedInputEvent objectsDetectedInputEvent)
                {
                    // update the text specs from the current objects but do some merging
                    interactionModel.RemoveAllTextBillboards();
                    var objectLocations = new Dictionary<string, Dictionary<string, Point3D>>();
                    foreach ((var @class, var instanceId, var location) in interactionModel.InteractionState.CurrentObjects)
                    {
                        if (!objectLocations.ContainsKey(@class))
                        {
                            objectLocations.Add(@class, new Dictionary<string, Point3D>() { { instanceId, location } });
                        }
                        else if (objectLocations[@class].Values.Min(v => location.DistanceTo(v)) > 0.2)
                        {
                            objectLocations[@class].Add(instanceId, location);
                        }
                    }

                    foreach (var name in objectLocations.Keys)
                    {
                        foreach (var location in objectLocations[name].Values)
                        {
                            interactionModel.AddTextBillboard(location, name);
                        }
                    }

                    // Add the objects to the list of found objects
                    foreach (var detectedObject in objectsDetectedInputEvent.DetectedObjects)
                    {
                        if (!interactionModel.InteractionState.FoundObjects.Contains(detectedObject))
                        {
                            interactionModel.InteractionState.FoundObjects.Add(detectedObject);
                        }
                    }

                    var answer = interactionModel.InteractionState.ISeeYouAlreadyHaveSpoken ?
                        $"I see that you also have the {objectsDetectedInputEvent.DetectedObjects.First()}." :
                        $"I see that you already have the {objectsDetectedInputEvent.DetectedObjects.First()}.";
                    interactionModel.InteractionState.ISeeYouAlreadyHaveSpoken = true;

                    // Move the gem to the object location
                    if (interactionModel.Configuration.MoveGemToShowObjectLocations)
                    {
                        interactionModel.InteractionState.GemState = GemState.PointingToObject(objectsDetectedInputEvent.DetectedObjects.First());
                    }

                    yield return DialogAction.Speak(answer);

                    var gatherStep = interactionModel.InteractionState.SelectedStep as GatherStep;

                    // Now if all objects of the gather step have been found
                    if (gatherStep.Objects.All(interactionModel.InteractionState.FoundObjects.Contains))
                    {
                        // Move on
                        interactionModel.InteractionState.GemState = GemState.AtUserInterface();
                        yield return DialogAction.Speak("Now that you have everything we need, let's move on to the next step.");
                        interactionModel.InteractionState.SelectedStepIndex++;
                        yield return DialogAction.Execute(interactionModel.ContinueWithSelectedStep);
                        yield break;
                    }
                    else
                    {
                        // O/w continue with the same dialog state
                        yield return DialogAction.ContinueWith<ExecuteStep>(noSpeechSynthesis: true);
                        yield break;
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Implements the dialog state for executing a complex step.
        /// </summary>
        public class ExecuteComplexStep : DialogState<DiamondInteractionModel>
        {
            /// <inheritdoc/>
            public override void OnEnter(DiamondInteractionModel interactionModel)
            {
                interactionModel.InteractionState.SelectedSubStepIndex = null;
                interactionModel.InteractionState.ShowSubSteps = false;
            }

            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => (interactionModel.InteractionState.SelectedStepIndex.Value switch
                {
                    0 => $"The first step is to {interactionModel.InteractionState.Task.Steps[0].GetSpokenInstructions().ToLower()}.",
                    _ => $"The next step is to {interactionModel.InteractionState.Task.Steps[interactionModel.InteractionState.SelectedStepIndex.Value].GetSpokenInstructions().ToLower()}."
                },
                    null);

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                interactionModel.InteractionState.SelectedSubStepIndex = 0;
                yield return DialogAction.ContinueWith<ExecuteSubStep>();
            }
        }

        /// <summary>
        /// Implements the dialog state for executing a sub-step.
        /// </summary>
        public class ExecuteSubStep : DialogState<DiamondInteractionModel>
        {
            private readonly bool usePreamble = true;

            /// <summary>
            /// Initializes a new instance of the <see cref="ExecuteSubStep"/> class.
            /// </summary>
            public ExecuteSubStep()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ExecuteSubStep"/> class.
            /// </summary>
            /// <param name="usePreamble">Indicates whether to use a preamble for the step (e.g., first, next, finally...)</param>
            public ExecuteSubStep(bool usePreamble)
            {
                this.usePreamble = usePreamble;
            }

            /// <inheritdoc/>
            public override void OnEnter(DiamondInteractionModel interactionModel)
            {
                interactionModel.InteractionState.ShowSubSteps = true;
                interactionModel.InteractionState.ShowSubStepModels = true;
            }

            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
            {
                string systemPrompt = (interactionModel.InteractionState.SelectedStep as ComplexStep).SubSteps[interactionModel.InteractionState.SelectedSubStepIndex.Value].Description.TrimEnd('.');

                if (this.usePreamble)
                {
                    if (interactionModel.InteractionState.SelectedSubStepIndex == 0)
                    {
                        systemPrompt = $"First, {systemPrompt.ToLower()}.";
                    }
                    else if (interactionModel.InteractionState.SelectedSubStepIndex > 1 && interactionModel.InteractionState.SelectedSubStepIndex == (interactionModel.InteractionState.SelectedStep as ComplexStep).SubSteps.Count - 1)
                    {
                        systemPrompt = $"Finally, {systemPrompt.ToLower()}.";
                    }
                    else
                    {
                        systemPrompt = $"Next, {systemPrompt.ToLower()}.";
                    }
                }
                else
                {
                    systemPrompt = $"{systemPrompt.Capitalize()}.";
                }

                var userResponseSet = new List<string>() { "Next step." };
                if ((interactionModel.InteractionState.SelectedSubStepIndex > 0) || (interactionModel.InteractionState.SelectedStepIndex > 0))
                {
                    userResponseSet.Add("Previous step.");
                }

                userResponseSet.Add("Let's abandon this task.");

                return (systemPrompt, userResponseSet.ToArray());
            }

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                var speechRecognitionResult = (inputEvent as SpeechRecognitionInputEvent)?.SpeechRecognitionResult;
                if (speechRecognitionResult != null && speechRecognitionResult.ContainsOneOf("next", "done", "okay", "ok"))
                {
                    // If we have a next sub-step
                    if (interactionModel.InteractionState.SelectedSubStepIndex < (interactionModel.InteractionState.SelectedStep as ComplexStep).SubSteps.Count - 1)
                    {
                        interactionModel.InteractionState.SelectedSubStepIndex++;
                        yield return DialogAction.ContinueWith<ExecuteSubStep>();
                    }

                    // O/w if we have a next step
                    else if (interactionModel.InteractionState.SelectedStepIndex < interactionModel.InteractionState.Task.Steps.Count - 1)
                    {
                        yield return DialogAction.Speak("Great.");
                        interactionModel.InteractionState.SelectedStepIndex++;
                        interactionModel.InteractionState.SelectedSubStepIndex = null;
                        yield return DialogAction.Execute(interactionModel.ContinueWithSelectedStep);
                    }

                    // O/w if this is the end of the task.
                    else
                    {
                        yield return DialogAction.Speak("Congratulations. You have finished the task.");
                        if (string.IsNullOrEmpty(interactionModel.Configuration.AutoStartTaskName))
                        {
                            interactionModel.ClearTask();
                            yield return DialogAction.Speak("Is there something else I can help you with today?");
                            yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                        }
                        else
                        {
                            yield return DialogAction.Speak("I'll see you next time. Bye bye.");
                            yield return DialogAction.ExitCommand();
                        }
                    }
                }
                else if (speechRecognitionResult != null &&
                    speechRecognitionResult.Contains("previous") &&
                    speechRecognitionResult.Contains("step"))
                {
                    if (interactionModel.InteractionState.SelectedSubStepIndex > 0)
                    {
                        interactionModel.InteractionState.SelectedSubStepIndex--;
                        yield return DialogAction.ContinueWith(new ExecuteSubStep(usePreamble: false));
                    }
                    else if (interactionModel.InteractionState.SelectedStepIndex > 0)
                    {
                        interactionModel.InteractionState.SelectedStepIndex--;
                        if (interactionModel.InteractionState.TryGetSelectedStepOfType<ComplexStep>(out var complexStep))
                        {
                            interactionModel.InteractionState.SelectedSubStepIndex = complexStep.SubSteps.Count - 1;
                            yield return DialogAction.ContinueWith(new ExecuteSubStep(usePreamble: false));
                        }
                        else
                        {
                            interactionModel.InteractionState.SelectedSubStepIndex = null;
                            yield return DialogAction.ContinueWith(new ExecuteStep(usePreamble: false));
                        }
                    }
                    else
                    {
                        yield return DialogAction.Speak("We are already at the first step of the task.");
                        yield return DialogAction.ContinueWith<ExecuteSubStep>(noSpeechSynthesis: true);
                    }
                }
                else if (speechRecognitionResult != null && speechRecognitionResult.ContainsOneOf("freeze", "pause"))
                {
                    yield return DialogAction.ContinueWith(new PauseInteraction(this));
                }
                else if (speechRecognitionResult != null && speechRecognitionResult.Contains("abandon"))
                {
                    yield return DialogAction.Execute(interactionModel.AbandonTask());
                }
                else
                {
                    if (interactionModel.Configuration.UsesLLMQueryLibrary)
                    {
                        yield return DialogAction.Execute(interactionModel.AnswerOpenEndedQuestion(speechRecognitionResult));
                        yield return DialogAction.ContinueWith<ExecuteSubStep>(noSpeechSynthesis: true);
                    }
                    else
                    {
                        yield return DialogAction.Speak(Language.Nonunderstanding);
                        yield return DialogAction.ContinueWith<ExecuteSubStep>(noSpeechSynthesis: true);
                    }
                }
            }
        }

        /// <summary>
        /// Implements the dialog state for asking context questions.
        /// </summary>
        public class AskContextQuestions : DialogState<DiamondInteractionModel>
        {
            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => (interactionModel.InteractionState.ContextQuestions[interactionModel.InteractionState.ContextAnswers.Count], new string[] { "..." });

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                if (inputEvent is SpeechRecognitionInputEvent speechRecognitionInputEvent)
                {
                    if (speechRecognitionInputEvent.SpeechRecognitionResult.ContainsOneOf("pause", "freeze"))
                    {
                        yield return DialogAction.ContinueWith(new PauseInteraction(this));
                    }
                    else
                    {
                        interactionModel.InteractionState.ContextAnswers.Add(speechRecognitionInputEvent.SpeechRecognitionResult);

                        // If we don't have any more questions
                        if (interactionModel.InteractionState.ContextQuestions.Count == interactionModel.InteractionState.ContextAnswers.Count)
                        {
                            // Then generate the task and start it
                            yield return DialogAction.Execute(interactionModel.GenerateTask);
                            if (interactionModel.InteractionState.Task == null)
                            {
                                yield return DialogAction.Speak("I'm sorry but I don't think I can actually help with this task.");
                                yield return DialogAction.Speak("Is there anything else you'd like to do today?");
                                yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                            }
                            else
                            {
                                yield return DialogAction.Execute(interactionModel.StartTask);
                                yield return DialogAction.Execute(interactionModel.ContinueWithSelectedStep);
                            }
                        }
                        else
                        {
                            // O/w go on to the next question
                            yield return DialogAction.ContinueWith<AskContextQuestions>();
                        }
                    }
                }
                else
                {
                    yield return DialogAction.ContinueWith<AskContextQuestions>(noSpeechSynthesis: true);
                }
            }
        }

        /// <summary>
        /// Implements the dialog state for the end of the dialog.
        /// </summary>
        public class WeAreDone : DialogState<DiamondInteractionModel>
        {
            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => ("This is the end of the dialog.",
                    new string[] { "Ok." });

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                yield return DialogAction.ContinueWith<WeAreDone>();
            }
        }

        /// <summary>
        /// Implements the dialog state for pausing the interaction.
        /// </summary>
        public class PauseInteraction : DialogState<DiamondInteractionModel>
        {
            private readonly DialogState<DiamondInteractionModel> previousDialogState;

            /// <summary>
            /// Initializes a new instance of the <see cref="PauseInteraction"/> class.
            /// </summary>
            public PauseInteraction()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PauseInteraction"/> class.
            /// </summary>
            /// <param name="previousDialogState">The previous dialog state.</param>
            public PauseInteraction(DialogState<DiamondInteractionModel> previousDialogState)
            {
                this.previousDialogState = previousDialogState;
            }

            /// <inheritdoc/>
            public override void OnEnter(DiamondInteractionModel interactionModel)
                => interactionModel.InteractionState.IsPaused = true;

            /// <inheritdoc/>
            public override void OnLeave(DiamondInteractionModel interactionModel)
                => interactionModel.InteractionState.IsPaused = false;

            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => ("Interaction is paused.",
                    new string[] { "Resume the interaction." });

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                if (inputEvent is SpeechRecognitionInputEvent speechRecognitionInputEvent &&
                    speechRecognitionInputEvent.SpeechRecognitionResult.ToLower().Contains("resume"))
                {
                    yield return DialogAction.Speak("Ok. Resuming.");
                    yield return DialogAction.ContinueWith(this.previousDialogState);
                }
                else
                {
                    yield return DialogAction.ContinueWith(this, noSpeechSynthesis: true);
                }
            }
        }

        /// <summary>
        /// Implements the dialog state for taking a note.
        /// </summary>
        public class TakeANote : DialogState<DiamondInteractionModel>
        {
            private readonly DialogState<DiamondInteractionModel> previousDialogState;

            /// <summary>
            /// Initializes a new instance of the <see cref="TakeANote"/> class.
            /// </summary>
            /// <param name="previousDialogState">The previous dialog state.</param>
            public TakeANote(DialogState<DiamondInteractionModel> previousDialogState)
            {
                this.previousDialogState = previousDialogState;
            }

            /// <inheritdoc/>
            public override (string SystemPrompt, string[] UserResponseSet) GetSystemPromptAndUserResponseSet(DiamondInteractionModel interactionModel)
                => (null, new string[] { "..." });

            /// <inheritdoc/>
            public override IEnumerable<DialogAction> GetNextDialogActions(IInputEvent inputEvent, DiamondInteractionModel interactionModel)
            {
                if (inputEvent is SpeechRecognitionInputEvent)
                {
                    yield return DialogAction.Speak("Got it.");
                    yield return DialogAction.ContinueWith(this.previousDialogState);
                }
            }
        }
    }
}
