// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Implements a base abstract class for an interaction state manager.
    /// </summary>
    /// <typeparam name="TConfiguration">The type of the configuration.</typeparam>
    /// <typeparam name="TInteractionModel">The interaction model.</typeparam>
    /// <typeparam name="TPersistentState">The type of the persistent state.</typeparam>
    /// <typeparam name="TInteractionState">The type of the interaction state.</typeparam>
    /// <typeparam name="TUserInterfaceState">The type of the user interface state.</typeparam>
    /// <typeparam name="TUserInterfaceCommands">The type of the user interface commands.</typeparam>
    public abstract class InteractionStateManager<TConfiguration, TInteractionModel, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>
        where TInteractionModel : InteractionModel<TConfiguration, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands>, new()
        where TPersistentState : class, new()
        where TInteractionState : class, new()
        where TUserInterfaceState : class, new()
        where TUserInterfaceCommands : class, new()
        where TConfiguration : ComputeServerPipelineConfiguration, new()
    {
        private readonly string name;
        private readonly TimeSpan minOutputTimeSpan = TimeSpan.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractionStateManager{TConfiguration, TInteractionModel, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <param name="maxOutputFrequency">An optional max frequency at which to output interface commands and output state.</param>
        public InteractionStateManager(Pipeline pipeline, string name, double maxOutputFrequency = 100)
        {
            this.name = name;
            this.minOutputTimeSpan = TimeSpan.FromSeconds(1d / maxOutputFrequency);

            this.UserInterfaceStateInput = pipeline.CreateReceiver<TUserInterfaceState>(this, this.ReceiveUserInterfaceState, nameof(this.UserInterfaceStateInput));
            this.PersistentStateOutput = pipeline.CreateEmitter<TPersistentState>(this, nameof(this.PersistentStateOutput));
            this.InteractionStateOutput = pipeline.CreateEmitter<TInteractionState>(this, nameof(this.InteractionStateOutput));
            this.UserInterfaceCommandsOutput = pipeline.CreateEmitter<TUserInterfaceCommands>(this, nameof(this.UserInterfaceCommandsOutput));

            pipeline.PipelineCompleted += this.OnPipelineCompleted;
        }

        /// <summary>
        /// Gets a receiver for the user interface state.
        /// </summary>
        public Receiver<TUserInterfaceState> UserInterfaceStateInput { get; }

        /// <summary>
        /// Gets the emitter for the persistent state.
        /// </summary>
        public Emitter<TPersistentState> PersistentStateOutput { get; }

        /// <summary>
        /// Gets the emitter for the interaction state.
        /// </summary>
        public Emitter<TInteractionState> InteractionStateOutput { get; }

        /// <summary>
        /// Gets the emitter for the user interface commands.
        /// </summary>
        public Emitter<TUserInterfaceCommands> UserInterfaceCommandsOutput { get; }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        public TConfiguration Configuration
        {
            get => this.InteractionModel.Configuration;
            protected set => this.InteractionModel.Configuration = value;
        }

        /// <summary>
        /// Gets the interaction model.
        /// </summary>
        public TInteractionModel InteractionModel { get; } = new TInteractionModel();

        /// <summary>
        /// Gets or sets the persistent state.
        /// </summary>
        public TPersistentState PersistentState
        {
            get => this.InteractionModel.PersistentState;
            protected set => this.InteractionModel.PersistentState = value;
        }

        /// <summary>
        /// Gets or sets the user interface state.
        /// </summary>
        public TUserInterfaceState UserInterfaceState
        {
            get => this.InteractionModel.UserInterfaceState;
            protected set => this.InteractionModel.UserInterfaceState = value;
        }

        /// <summary>
        /// Gets or sets the interaction state.
        /// </summary>
        public TInteractionState InteractionState
        {
            get => this.InteractionModel.InteractionState;
            protected set => this.InteractionModel.InteractionState = value;
        }

        /// <summary>
        /// Gets or sets the user interface commands.
        /// </summary>
        protected TUserInterfaceCommands UserInterfaceCommands { get; set; } = new TUserInterfaceCommands();

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Virtual method that writes the interaction state manager streams to an exporter.
        /// </summary>
        /// <param name="prefix">The prefix to write the streams under.</param>
        /// <param name="exporter">The exporter to write the streams to.</param>
        public virtual void Write(string prefix, Exporter exporter)
            => this.InteractionStateOutput?.Write($"{prefix}.{nameof(this.InteractionState)}", exporter);

        /// <summary>
        /// Virtual method called upon pipeline completion.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The pipeline completed event arguments.</param>
        protected virtual void OnPipelineCompleted(object sender, PipelineCompletedEventArgs eventArgs)
            => this.PersistFinalState(eventArgs.CompletedOriginatingTime);

        /// <summary>
        /// Method called upon receiving the interface state.
        /// </summary>
        protected virtual void OnReceiveInterfaceState()
        {
        }

        /// <summary>
        /// Method called to persist the final state.
        /// </summary>
        /// <param name="originatingTime">The originating time.</param>
        protected virtual void PersistFinalState(DateTime originatingTime)
        {
        }

        private void ReceiveUserInterfaceState(TUserInterfaceState userInterfaceState, Envelope envelope)
        {
            this.InteractionModel.UserInterfaceState = userInterfaceState?.DeepClone();

            this.OnReceiveInterfaceState();

            var originatingTime = this.UserInterfaceCommandsOutput.Pipeline.GetCurrentTime();

            if (this.PersistentStateOutput.LastEnvelope == default)
            {
                this.PersistentStateOutput.Post(this.InteractionModel.PersistentState, originatingTime);
            }

            if (originatingTime - this.UserInterfaceCommandsOutput.LastEnvelope.OriginatingTime > this.minOutputTimeSpan)
            {
                this.InteractionModel.ComputeUserInterfaceCommands(this.UserInterfaceCommands);
                this.UserInterfaceCommandsOutput.Post(this.UserInterfaceCommands, originatingTime);
                this.InteractionStateOutput.Post(this.InteractionState, originatingTime);
            }
        }
    }
}
