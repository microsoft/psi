// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using System;
    using System.Collections.Generic;
    using static Sigma.Diamond.DiamondDialogStates;

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
        /// Begins the task.
        /// </summary>
        /// <param name="isKnownTask">Indicates whether this is a known (library) task.</param>
        /// <returns>The set of corresponding dialog actions.</returns>
        /// <exception cref="Exception">An exception is thrown if the task generation policy is unknown.</exception>
        public virtual IEnumerable<DialogAction> BeginTask(bool isKnownTask)
        {
            if (this.Configuration.TaskGenerationPolicy == TaskGenerationPolicy.FromLibraryOnly)
            {
                if (isKnownTask)
                {
                    yield return DialogAction.Execute(this.StartTask);
                    yield return DialogAction.Execute(this.ContinueWithSelectedStep);
                }
                else
                {
                    yield return DialogAction.Speak("I'm sorry but I don't know how to help with this task.");
                    yield return DialogAction.Speak("Is there anything else you'd like to do today?");
                    yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                }
            }
            else if (this.Configuration.TaskGenerationPolicy == TaskGenerationPolicy.FromLibraryOrLLMGenerate)
            {
                if (isKnownTask)
                {
                    yield return DialogAction.Execute(this.StartTask);
                    yield return DialogAction.Execute(this.ContinueWithSelectedStep);
                }
                else
                {
                    // O/w if we are setup to ask context questions
                    if (this.Configuration.AskContextQuestionsBeforeGeneratingTask)
                    {
                        // Get the context questions
                        yield return DialogAction.Execute(this.GetContextQuestions);
                        if (this.InteractionState.ContextQuestions == null)
                        {
                            yield return DialogAction.Speak("I'm sorry but I don't think I can actually help with this task.");
                            yield return DialogAction.Speak("Is there anything else you'd like to do today?");
                            yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                        }
                        else
                        {
                            yield return DialogAction.Speak("First, a couple of quick questions.");
                            yield return DialogAction.ContinueWith<AskContextQuestions>();
                        }
                    }
                    else
                    {
                        // Generate the task
                        yield return DialogAction.Execute(this.GenerateTask);
                        if (this.InteractionState.Task == null)
                        {
                            yield return DialogAction.Speak("I'm sorry but I don't think I can actually help with this task.");
                            yield return DialogAction.Speak("Is there anything else you'd like to do today?");
                            yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                        }
                        else
                        {
                            yield return DialogAction.Execute(this.StartTask);
                            yield return DialogAction.Execute(this.ContinueWithSelectedStep);
                        }
                    }
                }
            }
            else if (this.Configuration.TaskGenerationPolicy == TaskGenerationPolicy.AlwaysLLMGenerate)
            {
                // O/w if we are setup to ask context questions
                if (this.Configuration.AskContextQuestionsBeforeGeneratingTask)
                {
                    // Get the context questions
                    yield return DialogAction.Execute(this.GetContextQuestions);
                    if (this.InteractionState.ContextQuestions == null)
                    {
                        yield return DialogAction.Speak("I'm sorry but I don't think I can actually help with this task.");
                        yield return DialogAction.Speak("Is there anything else you'd like to do today?");
                        yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                    }
                    else
                    {
                        yield return DialogAction.Speak("First, a couple of quick questions.");
                        yield return DialogAction.ContinueWith<AskContextQuestions>();
                    }
                }
                else
                {
                    // Generate the task
                    yield return DialogAction.Execute(this.GenerateTask);
                    if (this.InteractionState.Task == null)
                    {
                        yield return DialogAction.Speak("I'm sorry but I don't think I can actually help with this task.");
                        yield return DialogAction.Speak("Is there anything else you'd like to do today?");
                        yield return DialogAction.ContinueWith<WhatAreWeDoing>(noSpeechSynthesis: true);
                    }
                    else
                    {
                        yield return DialogAction.Execute(this.StartTask);
                        yield return DialogAction.Execute(this.ContinueWithSelectedStep);
                    }
                }
            }
            else
            {
                throw new Exception("Unexpected TaskGenerationPolicy.");
            }
        }

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
