// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Executive
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Scheduling;

    /// <summary>
    /// Class that encapsulates the execution context of a component (the state object, the sync object, the component wiring etc.)
    /// </summary>
    internal class PipelineElement
    {
        private readonly bool isFiniteSource;
        private object stateObject;
        private Dictionary<string, IEmitter> outputs = new Dictionary<string, IEmitter>();
        private Dictionary<string, IReceiver> inputs = new Dictionary<string, IReceiver>();
        private SynchronizationLock syncContext;
        private string name;
        private Pipeline pipeline;
        private State state = State.Initial;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineElement"/> class.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <param name="stateObject">The state object wrapped by this node.</param>
        public PipelineElement(string name, object stateObject)
        {
            this.name = name;
            this.stateObject = stateObject;
            this.isFiniteSource = stateObject is IFiniteSourceComponent;
            this.IsSource = this.isFiniteSource || this.stateObject is ISourceComponent;
            this.syncContext = new SynchronizationLock(this.stateObject);
        }

        private enum State
        {
            Initial,
            Active,
            Deactivated,
            Stopped
        }

        /// <summary>
        /// Gets the name of the entity
        /// </summary>
        public string Name => this.name;

        public SynchronizationLock SyncContext => this.syncContext;

        /// <summary>
        /// Gets a value indicating whether the component is active.
        /// </summary>
        internal bool IsActive => this.state == State.Active;

        /// <summary>
        /// Gets a value indicating whether the component is deactivated.
        /// </summary>
        internal bool IsDeactivated => this.state == State.Deactivated;

        /// <summary>
        /// Gets a value indicating whether the entity is a source component
        /// Generally this means it produces messages on its own thread rather than in response to other messages.
        /// </summary>
        internal bool IsSource { get; private set; }

        internal bool IsInitialized { get; private set; }

        internal object StateObject => this.stateObject;

        internal Dictionary<string, IEmitter> Outputs => this.outputs;

        internal Dictionary<string, IReceiver> Inputs => this.inputs;

        internal IEnumerable<string> InputNames => this.inputs.Keys;

        internal Action OnStartHandler { get; set; }

        internal Action OnStopHandler { get; set; }

        internal Action OnFinalHandler { get; set; }

        /// <summary>
        /// Delayed initialization of the state object. Note that we don't have a Scheduler yet.
        /// </summary>
        /// <param name="pipeline">The parent pipeline</param>
        internal void Initialize(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            if (this.state != State.Initial)
            {
                throw new InvalidOperationException($"Initialize was called on component {this.Name}, which is has already started (state={this.state}).");
            }
        }

        /// <summary>
        /// Disposes the state object and turns off the receivers.
        /// </summary>
        internal void Dispose()
        {
            // disable the inputs
            foreach (var input in this.Inputs.Values)
            {
                input.Dispose();
            }

            if (this.stateObject is IDisposable)
            {
                ((IDisposable)this.stateObject).Dispose();
            }

            this.stateObject = null;
        }

        /// <summary>
        /// Activates the entity.
        /// </summary>
        /// <param name="replayContext">If the pipeline is in replay mode, this is set and provides replay information</param>
        internal void Start(ReplayDescriptor replayContext)
        {
            if (this.state != State.Initial)
            {
                throw new InvalidOperationException($"Start was called on component {this.Name}, which is has already started (state={this.state}).");
            }

            this.state = State.Active;

            if (this.isFiniteSource)
            {
                ((IFiniteSourceComponent)this.stateObject).Initialize(this.OnCompleted);
            }

            // tell the component it's being activated
            if (this.OnStartHandler != null)
            {
                // start through the Scheduler to ensure exclusive execution of Start with respect to any receivers.
                this.pipeline.Scheduler.Schedule(
                    this.syncContext,
                    this.OnStartHandler,
                    replayContext.Start);
            }
        }

        /// <summary>
        /// Deactivates the entity.
        /// </summary>
        internal void Deactivate()
        {
            if (this.state == State.Active || this.stateObject is Subpipeline)
            {
                this.state = State.Deactivated;

                if (this.OnStopHandler != null)
                {
                    // tell the component it is being deactivated, let any exception bubble up
                    // stop through the Scheduler to ensure exclusive execution of Stop with respect to any receivers.
                    this.pipeline.Scheduler.Schedule(this.syncContext, this.OnStopHandler, this.pipeline.GetCurrentTime());
                }
            }
            else
            {
                // This is an early bug avoidance measure. Outside of component infrastructure, nothing should call deactivate
                throw new InvalidOperationException($"Deactivate was called on component {this.Name}, which is not active (state={this.state}).");
            }
        }

        /// <summary>
        /// Stop the entity.
        /// </summary>
        internal void Stop()
        {
            this.state = State.Stopped;

            if (this.OnFinalHandler != null)
            {
                // tell the component it is being deactivated, let any exception bubble up
                // stop through the Scheduler to ensure exclusive execution of Stop with respect to any receivers.
                this.pipeline.Scheduler.Schedule(this.syncContext, this.OnFinalHandler, this.pipeline.GetCurrentTime());
            }
        }

        internal void OnCompleted()
        {
            this.pipeline.NotifyCompleted(this);
        }

        internal IEmitter GetOutput(string name)
        {
            return this.outputs[name];
        }

        internal IReceiver GetInput(string name)
        {
            return this.inputs[name];
        }

        internal void AddOutput(string name, IEmitter output)
        {
            name = name ?? $"{this.Name}<{output.GetType().GetGenericArguments()[0].Name}> {output.GetHashCode()}";
            this.outputs.Add(name, output);
        }

        internal void AddInput(string name, IReceiver input)
        {
            name = name ?? $"{this.Name}[{input.GetType().GetGenericArguments()[0].Name}] {input.GetHashCode()}";
            this.inputs.Add(name, input);
        }
    }
}