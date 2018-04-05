// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi.Executive;
    using Microsoft.Psi.Scheduling;

    /// <summary>
    /// Represents a graph of components and controls scheduling and message passing.
    /// </summary>
    public class Pipeline : IDisposable
    {
        private static int lastStreamId = 0;
        private static int nextElementId;

        private readonly string name;

        /// <summary>
        /// This event becomes set once the pipeline is done
        /// </summary>
        private readonly ManualResetEvent completed = new ManualResetEvent(false);

        /// <summary>
        /// This event becomes set when the first startable component is done
        /// </summary>
        private readonly ManualResetEvent anyCompleted = new ManualResetEvent(false);

        private readonly KeyValueStore configStore = new KeyValueStore();

        private readonly DeliveryPolicy globalPolicy;

        /// <summary>
        /// If set, indicates that the pipeline is in replay mode
        /// </summary>
        private ReplayDescriptor replayDescriptor;

        private TimeInterval proposedTimeInterval;

        private TimeInterval proposedOriginatingTimeInterval;

        // the pipeline time, which might or might not be the same as real time (is shifted when in replay mode, and can be slowed down)
        private Clock clock;

        /// <summary>
        /// The wiring of components
        /// </summary>
        private ConcurrentQueue<PipelineElement> components = new ConcurrentQueue<PipelineElement>();

        /// <summary>
        /// The startable wiring components
        /// </summary>
        private List<PipelineElement> startableComponents = new List<PipelineElement>();

        private Scheduler scheduler;

        private List<Exception> errors = new List<Exception>();

        // true while stopping
        private bool stopping;

        private bool enableExceptionHandling;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pipeline"/> class.
        /// </summary>
        /// <param name="name">Pipeline name.</param>
        /// <param name="globalPolicy">Global delivery policy.</param>
        /// <param name="threadCount">Number of threads.</param>
        /// <param name="allowSchedulingOnExternalThreads">Whether to allow scheduling on external threads.</param>
        public Pipeline(string name, DeliveryPolicy globalPolicy, int threadCount, bool allowSchedulingOnExternalThreads)
        {
            this.name = name ?? "default";
            this.globalPolicy = globalPolicy ?? DeliveryPolicy.Unlimited;
            this.enableExceptionHandling = false;
            this.scheduler = new Scheduler(this.globalPolicy, this.ErrorHandler, threadCount, allowSchedulingOnExternalThreads, name);
        }

        /// <summary>
        /// Event fired upon pipeline completion.
        /// </summary>
        public event EventHandler<PipelineCompletionEventArgs> PipelineCompletionEvent;

        /// <summary>
        /// Event fired upon component completion.
        /// </summary>
        public event EventHandler<string> ComponentCompletionEvent;

        /// <summary>
        /// Gets pipeline name.
        /// </summary>
        public string Name => this.name;

        /// <summary>
        /// Gets replay descriptor.
        /// </summary>
        public ReplayDescriptor ReplayDescriptor => this.replayDescriptor;

        /// <summary>
        /// Gets global delivery policy.
        /// </summary>
        public DeliveryPolicy GlobalPolicy => this.globalPolicy;

        internal Scheduler Scheduler => this.scheduler;

        internal Clock Clock => this.clock;

        internal KeyValueStore ConfigurationStore => this.configStore;

        internal ConcurrentQueue<PipelineElement> Components => this.components;

        /// <summary>
        /// Create pipeline.
        /// </summary>
        /// <param name="name">Pipeline name.</param>
        /// <param name="globalPolicy">Global delivery policy.</param>
        /// <param name="threadCount">Number of threads.</param>
        /// <param name="allowSchedulingOnExternalThreads">Whether to allow scheduling on external threads.</param>
        /// <returns>Created pipeline.</returns>
        public static Pipeline Create(string name = null, DeliveryPolicy globalPolicy = null, int threadCount = 0, bool allowSchedulingOnExternalThreads = false)
        {
            return new Pipeline(name, globalPolicy, threadCount, allowSchedulingOnExternalThreads);
        }

        /// <summary>
        /// Add component to pipeline.
        /// </summary>
        /// <param name="name">Component name.</param>
        /// <param name="stateObject">Initial state object.</param>
        public void AddComponent(string name, object stateObject)
        {
            this.AddComponent(new PipelineElement(name, stateObject));
        }

        /// <summary>
        /// Propose replay time.
        /// </summary>
        /// <param name="activeInterval">Active time interval.</param>
        /// <param name="originatingTimeInterval">Originating time interval.</param>
        public void ProposeReplayTime(TimeInterval activeInterval, TimeInterval originatingTimeInterval)
        {
            if (!activeInterval.LeftEndpoint.Bounded)
            {
                throw new ArgumentException(nameof(activeInterval), "Replay time intervals must have a valid start time.");
            }

            if (!originatingTimeInterval.LeftEndpoint.Bounded)
            {
                throw new ArgumentException(nameof(originatingTimeInterval), "Replay time intervals must have a valid start time.");
            }

            this.proposedTimeInterval = (this.proposedTimeInterval == null) ? activeInterval : TimeInterval.Coverage(new[] { this.proposedTimeInterval, activeInterval });
            this.proposedOriginatingTimeInterval = (this.proposedOriginatingTimeInterval == null) ? originatingTimeInterval : TimeInterval.Coverage(new[] { this.proposedOriginatingTimeInterval, originatingTimeInterval });
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle)</param>
        /// <returns>A new receiver</returns>
        public Receiver<T> CreateReceiver<T>(object owner, Action<T, Envelope> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data, m.Envelope), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle)</param>
        /// <returns>A new receiver</returns>
        public Receiver<T> CreateReceiver<T>(object owner, Action<T> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object.
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle)</param>
        /// <returns>A new receiver</returns>
        public Receiver<T> CreateReceiver<T>(object owner, Action<Message<T>> action, string name, bool autoClone = false)
        {
            PipelineElement node = this.GetOrCreateNode(owner);
            var receiver = new Receiver<T>(owner, action, node.SyncContext, this, autoClone);
            node.AddInput(name, receiver);
            return receiver;
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object, connected to an async message processing function.
        /// The expected signature of the message processing delegate is: <code>async void Receive(<typeparamref name="T"/> message, Envelope env);</code>
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle)</param>
        /// <returns>A new receiver</returns>
        public Receiver<T> CreateAsyncReceiver<T>(object owner, Func<T, Envelope, Task> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data, m.Envelope).Wait(), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object, connected to an async message processing function.
        /// The expected signature of the message processing delegate is: <code>async void Receive(<typeparamref name="T"/> message);</code>
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle)</param>
        /// <returns>A new receiver</returns>
        public Receiver<T> CreateAsyncReceiver<T>(object owner, Func<T, Task> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m.Data).Wait(), name, autoClone);
        }

        /// <summary>
        /// Creates an input receiver associated with the specified component object, connected to an async message processing function.
        /// The expected signature of the message processing delegate is: <code>async void Receive(Message{<typeparamref name="T"/>} message);</code>
        /// </summary>
        /// <typeparam name="T">The type of messages accepted by this receiver</typeparam>
        /// <param name="owner">The component that owns the receiver. This is usually the state object that the receiver operates on.
        /// The receivers associated with the same owner are never executed concurrently.</param>
        /// <param name="action">The action to execute when a message is delivered to this receiver.</param>
        /// <param name="name">The debug name of the receiver</param>
        /// <param name="autoClone">If true, the receiver will clone the message before passing it to the action, which is then responsible for recycling it as needed (using receiver.Recycle)</param>
        /// <returns>A new receiver</returns>
        public Receiver<T> CreateAsyncReceiver<T>(object owner, Func<Message<T>, Task> action, string name, bool autoClone = false)
        {
            return this.CreateReceiver<T>(owner, m => action(m).Wait(), name, autoClone);
        }

        /// <summary>
        /// Create emitter.
        /// </summary>
        /// <typeparam name="T">Type of emitted messages.</typeparam>
        /// <param name="owner">Owner of emitter.</param>
        /// <param name="name">Name of emitter.</param>
        /// <returns>Created emitter.</returns>
        public Emitter<T> CreateEmitter<T>(object owner, string name)
        {
            PipelineElement node = this.GetOrCreateNode(owner);
            var emitter = new Emitter<T>(Interlocked.Increment(ref lastStreamId), owner, node.SyncContext, this);
            node.AddOutput(name, emitter);
            return emitter;
        }

        /// <summary>
        /// Wait for all components to complete.
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout (milliseconds).</param>
        /// <returns>Success.</returns>
        public bool WaitAll(int millisecondsTimeout = Timeout.Infinite)
        {
            bool result = this.completed.WaitOne(millisecondsTimeout);
            this.ThrowIfError();
            return result;
        }

        /// <summary>
        /// Wait for any component to complete.
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout (milliseconds).</param>
        /// <returns>Success.</returns>
        public bool WaitAny(int millisecondsTimeout = Timeout.Infinite)
        {
            bool result = this.anyCompleted.WaitOne(millisecondsTimeout);
            this.ThrowIfError();
            return result;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="descriptor">Replay descriptor.</param>
        /// <param name="enableExceptionHandling">Whether to enable exception handling.</param>
        public void Run(ReplayDescriptor descriptor, bool enableExceptionHandling = false)
        {
            this.enableExceptionHandling = enableExceptionHandling;
            this.RunAsync(descriptor);
            this.WaitAll();
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="replayInterval">Time interval within which to replay.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <param name="enableExceptionHandling">Whether to enable exception handling.</param>
        public void Run(TimeInterval replayInterval = null, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1, bool enableExceptionHandling = false)
        {
            this.Run(new ReplayDescriptor(replayInterval, useOriginatingTime, enforceReplayClock, replaySpeedFactor), enableExceptionHandling);
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <param name="enableExceptionHandling">Whether to enable exception handling.</param>
        public void Run(DateTime replayStartTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1, bool enableExceptionHandling = false)
        {
            this.Run(new ReplayDescriptor(replayStartTime, DateTime.MaxValue, useOriginatingTime, enforceReplayClock, replaySpeedFactor), enableExceptionHandling);
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="replayEndTime">Time at which to end replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <param name="enableExceptionHandling">Whether to enable exception handling.</param>
        public void Run(DateTime replayStartTime, DateTime replayEndTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1, bool enableExceptionHandling = false)
        {
            this.Run(new ReplayDescriptor(replayStartTime, replayEndTime, useOriginatingTime, enforceReplayClock, replaySpeedFactor), enableExceptionHandling);
        }

        /// <summary>
        /// Run pipeline (synchronously).
        /// </summary>
        /// <param name="duration">Duration (time span) to replay.</param>
        /// <param name="enableExceptionHandling">Whether to enable exception handling.</param>
        public void Run(TimeSpan duration, bool enableExceptionHandling = false)
        {
            this.enableExceptionHandling = enableExceptionHandling;
            this.RunAsync();
            if (!this.WaitAll((int)duration.TotalMilliseconds))
            {
                this.Stop();
            }
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="replayInterval">Time interval within which to replay.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public IDisposable RunAsync(TimeInterval replayInterval = null, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            return this.RunAsync(new ReplayDescriptor(replayInterval, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public IDisposable RunAsync(DateTime replayStartTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            return this.RunAsync(new ReplayDescriptor(replayStartTime, DateTime.MaxValue, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="replayStartTime">Time at which to start replaying.</param>
        /// <param name="replayEndTime">Time at which to end replaying.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor at which to replay (e.g. 2 for double speed, 0.5 for half speed).</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public IDisposable RunAsync(DateTime replayStartTime, DateTime replayEndTime, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            return this.RunAsync(new ReplayDescriptor(replayStartTime, replayEndTime, useOriginatingTime, enforceReplayClock, replaySpeedFactor));
        }

        /// <summary>
        /// Run pipeline (asynchronously).
        /// </summary>
        /// <param name="descriptor">Replay descriptor.</param>
        /// <returns>Disposable used to terminate pipeline.</returns>
        public IDisposable RunAsync(ReplayDescriptor descriptor)
        {
            descriptor = descriptor ?? ReplayDescriptor.ReplayAll;
            this.replayDescriptor = descriptor.Intersect(descriptor.UseOriginatingTime ? this.proposedOriginatingTimeInterval : this.proposedTimeInterval);

            bool hasExplicitStart = this.replayDescriptor.Interval.Left != DateTime.MinValue;
            this.clock = hasExplicitStart ? new Clock(this.replayDescriptor.Start, 1 / this.replayDescriptor.ReplaySpeedFactor) : new Clock(default(TimeSpan), 1 / this.replayDescriptor.ReplaySpeedFactor);
            this.completed.Reset();
            this.scheduler.Start(this.clock, descriptor.EnforceReplayClock);

            // keep track of startable components
            foreach (var component in this.components)
            {
                if (component.IsStartable)
                {
                    lock (this.startableComponents)
                    {
                        this.startableComponents.Add(component);
                    }
                }
            }

            foreach (var component in this.components)
            {
                component.Activate(this.replayDescriptor);
            }

            return this;
        }

        /// <summary>
        /// Get current clock time.
        /// </summary>
        /// <returns>Current clock time.</returns>
        public DateTime GetCurrentTime()
        {
            return this.clock.GetCurrentTime();
        }

        /// <summary>
        /// Get current time, given elapsed ticks.
        /// </summary>
        /// <param name="ticksFromSystemBoot">Ticks elapsed since system boot.</param>
        /// <returns>Current time.</returns>
        public DateTime GetCurrentTimeFromElapsedTicks(long ticksFromSystemBoot)
        {
            return this.clock.GetTimeFromElapsedTicks(ticksFromSystemBoot);
        }

        /// <summary>
        /// Convert virtual duration to real time.
        /// </summary>
        /// <param name="duration">Duration to convert.</param>
        /// <returns>Converted time span.</returns>
        public TimeSpan ConvertToRealTime(TimeSpan duration)
        {
            return this.clock.ToRealTime(duration);
        }

        /// <summary>
        /// Convert virtual datetime to real time.
        /// </summary>
        /// <param name="time">Datetime to convert.</param>
        /// <returns>Converted datetime.</returns>
        public DateTime ConvertToRealTime(DateTime time)
        {
            return this.clock.ToRealTime(time);
        }

        /// <summary>
        /// Convert real timespan to virtual.
        /// </summary>
        /// <param name="duration">Duration to convert.</param>
        /// <returns>Converted time span.</returns>
        public TimeSpan ConvertFromRealTime(TimeSpan duration)
        {
            return this.clock.ToVirtualTime(duration);
        }

        /// <summary>
        /// Contert real datetime to virtual.
        /// </summary>
        /// <param name="time">Datetime to convert.</param>
        /// <returns>Converted datetime.</returns>
        public DateTime ConvertFromRealTime(DateTime time)
        {
            return this.clock.ToVirtualTime(time);
        }

        internal Emitter<T> CreateEmitterWithFixedStreamId<T>(object owner, string name, int streamId)
        {
            PipelineElement node = this.GetOrCreateNode(owner);
            var emitter = new Emitter<T>(streamId, owner, node.SyncContext, this);
            node.AddOutput(name, emitter);
            return emitter;
        }

        /// <summary>
        /// Stops the pipeline and removes all connectivity (pipes).
        /// </summary>
        /// <param name="abandonPendingWorkItems">Abandons pending work items.</param>
        internal void Dispose(bool abandonPendingWorkItems)
        {
            if (this.scheduler == null)
            {
                // we never started or we've been already disposed
                return;
            }

            this.Stop(abandonPendingWorkItems);
            this.DisposeComponents();
            this.scheduler = null;
            this.components = null;
            this.ThrowIfError();
        }

        internal void AddComponent(PipelineElement pe)
        {
            pe.Initialize(this);
            this.components.Enqueue(pe);
        }

        internal void NotifyCompleted(PipelineElement component)
        {
            if (component.IsStartable)
            {
                bool lastRemainingStartable = false;
                lock (this.startableComponents)
                {
                    if (!this.startableComponents.Contains(component))
                    {
                        // the component was already removed (e.g. because the pipeline is stopping)
                        return;
                    }

                    this.startableComponents.Remove(component);
                    lastRemainingStartable = this.startableComponents.Count == 0;
                }

                this.anyCompleted.Set();
                this.ComponentCompletionEvent?.Invoke(this, component.Name);

                if (lastRemainingStartable)
                {
                    // stop once all IStartableComponents have stopped
                    ThreadPool.QueueUserWorkItem(_ => this.Stop());
                }
            }
        }

        private PipelineElement GetOrCreateNode(object component)
        {
            PipelineElement node = this.components.FirstOrDefault(c => c.StateObject == component);
            if (node == null)
            {
                var id = nextElementId++;
                var name = component.GetType().Name;
                var fullName = $"{id}.{name}";
                node = new PipelineElement(fullName, component);
                this.AddComponent(node);
            }

            return node;
        }

        /// <summary>
        /// Stops the pipeline by disabling message passing between the pipeline components.
        /// The pipeline configuration is not changed and the pipeline can be restarted later.
        /// </summary>
        /// <param name="abandonPendingWorkitems">Abandons the pending work items</param>
        private void Stop(bool abandonPendingWorkitems = false)
        {
            if (this.stopping)
            {
                this.completed.WaitOne();
                return;
            }

            this.stopping = true;

            // stop all startable components, to disable the streaming of new messages
            this.DeactivateComponents();

            // block until all messages in the pipeline are fully processed
            this.scheduler.Stop(abandonPendingWorkitems);

            this.completed.Set();
            if (this.PipelineCompletionEvent != null)
            {
                // this.clock might be null if RunAsync was never called.
                if (this.clock != null)
                {
                    this.PipelineCompletionEvent(this, new PipelineCompletionEventArgs(this.clock.GetCurrentTime(), abandonPendingWorkitems, this.errors));
                }

                this.errors.Clear();
            }

            this.clock = null;
        }

        private void DeactivateComponents()
        {
            // to avoid deadlocks resulting from component calls to NotifyCompleted, copy and empty the list before calling each startable component
            PipelineElement[] startedComponents;
            lock (this.startableComponents)
            {
                startedComponents = this.startableComponents.ToArray();
                this.startableComponents.Clear();
            }

            foreach (var component in startedComponents)
            {
                if (component.IsActive)
                {
                    component.Deactivate();
                }
            }
        }

        private void DisposeComponents()
        {
            foreach (var component in this.components)
            {
                component.Dispose();
            }
        }

        private bool ErrorHandler(Exception e)
        {
            lock (this.errors)
            {
                this.errors.Add(e);
                if (!this.stopping)
                {
                    ThreadPool.QueueUserWorkItem(_ => this.Stop());
                }
            }

            return this.enableExceptionHandling || this.PipelineCompletionEvent != null; // let the exception bubble up
        }

        private void ThrowIfError()
        {
            lock (this.errors)
            {
                if (this.errors.Count > 0)
                {
                    var error = new AggregateException($"Pipeline '{this.name}' was terminated because of one or more unexpected errors", this.errors);
                    this.errors.Clear();
                    throw error;
                }
            }
        }
    }
}
