// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality.Applications;
    using StereoKit;

    /// <summary>
    /// Implements a component for the user interface elements of the Sigma app.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    public class SigmaUserInterface<TTask, TUserInterfaceState, TPersistentState, TUserInterfaceCommands> : UserInterface<TPersistentState, TUserInterfaceState, TUserInterfaceCommands>, ISigmaUserInterface
        where TTask : Task, IInteropSerializable, new()
        where TUserInterfaceState : SigmaUserInterfaceState, new()
        where TPersistentState : SigmaPersistentState<TTask>
        where TUserInterfaceCommands : SigmaUserInterfaceCommands
    {
        // Configuration
        private readonly SigmaUserInterfaceConfiguration configuration;

        private readonly GemUserInterface gemUserInterface;
        private readonly BubbleDialogUserInterface bubbleDialogUserInterface;
        private readonly TaskPanelUserInterface<TTask> taskPanelUserInterface;
        private readonly ModelsUserInterface modelsUserInterface;
        private readonly TimersUserInterface timersUserInterface;
        private readonly TextBillboardsUserInterface textBillboardsUserInterface;
        private readonly Dictionary<string, (Model Model, Pose Pose, string Basis)> virtualObjects = new ();

        private CoordinateSystem targetUserInterfacePose = null;

        private Guid lastSynthesisSpeakCommandGuid = Guid.Empty;
        private Guid lastSynthesisStopCommandGuid = Guid.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaUserInterface{TTask, TUserInterfaceState, TPersistentState, TUserInterfaceCommands}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="availableModels">The set of available models to render.</param>
        /// <param name="name">An optional name for the component.</param>
        public SigmaUserInterface(Pipeline pipeline, SigmaUserInterfaceConfiguration configuration, Dictionary<string, Model> availableModels, string name = null)
            : base(pipeline, name ?? nameof(SigmaUserInterface<TTask, TUserInterfaceState, TPersistentState, TUserInterfaceCommands>))
        {
            this.configuration = configuration ?? new SigmaUserInterfaceConfiguration();

            this.gemUserInterface = new GemUserInterface(configuration.GemUserInterfaceConfiguration);
            this.gemUserInterface.OnGemMovedByUser += (s, e) =>
            {
                this.UserInterfaceState.UserInterfacePose = this.gemUserInterface.CurrentGemPose;
            };

            this.bubbleDialogUserInterface = new BubbleDialogUserInterface(configuration.BubbleDialogUserInterfaceConfiguration, "BubbleDialog");
            this.taskPanelUserInterface = new TaskPanelUserInterface<TTask>(configuration.TaskPanelUserInterfaceConfiguration, "TaskPanel");
            this.modelsUserInterface = new ModelsUserInterface(availableModels);
            this.timersUserInterface = new TimersUserInterface(configuration.TimersUserInterfaceConfiguration);
            this.textBillboardsUserInterface = new TextBillboardsUserInterface(configuration.TextBillboardsUserInterfaceConfiguration);

            this.SpeechSynthesisCommand = pipeline.CreateEmitter<string>(this, nameof(this.SpeechSynthesisCommand));
            this.Position = this.Out
                .Where(s => s.UserInterfacePose != null)
                .Select(s => s.UserInterfacePose.Origin, DeliveryPolicy.SynchronousOrThrottle).Out;
        }

        /// <inheritdoc/>
        public Emitter<string> SpeechSynthesisCommand { get; }

        /// <inheritdoc/>
        public Emitter<Point3D> Position { get; }

        /// <inheritdoc/>
        protected override void Initialize(TPersistentState persistentState, Envelope envelope)
        {
            this.taskPanelUserInterface.Initialize(persistentState?.TaskLibrary);
        }

        /// <inheritdoc/>
        protected override void Update(TUserInterfaceCommands userInterfaceCommands, Envelope envelope)
        {
            if (userInterfaceCommands != null)
            {
                if (userInterfaceCommands.ExitCommand)
                {
                    SK.Quit();
                    return;
                }

                // Trigger speech synthesis if we need to
                if (userInterfaceCommands.SpeechSynthesisCommand != null &&
                    userInterfaceCommands.SpeechSynthesisCommand.Guid != this.lastSynthesisSpeakCommandGuid &&
                    !string.IsNullOrEmpty(userInterfaceCommands.SpeechSynthesisCommand.Text))
                {
                    this.lastSynthesisSpeakCommandGuid = userInterfaceCommands.SpeechSynthesisCommand.Guid;
                    var synthesisText = userInterfaceCommands.SpeechSynthesisCommand.Text;
                    synthesisText = Regex.Replace(synthesisText, @"\b1\s+g\b", "1 gram");
                    synthesisText = Regex.Replace(synthesisText, @"(\d+)\s*g\b", "$1 grams");
                    synthesisText = Regex.Replace(synthesisText, @"\b1\s+l\b", "1 liter");
                    synthesisText = Regex.Replace(synthesisText, @"(\d+)\s*l\b", "$1 liters");
                    synthesisText = Regex.Replace(synthesisText, @"\b1\s+tsp\b", "1 teaspoon");
                    synthesisText = Regex.Replace(synthesisText, @"(\d+)\s*tsp\b", "$1 teaspoons");
                    synthesisText = Regex.Replace(synthesisText, @"\b1\s+tbsp\b", "1 tablespoon");
                    synthesisText = Regex.Replace(synthesisText, @"(\d+)\s*tbsp\b", "$1 tablespoons");
                    synthesisText = Regex.Replace(synthesisText, @"(\d+)\s*F\b", "$1 Fahrenheit");
                    this.SpeechSynthesisCommand.Post(synthesisText, this.SpeechSynthesisCommand.Pipeline.GetCurrentTime());
                }
                else if (userInterfaceCommands.SpeechSynthesisCommand != null &&
                    userInterfaceCommands.SpeechSynthesisCommand.Guid != this.lastSynthesisStopCommandGuid &&
                    userInterfaceCommands.SpeechSynthesisCommand.Stop)
                {
                    this.lastSynthesisStopCommandGuid = userInterfaceCommands.SpeechSynthesisCommand.Guid;
                    this.SpeechSynthesisCommand.Post(null, this.SpeechSynthesisCommand.Pipeline.GetCurrentTime());
                }

                // Update the various other components of the user interface
                this.gemUserInterface.Update(userInterfaceCommands.GemUserInterfaceCommand);
                this.bubbleDialogUserInterface.Update(userInterfaceCommands.BubbleDialogUserInterfaceCommand);
                this.taskPanelUserInterface.Update(userInterfaceCommands.TaskPanelUserInterfaceCommand);
                this.modelsUserInterface.Update(userInterfaceCommands.ModelsUserInterfaceCommand);
                this.timersUserInterface.Update(userInterfaceCommands.TimersUserInterfaceCommand);
                this.textBillboardsUserInterface.Update(userInterfaceCommands.TextBillboardsUserInterfaceCommand);

                // Update the target user interface pose
                if (userInterfaceCommands.MoveUserInterfaceToPoseCommand != null)
                {
                    this.targetUserInterfacePose = userInterfaceCommands.MoveUserInterfaceToPoseCommand;
                }

                // If we are in the process of moving the UI to a new location
                if (this.targetUserInterfacePose != null)
                {
                    // If the UI has not yet been placed
                    if (this.UserInterfaceState.UserInterfacePose == null)
                    {
                        // Then place it directly at the target position
                        this.UserInterfaceState.UserInterfacePose = this.targetUserInterfacePose;
                    }
                    else
                    {
                        // O/w if the gem is in the process of moving to the same location, don't move
                        // the UI until the gem arrives at the specified location
                        var gemIsMovingToTargetLocation = this.gemUserInterface.TargetGemPose != null &&
                            this.gemUserInterface.TargetGemPose.Origin == this.targetUserInterfacePose.Origin &&
                            this.gemUserInterface.CurrentGemPose.Origin != this.targetUserInterfacePose.Origin;

                        if (!gemIsMovingToTargetLocation)
                        {
                            this.UserInterfaceState.UserInterfacePose = this.targetUserInterfacePose;
                            this.targetUserInterfacePose = null;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void HandleUserInputs()
        {
            this.gemUserInterface.HandleUserInputs(this.UserState);
            this.timersUserInterface.HandleUserInputs(this.UserState);
        }

        /// <inheritdoc/>
        protected override void Render()
        {
            if (this.UserInterfaceState.UserInterfacePose != null)
            {
                this.UserInterfaceState.Rectangle3DUserInterfaces.Clear();

                // Render the bubbles dialog UI
                var bubbleDialogPose =
                    new CoordinateSystem(
                        new Point3D(0, 0, 0.01),
                        UnitVector3D.XAxis,
                        UnitVector3D.YAxis,
                        UnitVector3D.ZAxis)
                    .TransformBy(this.UserInterfaceState.UserInterfacePose);

                this.UserInterfaceState.Rectangle3DUserInterfaces.AddRange(
                    this.bubbleDialogUserInterface.Render(this.Renderer, bubbleDialogPose));

                // Render the task panel UI
                var taskPanelPose =
                    new CoordinateSystem(
                        new Point3D(0, -this.configuration.TaskPanelUserInterfaceConfiguration.Width / 2, -0.01),
                        UnitVector3D.XAxis,
                        UnitVector3D.YAxis,
                        UnitVector3D.ZAxis)
                    .TransformBy(this.UserInterfaceState.UserInterfacePose);

                this.UserInterfaceState.Rectangle3DUserInterfaces.AddRange(
                    this.taskPanelUserInterface.Render(this.Renderer, taskPanelPose));
            }

            // Render the Sigma gem
            this.gemUserInterface.Render(this.Renderer);
            this.UserInterfaceState.GemPose = this.gemUserInterface.CurrentGemPose;

            // Render the models
            this.modelsUserInterface.Render(this.Renderer);
            this.UserInterfaceState.ModelUserInterfaces = this.modelsUserInterface.State;

            // Render the timers
            this.timersUserInterface.Render(this.Renderer);

            // Render the text billboards
            this.textBillboardsUserInterface.Render(this.Renderer);
        }

        private void UpdateUserInterfacePosition()
        {
        }
    }
}
