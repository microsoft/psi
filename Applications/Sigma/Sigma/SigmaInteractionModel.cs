// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.Spatial.Euclidean;
    using static Sigma.Diamond.DiamondDialogStates;

    /// <summary>
    /// Defines the Sigma interaction model.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TInteractionState">The type of the interaction state.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    public class SigmaInteractionModel<TTask, TConfiguration, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>
        : InteractionModel<TConfiguration, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>
        where TTask : Task, IInteropSerializable, new()
        where TConfiguration : SigmaComputeServerPipelineConfiguration, new()
        where TPersistentState : SigmaPersistentState<TTask>, new()
        where TInteractionState : SigmaInteractionState<TTask>, new()
        where TUserInterfaceState : SigmaUserInterfaceState, new()
        where TUserInterfaceCommands : SigmaUserInterfaceCommands, new()
    {
        private CoordinateSystem initialUserInterfacePose = null;

        /// <summary>
        /// Gets or sets the user state.
        /// </summary>
        public UserState UserState { get; set; }

        /// <inheritdoc/>
        public override void ComputeUserInterfaceCommands(TUserInterfaceCommands userInterfaceCommands)
        {
            // Begin by nulling out the user interface commands
            userInterfaceCommands.MoveUserInterfaceToPoseCommand = null;
            userInterfaceCommands.GemUserInterfaceCommand = null;

            // If the UI doesn't yet have a position, then set it to the default initial position
            if (this.UserInterfaceState.UserInterfacePose == null)
            {
                // Compute initial user interface position 50 cm in front and 10 cm down from the user's head
                if (this.initialUserInterfacePose == null && this.UserState?.Head != null)
                {
                    var xAxis = this.UserState.Head.XAxis.ProjectOn(Constants.HorizontalPlane).Direction;
                    var position = this.UserState.Head.Origin + xAxis.ScaleBy(0.5) + UnitVector3D.ZAxis.ScaleBy(-0.15);
                    this.initialUserInterfacePose = Operators.GetTargetOrientedCoordinateSystem(position, this.UserState.Head.Origin);
                }

                // If we have established an available initial position
                if (this.initialUserInterfacePose != null)
                {
                    // Then create the corresponding user interface commands
                    userInterfaceCommands.MoveUserInterfaceToPoseCommand = this.initialUserInterfacePose;
                    userInterfaceCommands.GemUserInterfaceCommand = new GemUserInterfaceCommand()
                    {
                        GemPose = this.initialUserInterfacePose,
                        GemIsRotating = false,
                    };
                }
            }
            else
            {
                // O/w if the UI has an already existing position, figure out if we need to issue any
                // commands to move the UI or the gem to a new position

                // If we have a new user interface pose target specified
                if (this.InteractionState.MoveUserInterfaceToPose != null)
                {
                    // If the user interface state has already reached the target pose
                    if (Equals(this.UserInterfaceState.UserInterfacePose, this.InteractionState.MoveUserInterfaceToPose))
                    {
                        // Then the action has already been performed, clear it out from the state.
                        this.InteractionState.MoveUserInterfaceToPose = null;
                    }
                    else
                    {
                        // O/w adopt a command to move the UI and the gem to the desired location
                        userInterfaceCommands.MoveUserInterfaceToPoseCommand = this.InteractionState.MoveUserInterfaceToPose;
                        userInterfaceCommands.GemUserInterfaceCommand = new GemUserInterfaceCommand()
                        {
                            GemPose = this.InteractionState.MoveUserInterfaceToPose,
                            GemIsRotating = false,
                        };
                    }
                }

                // If there is no gem command already constructed (one could have been constructed above as part of
                // moving the whole UI)
                if (userInterfaceCommands.GemUserInterfaceCommand == null)
                {
                    // Then construct one based on the gem behavior

                    // If the gem behavior is to point to a specific object
                    if (this.InteractionState.GemState.IsPointingToObject(out var objectName))
                    {
                        // Construct a command to point to the object if the gem is not already pointing to it
                        var objectLocation = this.InteractionState.CurrentObjects.First(t => t.Class == objectName).Location;
                        if (this.UserInterfaceState.GemPose == null ||
                            this.UserInterfaceState.GemPose.Origin != objectLocation)
                        {
                            userInterfaceCommands.GemUserInterfaceCommand =
                                new ()
                                {
                                    GemPose = new CoordinateSystem(objectLocation + UnitVector3D.ZAxis.ScaleBy(0.15), UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis),
                                    GemIsRotating = true,
                                    GemSize = 0.07f,
                                };
                        }
                    }
                    else
                    {
                        // O/w the gem behavior is to point to the user interface

                        // If the gem is not already pointing to the user interface
                        if (this.UserInterfaceState.GemPose == null ||
                            this.UserInterfaceState.GemPose.Origin != this.UserInterfaceState.UserInterfacePose.Origin)
                        {
                            // Then construct a command to point to the user interface
                            userInterfaceCommands.GemUserInterfaceCommand =
                                new ()
                                {
                                    GemPose = this.UserInterfaceState.UserInterfacePose,
                                    GemIsRotating = false,
                                    GemSize = 0.008f,
                                };
                        }
                    }
                }
            }

            var objectsChecklist = (this.InteractionState.SelectedStep as GatherStep)
                ?.Objects
                ?.Select(o => (o, this.InteractionState.FoundObjects.Contains(o), true))
                ?.ToList();

            userInterfaceCommands.TaskPanelUserInterfaceCommand = new TaskPanelUserInterfaceCommand()
            {
                Mode = this.InteractionState.TaskPanelMode,
                Task = this.InteractionState.Task,
                SelectedStepIndex = this.InteractionState.SelectedStepIndex,
                SelectedSubStepIndex = this.InteractionState.SelectedSubStepIndex,
                ShowOnlySelectedStep = this.InteractionState.ShowOnlySelectedStep,
                ShowComplexStepObjects = this.InteractionState.ShowComplexStepObjects,
                ShowSubSteps = this.InteractionState.ShowSubSteps,
                ObjectsChecklist = objectsChecklist,
            };

            // Now generate the corresponding models user interface command
            userInterfaceCommands.ModelsUserInterfaceCommand.Clear();
            if (this.InteractionState.ShowSubStepModels &&
                this.InteractionState.SelectedStep is ComplexStep complexStep &&
                this.InteractionState.SelectedSubStepIndex.HasValue)
            {
                var subStep = complexStep.SubSteps[this.InteractionState.SelectedSubStepIndex.Value];

                foreach (var virtualObjectSpec in subStep.VirtualObjects)
                {
                    if (virtualObjectSpec.SpatialPose.TryGetWorldCoordinateSystem(this.PersistentState.KnownSpatialLocations, out var worldCoordinateSystem))
                    {
                        userInterfaceCommands.ModelsUserInterfaceCommand.Add(
                            virtualObjectSpec.Name,
                            new ModelUserInterfaceCommand()
                            {
                                ModelName = virtualObjectSpec.Name,
                                ModelType = virtualObjectSpec.ModelType.ToLower(),
                                Visible = true,
                                CanBeMovedByUser = false,
                                CanBeScaledByUser = false,
                                Pose = worldCoordinateSystem,
                                Wireframe = false,
                            });
                    }
                }
            }

            // Create the cooresponding timers user interface command
            userInterfaceCommands.TimersUserInterfaceCommand = this.InteractionState.Timers.ToDictionary(
                kvp => kvp.Key,
                kvp => new TimerUserInterfaceCommand()
                {
                    Guid = kvp.Key,
                    Location = kvp.Value.Location,
                    ExpiryDateTime = kvp.Value.ExpiryTime,
                });

            // Create the corresponding text billboards user interface command
            userInterfaceCommands.TextBillboardsUserInterfaceCommand = this.InteractionState.TextBillboards.Values.Select(t => new TextBillboardUserInterfaceCommand(t.Location, t.Text)).ToList();

            // Exit
        }

        /// <summary>
        /// Gets the top level intent.
        /// </summary>
        /// <param name="recognitionResult">The speech recognition result.</param>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> GetTopLevelIntent(string recognitionResult)
        {
            this.InteractionState.TopLevelIntent = null;
            this.InteractionState.TaskName = null;

            recognitionResult = recognitionResult.ToLower();
            var knownTaskNames = this.PersistentState.TaskLibrary.Tasks.Select(t => t.Name);

            if (this.Configuration.UsesLLMQueryLibrary)
            {
                yield return DialogAction.RunLLMQuery("IntentReco-Diamond", recognitionResult);
                if (this.InteractionState.LLMResult.StartsWith("guide("))
                {
                    // Extract the top level intent
                    this.InteractionState.TopLevelIntent = this.InteractionState.LLMResult.Substring(0, this.InteractionState.LLMResult.IndexOf('('));

                    // Get out the task name as specified by the user
                    var userSpecifiedTaskName = this.InteractionState.LLMResult.Trim().Substring(this.InteractionState.TopLevelIntent.Length + 1).TrimEnd(')');

                    // Run a query to evaluate if it's in the known list
                    yield return DialogAction.RunLLMQuery("GuideNameExtraction", string.Join(",", knownTaskNames), userSpecifiedTaskName);

                    // Assign the name based on the query results
                    this.InteractionState.LLMResult = this.InteractionState.LLMResult.TrimEnd('.');
                    this.InteractionState.TaskName = this.IsKnownTask(this.InteractionState.LLMResult) ? this.InteractionState.LLMResult : userSpecifiedTaskName.Capitalize();
                }
                else if (this.InteractionState.LLMResult == "list")
                {
                    this.InteractionState.TopLevelIntent = "list";
                    this.InteractionState.TaskName = null;
                }
                else
                {
                    this.InteractionState.TopLevelIntent = null;
                    this.InteractionState.TaskName = null;
                }
            }
            else
            {
                if (recognitionResult.Contains("what") || recognitionResult.Contains("list"))
                {
                    this.InteractionState.TopLevelIntent = "list";
                    this.InteractionState.TaskName = null;
                }
                else if (recognitionResult.TryGetStringAfter("help me", out var spokenTaskName) ||
                    recognitionResult.TryGetStringAfter("guide me through how to", out spokenTaskName) ||
                    recognitionResult.TryGetStringAfter("show me how to", out spokenTaskName))
                {
                    if (knownTaskNames.TryGetLargestWordOverlap(spokenTaskName, out var taskName))
                    {
                        this.InteractionState.TopLevelIntent = "guide";
                        this.InteractionState.TaskName = taskName;
                    }
                    else
                    {
                        this.InteractionState.TopLevelIntent = null;
                        this.InteractionState.TaskName = null;
                    }
                }
                else
                {
                    this.InteractionState.TopLevelIntent = null;
                    this.InteractionState.TaskName = null;
                }
            }
        }

        /// <summary>
        /// Adds a specified timer.
        /// </summary>
        /// <param name="doStep">The <see cref="DoStep"/> associated with the timer.</param>
        /// <param name="point3D">The location to display the timer at.</param>
        public virtual void StartTimer(DoStep doStep, Point3D point3D)
            => this.InteractionState.Timers.Add(Guid.NewGuid(), (doStep, point3D, DateTime.Now + doStep.TimerDuration));

        /// <summary>
        /// Clears all the timers.
        /// </summary>
        public virtual void RemoveAllTimers()
            => this.InteractionState.Timers.Clear();

        /// <summary>
        /// Adds a text billboard.
        /// </summary>
        /// <param name="point3D">The location to display the text billboard at.</param>
        /// <param name="text">The text to display.</param>
        public virtual void AddTextBillboard(Point3D point3D, string text)
            => this.InteractionState.TextBillboards.Add(Guid.NewGuid(), (null, point3D, text));

        /// <summary>
        /// Clears the text billboards.
        /// </summary>
        public virtual void RemoveAllTextBillboards()
            => this.InteractionState.TextBillboards.Clear();

        /// <summary>
        /// Generates the specified task.
        /// </summary>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> GenerateTask()
        {
            var answers = string.Empty;
            var contextQuestions = this.InteractionState.ContextQuestions;
            var contextAnswers = this.InteractionState.ContextAnswers;

            // If we have context questions
            if (contextQuestions != null && contextQuestions.Count != 0)
            {
                // Prep the prompt
                for (int i = 0; i < contextQuestions.Count; i++)
                {
                    answers += $"Question: {contextQuestions[i]}\\nHumanAnswer:{contextAnswers[i]}\\n";
                }

                yield return DialogAction.RunLLMQuery("GetRecipeWithUserContext", this.InteractionState.TaskName, answers);
            }
            else
            {
                yield return DialogAction.RunLLMQuery("GetRecipe", this.InteractionState.TaskName);
            }

            if (string.IsNullOrEmpty(this.InteractionState.LLMResult) ||
                this.InteractionState.LLMResult.ToLower().Contains("no-instructions"))
            {
                this.InteractionState.Task = null;
                yield break;
            }

            var recipeSteps = this.InteractionState.LLMResult.Split('\n');
            if (recipeSteps[0].TryGetStringAfter("gather the following equipment:", out var objectsList))
            {
                var objects = objectsList.Split(',').Select(x =>
                {
                    x = x.Trim().Trim('.');
                    if (x.StartsWith("and "))
                    {
                        x = x.Substring("and ".Length);
                    }

                    return x;
                }).ToList();

                this.InteractionState.Task = new TTask()
                {
                    Name = this.InteractionState.TaskName,
                    Steps = new (),
                };

                this.InteractionState.Task.Steps.Add(new GatherStep("1", "Gather", "Objects", objects));
                for (int i = 1; i < recipeSteps.Length; i++)
                {
                    if (recipeSteps[i].TryGetStringAfter(".", out var action))
                    {
                        action = action.Trim().ToSentenceCase();
                        this.InteractionState.Task.Steps.Add(new DoStep($"{i + 1}", action));
                    }
                    else
                    {
                        this.InteractionState.Task = null;
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Abandons the current task and continues with a specified state.
        /// </summary>
        /// <typeparam name="TInteractionModel">The type of the interaction model.</typeparam>
        /// <param name="nextDialogState">The next dialog state.</param>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> AbandonTask<TInteractionModel>(DialogState<TInteractionModel> nextDialogState)
        {
            this.ClearTask();
            if (nextDialogState != null)
            {
                yield return DialogAction.Speak("No problem.");
                yield return DialogAction.Speak("Is there anything else you'd like to do today?");
                yield return DialogAction.ContinueWith(nextDialogState, noSpeechSynthesis: true);
            }
            else
            {
                yield return DialogAction.Speak("Sure, we can stop here. I'll see you next time.");
                yield return DialogAction.ExitCommand();
            }
        }

        /// <summary>
        /// Clear the current task.
        /// </summary>
        public void ClearTask()
        {
            this.InteractionState.GemState = GemState.AtUserInterface();
            this.InteractionState.TaskPanelMode = TaskPanelMode.None;
        }

        /// <summary>
        /// Changes the user interface position to a glaceable location.
        /// </summary>
        public void ChangeUserInterfacePositionToGlanceable()
        {
            // Set the target user interface position to a new location to the left and below the head
            var xAxis = this.UserState.Head.XAxis.ProjectOn(Constants.HorizontalPlane).Direction;
            var yAxis = UnitVector3D.ZAxis.CrossProduct(xAxis);
            var position = this.UserState.Head.Origin + xAxis.ScaleBy(0.5 * Math.Cos(Math.PI / 4)) + yAxis.ScaleBy(0.4 * Math.Sin(Math.PI / 4)) + UnitVector3D.ZAxis.ScaleBy(-0.15);
            this.InteractionState.MoveUserInterfaceToPose = Operators.GetTargetOrientedCoordinateSystem(position, this.UserState.Head.Origin);
            this.InteractionState.MovedToGlanceable = true;
        }

        /// <summary>
        /// Gets the context questions.
        /// </summary>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> GetContextQuestions()
        {
            yield return DialogAction.RunLLMQuery("GetUserContext", this.InteractionState.TaskName);

            if (string.IsNullOrEmpty(this.InteractionState.LLMResult) ||
                this.InteractionState.LLMResult.ToLower().Contains("no-context"))
            {
                this.InteractionState.ContextQuestions = null;
                yield break;
            }

            var results = this.InteractionState.LLMResult.Split('\n').ToList();
            this.InteractionState.ContextQuestions = new List<string>();
            this.InteractionState.ContextAnswers = new List<string>();

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].StartsWith($"{i + 1}:"))
                {
                    results[i].TryGetStringAfter(":", out var question);
                    this.InteractionState.ContextQuestions.Add(question.Trim().Capitalize());
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Moves the UI to a glanceable position.
        /// </summary>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> MoveToGlanceablePosition()
        {
            if (!this.InteractionState.MovedToGlanceable)
            {
                this.ChangeUserInterfacePositionToGlanceable();
                yield return DialogAction.Speak($"Before starting, I'll move more to the side here to keep out of the way, but you can move me at any time wherever you'd like by pinching and dragging the blue diamond gem.");
            }
        }

        /// <summary>
        /// Determines whether the specified task is known.
        /// </summary>
        /// <param name="taskName">The task name.</param>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual bool IsKnownTask(string taskName)
            => this.PersistentState.TaskLibrary.Tasks.Select(t => t.Name.ToLower()).Contains(taskName.ToLower());

        /// <summary>
        /// Tries to retrieve a known task.
        /// </summary>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual bool TryGetKnownTask()
        {
            var dictionary = this.PersistentState.TaskLibrary.Tasks.ToDictionary(t => t.Name.ToLower(), t => t);
            this.InteractionState.Task = dictionary.ContainsKey(this.InteractionState.TaskName.ToLower()) ? dictionary[this.InteractionState.TaskName.ToLower()] : null;
            return dictionary.ContainsKey(this.InteractionState.TaskName.ToLower());
        }

        /// <summary>
        /// Starts the task.
        /// </summary>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> StartTask()
        {
            this.InteractionState.TaskPanelMode = TaskPanelMode.Task;
            this.InteractionState.SelectedStepIndex = 0;

            // Setup the objects to track
            if (this.Configuration.ObjectTrackingPipelineConfiguration != null)
            {
                if (this.Configuration.UsesLLMQueryLibrary)
                {
                    var allSteps = this.InteractionState.Task.Steps.Select(s => s is GatherStep gatherStep ? string.Join(",", gatherStep.Objects) : s.GetDisplayInstructions());
                    yield return DialogAction.RunLLMQuery("ExtractObjects", string.Join(",", allSteps));
                    this.InteractionState.ObjectClasses = this.InteractionState.LLMResult.Split(',').Select(x => x.Trim()).ToList();
                }
                else
                {
                    var objectClasses = string.Join(",", this.InteractionState.Task.GetStepsOfType<GatherStep>().Select(s => string.Join(",", s.Objects)));
                    this.InteractionState.ObjectClasses = objectClasses.Split(',').Select(x => x.Trim()).ToList();
                }
            }
        }

        /// <summary>
        /// Updates the found objects from speech recognition.
        /// </summary>
        /// <param name="recognitionResult">The speech recognition results.</param>
        /// <param name="objectNames">The object names.</param>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> UpdateFoundObjectNamesFromSpeechReco(
            string recognitionResult,
            IEnumerable<string> objectNames)
        {
            if (this.Configuration.UsesLLMQueryLibrary)
            {
                yield return DialogAction.RunLLMQuery("IdentifyObjectName", string.Join(",", objectNames), recognitionResult);
                if (this.InteractionState.LLMResult != "it doesn't belong")
                {
                    foreach (var foundObject in this.InteractionState.LLMResult.Split(',').Select(o => o.Trim()))
                    {
                        if (!this.InteractionState.FoundObjects.Contains(foundObject))
                        {
                            this.InteractionState.FoundObjects.Add(foundObject);
                        }
                    }
                }
            }
            else
            {
                if (recognitionResult.TryGetStringAfter("have the", out var spokenObjectName) ||
                    recognitionResult.TryGetStringAfter("found the", out spokenObjectName))
                {
                    if (objectNames.TryGetLargestWordOverlap(spokenObjectName, out var foundObject))
                    {
                        this.InteractionState.FoundObjects.Add(foundObject);
                    }
                }
            }
        }

        /// <summary>
        /// Answers an open-ended question.
        /// </summary>
        /// <param name="speechRecognition">The speech recognition result containing the open ended question.</param>
        /// <returns>The set of corresponding dialog actions.</returns>
        public virtual IEnumerable<DialogAction> AnswerOpenEndedQuestion(string speechRecognition)
        {
            if (this.InteractionState.TryGetSelectedStepOfType<GatherStep>(out var gatherStep))
            {
                var objects = string.Join(", ", gatherStep.Objects);
                var step = $"{this.InteractionState.SelectedStepIndex + 1}. Gather the necessary equipment: {objects}.";
                yield return DialogAction.RunLLMQuery("GetOpenEndedResponse", this.InteractionState.Task.Name, step, speechRecognition);
            }
            else
            {
                var step = $"{this.InteractionState.SelectedStepIndex + 1}. {this.InteractionState.SelectedStep.GetSpokenInstructions()}.";
                yield return DialogAction.RunLLMQuery("GetOpenEndedResponse", this.InteractionState.Task.Name, step, speechRecognition);
            }

            if (!this.InteractionState.LLMResult.Contains("no-question"))
            {
                yield return DialogAction.Speak(this.InteractionState.LLMResult.Capitalize());
            }
        }
    }
}
