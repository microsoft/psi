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
        private object stateObject;
        private Dictionary<string, IEmitter> outputs = new Dictionary<string, IEmitter>();
        private Dictionary<string, IReceiver> inputs = new Dictionary<string, IReceiver>();
        private SynchronizationLock syncContext;
        private int activationCount;
        private string name;
        private Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineElement"/> class.
        /// </summary>
        /// <param name="name">The name of the instance.</param>
        /// <param name="stateObject">The state object wrapped by this node.</param>
        public PipelineElement(string name, object stateObject)
        {
            this.name = name;
            this.stateObject = stateObject;
            this.IsStartable = this.stateObject is IStartable;
            this.syncContext = new SynchronizationLock(this.stateObject);
        }

        /// <summary>
        /// Gets the name of the entity
        /// </summary>
        public string Name => this.name;

        public SynchronizationLock SyncContext => this.syncContext;

        /// <summary>
        /// Gets a value indicating whether the component is active.
        /// </summary>
        internal bool IsActive => this.activationCount > 0;

        /// <summary>
        /// Gets a value indicating whether the entity is startable.
        /// Generally this means it produces messages on its own thread rather than in response to other messages.
        /// </summary>
        internal bool IsStartable { get; private set; }

        internal bool IsInitialized { get; private set; }

        internal object StateObject => this.stateObject;

        internal Dictionary<string, IEmitter> Outputs => this.outputs;

        internal Dictionary<string, IReceiver> Inputs => this.inputs;

        internal IEnumerable<string> InputNames => this.inputs.Keys;

        /// <summary>
        /// Delayed initialization of the state object. Note that we don't have a Scheduler yet.
        /// </summary>
        /// <param name="pipeline">The parent pipeline</param>
        internal void Initialize(Pipeline pipeline)
        {
            this.pipeline = pipeline;
            if (this.activationCount != 0)
            {
                throw new InvalidOperationException();
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
        internal void Activate(ReplayDescriptor replayContext)
        {
            if (this.activationCount == 0)
            {
                // tell the component it's being activated
                if (this.IsStartable)
                {
                    // activate through the Scheduler to ensure exclusive execution of Start with respect to any receivers.
                    this.pipeline.Scheduler.Schedule(
                        this.syncContext,
                        () => ((IStartable)this.stateObject).Start(this.OnCompleted, replayContext),
                        replayContext.Start);
                }
            }

            this.activationCount++;
        }

        /// <summary>
        /// Deactivates the entity.
        /// </summary>
        internal void Deactivate()
        {
            if (this.activationCount == 0)
            {
                // This is an early bug avoidance measure. Outside of component infrastructure, nothing should call deactivate
                throw new InvalidOperationException(string.Format("Deactivate was called on component {0}, which is not active.", this.Name));
            }

            this.activationCount--;
            if (this.activationCount == 0)
            {
                // tell the component it is being deactivated, let any exception bubble up
                if (this.stateObject is IStartable)
                {
                    // deactivate through the Scheduler to ensure exclusive execution of Stop with respect to any receivers.
                    this.pipeline.Scheduler.Schedule(
                        this.syncContext,
                        ((IStartable)this.stateObject).Stop,
                        this.pipeline.GetCurrentTime());
                }
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