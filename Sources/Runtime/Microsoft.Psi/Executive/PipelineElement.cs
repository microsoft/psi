// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Executive
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Scheduling;

    /// <summary>
    /// Class that encapsulates the execution context of a component (the state object, the sync object, the component wiring etc.)
    /// </summary>
    internal class PipelineElement
    {
#if DEBUG
        /// <summary>
        /// Slot for execution context local tracking of state object (receiver's owner or start/stop/final state object).
        /// </summary>
        private static readonly AsyncLocal<object> ExecutionContextStateObjectSlot = new AsyncLocal<object>();

        /// <summary>
        /// Slot for execution context local tracking of pipeline instance.
        /// </summary>
        private static readonly AsyncLocal<Pipeline> ExecutionContextPipelineSlot = new AsyncLocal<Pipeline>();
#endif

        private static readonly ConcurrentDictionary<object, SynchronizationLock> Locks = new ConcurrentDictionary<object, SynchronizationLock>();

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
            this.IsSource = stateObject is ISourceComponent;
            this.syncContext = Locks.GetOrAdd(stateObject, state => new SynchronizationLock(state));
        }

        private enum State
        {
            Initial,
            Active,
            Stopped,
            Finalized
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
        /// Gets a value indicating whether the component is stopped - meaning that it has been asked to cease producing non-reactive source messages (from timers, sensors, etc.)
        /// </summary>
        internal bool IsStopped => this.state == State.Stopped;

        /// <summary>
        /// Gets a value indicating whether the component is finalized - meaning that it should no longer be producing messages for any reason.
        /// </summary>
        internal bool IsFinalized => this.state == State.Finalized;

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

        /// <summary>
        /// Track state object in the execution context (in DEBUG only).
        /// </summary>
        /// <remarks>This allows checking that emitter post calls are from expected sources.</remarks>
        /// <param name="action">Action around which tracking will be instrumented.</param>
        /// <param name="owner">Owner/state object.</param>
        /// <param name="pipeline">Pipeline instance.</param>
        /// <returns>Action with tracking</returns>
        internal static Action TrackStateObjectOnContext(Action action, object owner, Pipeline pipeline)
        {
#if DEBUG
            return () =>
            {
                // save any previous tracked state in order to restore it after the action
                var (prevOwner, prevPipeline) = (ExecutionContextStateObjectSlot.Value, ExecutionContextPipelineSlot.Value);
                ExecutionContextStateObjectSlot.Value = owner;
                ExecutionContextPipelineSlot.Value = pipeline;
                action();
                ExecutionContextStateObjectSlot.Value = prevOwner;
                ExecutionContextPipelineSlot.Value = prevPipeline;
            };
#else
            return action; // no tracking
#endif
        }

        /// <summary>
        /// Track state object in the execution context.
        /// </summary>
        /// <remarks>This allows checking that emitter post calls are from expected sources.</remarks>
        /// <typeparam name="T">Type of action.</typeparam>
        /// <param name="action">Action around which tracking will be instrumented.</param>
        /// <param name="owner">Owner/state object.</param>
        /// <param name="pipeline">Pipeline instance.</param>
        /// <returns>Action with tracking</returns>
        internal static Action<T> TrackStateObjectOnContext<T>(Action<T> action, object owner, Pipeline pipeline)
        {
            return m =>
            {
                TrackStateObjectOnContext(() => action(m), owner, pipeline)();
            };
        }

#if DEBUG
        /// <summary>
        /// Check that the current state object being tracked on the execution context is the expected owner (exception for source components).
        /// </summary>
        /// <param name="owner">Owner/state object.</param>
        /// <param name="pipeline">Pipeline instance.</param>
        internal static void CheckStateObjectOnContext(object owner, Pipeline pipeline)
        {
            var trackedOwner = ExecutionContextStateObjectSlot.Value;
            if (trackedOwner == null)
            {
                // this component should be a source
                if (!(owner is ISourceComponent))
                {
                    throw new InvalidOperationException($"Emitter unexpectedly posted to from outside of a receiver (consider implementing {nameof(ISourceComponent)} if this is intentional - {owner}).");
                }
            }
            else
            {
                // this post is the result of a receiver (reactive component) - owners should match
                if (trackedOwner != owner)
                {
                    throw new InvalidOperationException($"Emitter of one component unexpectedly received post from a receiver of another component ({trackedOwner} -> {owner}).");
                }

                // pipelines should match with the single exception of Connector bridging pipelines
                var trackedPipeline = ExecutionContextPipelineSlot.Value;
                var typ = owner.GetType();
                if (trackedPipeline != pipeline && (!typ.IsGenericType || typ.GetGenericTypeDefinition() != typeof(Connector<>)))
                {
                    throw new InvalidOperationException($"Emitter created in one pipeline unexpectedly received post from a receiver in another pipeline (consider using a Connector to construct such bridges).");
                }
            }
        }
#endif

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

            Locks.TryRemove(this.stateObject, out var _);
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

            // tell the component it's being activated
            if (this.IsSource)
            {
                // start through the Scheduler to ensure exclusive execution of Start with respect to any receivers.
                this.pipeline.Scheduler.Schedule(
                    this.syncContext,
                    TrackStateObjectOnContext(() => ((ISourceComponent)this.stateObject).Start(this.OnNotifyCompletionTime), this.stateObject, this.pipeline),
                    replayContext.Start);
            }
        }

        /// <summary>
        /// Stops the entity - meaning that it should cease producing non-reactive source messages (from timers, sensors, etc.)
        /// </summary>
        internal void Stop()
        {
            if (this.state == State.Active || this.stateObject is Subpipeline)
            {
                this.state = State.Stopped;

                // tell the component it is being stopped, let any exception bubble up
                if (this.IsSource)
                {
                    // stop through the Scheduler to ensure exclusive execution of Stop with respect to any receivers.
                    this.pipeline.Scheduler.Schedule(
                        this.syncContext,
                        TrackStateObjectOnContext(((ISourceComponent)this.stateObject).Stop, this.stateObject, this.pipeline),
                        DateTime.MinValue);
                }
            }
            else
            {
                // This is an early bug avoidance measure. Outside of component infrastructure, nothing should call stop
                throw new InvalidOperationException($"Stop was called on component {this.Name}, which is not active (state={this.state}).");
            }
        }

        /// <summary>
        /// Finalize the entity - meaning that it may produce final messages now and then should no longer be producing messages for any reason.
        /// </summary>
        internal void Final()
        {
            this.state = State.Finalized;

            if (this.stateObject is Subpipeline subpipeline)
            {
                // finalize the subpipeline (its components have already been stopped)
                // stop through the Scheduler to ensure exclusive execution of Final with respect to any receivers.
                this.pipeline.Scheduler.Schedule(this.syncContext, TrackStateObjectOnContext(subpipeline.Final, this.stateObject, this.pipeline), DateTime.MinValue);
            }
        }

        internal void OnNotifyCompletionTime(DateTime finalOriginatingTime)
        {
            this.pipeline.NotifyCompletionTime(this, finalOriginatingTime);
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