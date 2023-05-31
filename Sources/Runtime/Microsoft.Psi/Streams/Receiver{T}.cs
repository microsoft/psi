// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Executive;
    using Microsoft.Psi.Scheduling;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Streams;

    /// <summary>
    /// A receiver that calls the wrapped delegate to deliver messages by reference (hence, unsafe).
    /// The wrapped delegate must not modify or store the message or any part of the message.
    /// </summary>
    /// <remarks>
    /// The Receiver class uses the Scheduler to deliver messages.
    /// However, the workitem unit scheduled by the Receiver is the whole receiver queue, not a single message.
    /// In other words, the Receiver simply schedules itself, and there will be only one workitem present in the scheduler queue for any given Receiver.
    /// This guarantees message delivery order regardless of the kind of scheduling used by the scheduler.
    /// </remarks>
    /// <typeparam name="T">The type of messages that can be received.</typeparam>
    [Serializer(typeof(Receiver<>.NonSerializer))]
    public sealed class Receiver<T> : IReceiver, IConsumer<T>
    {
        private readonly Action<Message<T>> onReceived;
        private readonly PipelineElement element;
        private readonly Pipeline pipeline;
        private readonly Scheduler scheduler;
        private readonly SchedulerContext schedulerContext;
        private readonly SynchronizationLock syncContext;
        private readonly bool enforceIsolation;
        private readonly List<UnsubscribedHandler> unsubscribedHandlers = new List<UnsubscribedHandler>();
        private readonly Lazy<DiagnosticsCollector.ReceiverCollector> receiverDiagnosticsCollector;

        private Envelope lastEnvelope;
        private DeliveryQueue<T> awaitingDelivery;
        private IPerfCounterCollection<ReceiverCounters> counters;
        private Func<T, int> computeDataSize = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Receiver{T}"/> class.
        /// </summary>
        /// <param name="id">The unique receiver id.</param>
        /// <param name="name">The debug name of the receiver.</param>
        /// <param name="element">The pipeline element associated with the receiver.</param>
        /// <param name="owner">The component that owns this receiver.</param>
        /// <param name="onReceived">The action to execute when a message is delivered to the receiver.</param>
        /// <param name="context">The synchronization context of the receiver.</param>
        /// <param name="pipeline">The pipeline in which to create the receiver.</param>
        /// <param name="enforceIsolation">A value indicating whether to enforce cloning of messages as they arrive at the receiver.</param>
        /// <remarks>
        /// The <paramref name="enforceIsolation"/> flag primarily affects synchronous delivery of messages, when the action is
        /// executed on the same thread on which the message was posted. If this value is set to true, the runtime will enforce
        /// isolation by cloning the message before passing it to the receiver action. If set to false, then the message will be
        /// passed by reference to the action without cloning, if and only if the receiver action executes synchronously. This
        /// should be used with caution, as any modifications that the action may make to the received message will be reflected
        /// in the source message posted by the upstream component. When in doubt, keep this value set to true to ensure that
        /// messages are always cloned. Regardless of the value set here, isolation is always enforced when messages are queued
        /// and delivered asynchronously.
        /// </remarks>
        internal Receiver(int id, string name, PipelineElement element, object owner, Action<Message<T>> onReceived, SynchronizationLock context, Pipeline pipeline, bool enforceIsolation = true)
        {
            this.scheduler = pipeline.Scheduler;
            this.schedulerContext = pipeline.SchedulerContext;
            this.lastEnvelope = default;
            this.onReceived = m =>
            {
                this.lastEnvelope = m.Envelope;
                PipelineElement.TrackStateObjectOnContext(onReceived, owner, pipeline)(m);
            };
            this.Id = id;
            this.Name = name;
            this.element = element;
            this.Owner = owner;
            this.syncContext = context;
            this.enforceIsolation = enforceIsolation;
            this.Recycler = RecyclingPool.Create<T>();
            this.pipeline = pipeline;
            this.receiverDiagnosticsCollector = new Lazy<DiagnosticsCollector.ReceiverCollector>(() => this.pipeline.DiagnosticsCollector?.GetReceiverDiagnosticsCollector(pipeline, element, this), true);
        }

        /// <summary>
        /// Receiver unsubscribed handler.
        /// </summary>
        /// <param name="finalOriginatingTime">Originating time of final message posted.</param>
        public delegate void UnsubscribedHandler(DateTime finalOriginatingTime);

        /// <summary>
        /// Event invoked after this receiver is unsubscribed from its source emitter.
        /// </summary>
        public event UnsubscribedHandler Unsubscribed
        {
            add
            {
                this.unsubscribedHandlers.Add(value);
            }

            remove
            {
                this.unsubscribedHandlers.Remove(value);
            }
        }

        /// <inheritdoc />
        IEmitter IReceiver.Source => this.Source;

        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public Type Type => typeof(T);

        /// <inheritdoc />
        public object Owner { get; }

        /// <summary>
        /// Gets the delivery policy for this receiver.
        /// </summary>
        public DeliveryPolicy<T> DeliveryPolicy { get; private set; }

        /// <summary>
        /// Gets receiver message recycler.
        /// </summary>
        public IRecyclingPool<T> Recycler { get; }

        /// <summary>
        /// Gets the envelope of the last message delivered.
        /// </summary>
        public Envelope LastEnvelope => this.lastEnvelope;

        /// <inheritdoc />
        Receiver<T> IConsumer<T>.In => this;

        internal Emitter<T> Source { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.counters?.Clear();
        }

        /// <summary>
        /// Recycle message.
        /// </summary>
        /// <param name="freeMessage">Message to recycle.</param>
        public void Recycle(Message<T> freeMessage)
        {
            this.Recycle(freeMessage.Data);
        }

        /// <summary>
        /// Recycle item.
        /// </summary>
        /// <param name="item">Item to recycle.</param>
        public void Recycle(T item)
        {
            this.Recycler.Recycle(item);
        }

        /// <summary>
        /// Enable performance counters.
        /// </summary>
        /// <param name="name">Instance name.</param>
        /// <param name="perf">Performance counters implementation (platform specific).</param>
        public void EnablePerfCounters(string name, IPerfCounters<ReceiverCounters> perf)
        {
            const string Category = "Microsoft Psi message delivery";

            if (this.counters != null)
            {
                throw new InvalidOperationException("Perf counters are already enabled for this receiver");
            }

#pragma warning disable SA1118 // Parameter must not span multiple lines
            perf.AddCounterDefinitions(
                Category,
                new Tuple<ReceiverCounters, string, string, PerfCounterType>[]
                {
                    Tuple.Create(ReceiverCounters.Total, "Total messages / second", "Number of messages received per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(ReceiverCounters.Dropped, "Dropped messages / second", "Number of messages dropped per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(ReceiverCounters.Processed, "Messages / second", "Number of messages processed per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(ReceiverCounters.ProcessingTime, "Processing time (ns)", "The time it takes the component to process a message", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.PipelineExclusiveDelay, "Exclusive pipeline delay (ns)", "The delta between the originating time of the message and the time the message was received.", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.IngestTime, "Ingest time (ns)", "The delta between the time the message was posted and the time the message was received.", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.TimeInQueue, "Time in queue (ns)", "The time elapsed between posting of the message and beginning its processing", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.ProcessingDelay, "Total processing delay (ns)", "The time elapsed between posting of the message and completing its processing.", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.PipelineInclusiveDelay, "Inclusive pipeline delay (ns)", "The end-to-end delay, from originating time to the time when processing completed.", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.QueueSize, "Queue size", "The number of messages waiting in the delivery queue", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.MaxQueueSize, "Max queue size", "The maximum number of messages ever waiting at the same time in the delivery queue", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.ThrottlingRequests, "Throttling requests / second", "The number of throttling requests issued due to queue full, per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(ReceiverCounters.OutstandingUnrecycled, "Unrecycled messages", "The number of messages that are still in use by the component", PerfCounterType.NumberOfItems32),
                    Tuple.Create(ReceiverCounters.AvailableRecycled, "Recycled messages", "The number of messages that are available for recycling", PerfCounterType.NumberOfItems32),
                });
#pragma warning restore SA1118 // Parameter must not span multiple lines

            this.counters = perf.Enable(Category, name);
            this.awaitingDelivery.EnablePerfCounters(this.counters);
        }

        internal void OnSubscribe(Emitter<T> source, bool allowSubscribeWhileRunning, DeliveryPolicy<T> policy)
        {
            if (this.Source != null)
            {
                throw new InvalidOperationException("This receiver is already connected to a source emitter.");
            }

            if (!allowSubscribeWhileRunning && (this.pipeline.IsRunning || source.Pipeline.IsRunning))
            {
                throw new InvalidOperationException("Attempting to connect a receiver to an emitter while pipeline is already running. Make all connections before running the pipeline.");
            }

            if (source.Pipeline != this.pipeline)
            {
                throw new InvalidOperationException("Receiver cannot subscribe to an emitter from a different pipeline. Use a Connector if you need to connect emitters and receivers from different pipelines.");
            }

            this.Source = source;
            this.DeliveryPolicy = policy;
            this.awaitingDelivery = new DeliveryQueue<T>(policy, this.Recycler);
            this.pipeline.DiagnosticsCollector?.PipelineElementReceiverSubscribe(this.pipeline, this.element, this, source, this.DeliveryPolicy.Name);
        }

        internal void OnUnsubscribe()
        {
            if (this.Source != null)
            {
                this.Source = null;
                this.OnUnsubscribed(this.pipeline.GetCurrentTime());
            }
        }

        internal void Receive(Message<T> message)
        {
            var hasDiagnosticsCollector = this.receiverDiagnosticsCollector.Value != null;
            var diagnosticsTime = DateTime.MinValue;

            if (hasDiagnosticsCollector)
            {
                diagnosticsTime = this.pipeline.GetCurrentTime();
                this.receiverDiagnosticsCollector.Value.MessageEmitted(message.Envelope, diagnosticsTime);
            }

            if (this.counters != null)
            {
                var messageTimeReal = this.scheduler.Clock.ToRealTime(message.CreationTime);
                var messageOriginatingTimeReal = this.scheduler.Clock.ToRealTime(message.OriginatingTime);
                this.counters.Increment(ReceiverCounters.Total);
                this.counters.RawValue(ReceiverCounters.IngestTime, (Time.GetCurrentTime() - messageTimeReal).Ticks / 10);
                this.counters.RawValue(ReceiverCounters.PipelineExclusiveDelay, (message.CreationTime - messageOriginatingTimeReal).Ticks / 10);
                this.counters.RawValue(ReceiverCounters.OutstandingUnrecycled, this.Recycler.OutstandingAllocationCount);
                this.counters.RawValue(ReceiverCounters.AvailableRecycled, this.Recycler.AvailableAllocationCount);
            }

            // First, only clone the message if the component requires isolation, to allow for clone-free
            // operation on the synchronous execution path if enforceIsolation is set to false.
            if (this.enforceIsolation)
            {
                message.Data = message.Data.DeepClone(this.Recycler);
            }

            if (this.DeliveryPolicy.AttemptSynchronousDelivery && this.awaitingDelivery.IsEmpty && message.SequenceId != int.MaxValue)
            {
                // fast path - try to deliver synchronously for as long as we can
                // however, if this thread already has a lock on the owner it means some other receiver is in our call stack (we have a delivery loop),
                // so bail out because executing the delegate would break the exclusive execution promise of the receivers
                // An existing lock can also indicate that a downstream component wants us to slow down (throttle)
                bool delivered = this.scheduler.TryExecute(
                    this.syncContext,
                    this.onReceived,
                    message,
                    message.OriginatingTime,
                    this.schedulerContext,
                    hasDiagnosticsCollector,
                    out var receiverStartTime,
                    out var receiverEndTime);

                if (delivered)
                {
                    if (this.receiverDiagnosticsCollector.Value != null)
                    {
                        this.receiverDiagnosticsCollector.Value.MessageProcessed(
                            message.Envelope,
                            receiverStartTime,
                            receiverEndTime,
                            this.pipeline.DiagnosticsConfiguration.TrackMessageSize ? this.ComputeDataSize(message.Data) : 0,
                            diagnosticsTime);

                        this.receiverDiagnosticsCollector.Value.UpdateDiagnosticState(this.Owner.ToString());
                    }

                    if (this.enforceIsolation)
                    {
                        // recycle the cloned copy if synchronous execution succeeded
                        this.Recycler.Recycle(message.Data);
                    }

                    return;
                }
            }

            // slow path - we need to queue the message, and let the scheduler do the rest
            // we need to clone the message before queuing, but only if we didn't already
            if (!this.enforceIsolation)
            {
                message.Data = message.Data.DeepClone(this.Recycler);
            }

            this.awaitingDelivery.Enqueue(message, this.receiverDiagnosticsCollector.Value, diagnosticsTime, this.StartThrottling, out QueueTransition stateTransition);

            // if the queue was empty or if the next message is a closing message, we need to schedule delivery
            if (stateTransition.ToNotEmpty || stateTransition.ToClosing)
            {
                // allow scheduling past finalization when throttling to ensure that we get a chance to unthrottle
                this.scheduler.Schedule(this.syncContext, this.DeliverNext, message.OriginatingTime, this.schedulerContext, true, this.awaitingDelivery.IsThrottling);
            }
        }

        internal void DeliverNext()
        {
            var currentTime = this.scheduler.Clock.GetCurrentTime();

            if (this.awaitingDelivery.TryDequeue(out Message<T> message, out QueueTransition stateTransition, currentTime, this.receiverDiagnosticsCollector.Value, this.StopThrottling))
            {
                if (message.SequenceId == int.MaxValue)
                {
                    // emitter was closed
                    this.OnUnsubscribed(message.OriginatingTime);
                    return;
                }

                DateTime start = (this.counters != null) ? Time.GetCurrentTime() : default;

                if (this.receiverDiagnosticsCollector.Value == null)
                {
                    this.onReceived(message);
                }
                else
                {
                    var receiverStartTime = this.pipeline.GetCurrentTime();
                    this.onReceived(message);
                    var receiverEndTime = this.pipeline.GetCurrentTime();

                    this.receiverDiagnosticsCollector.Value.MessageProcessed(
                        message.Envelope,
                        receiverStartTime,
                        receiverEndTime,
                        this.pipeline.DiagnosticsConfiguration.TrackMessageSize ? this.ComputeDataSize(message.Data) : 0,
                        currentTime);

                    this.receiverDiagnosticsCollector.Value.UpdateDiagnosticState(this.Owner.ToString());
                }

                if (this.counters != null)
                {
                    var end = Time.GetCurrentTime();
                    var messageTimeReal = this.scheduler.Clock.ToRealTime(message.CreationTime);
                    var messageOriginatingTimeReal = this.scheduler.Clock.ToRealTime(message.OriginatingTime);
                    this.counters.RawValue(ReceiverCounters.TimeInQueue, (start - messageTimeReal).Ticks / 10);
                    this.counters.RawValue(ReceiverCounters.ProcessingTime, (end - start).Ticks / 10);
                    this.counters.Increment(ReceiverCounters.Processed);
                    this.counters.RawValue(ReceiverCounters.ProcessingDelay, (end - messageTimeReal).Ticks / 10);
                    this.counters.RawValue(ReceiverCounters.PipelineInclusiveDelay, (end - messageOriginatingTimeReal).Ticks / 10);
                }

                // recycle the item we dequeued
                this.Recycler.Recycle(message.Data);

                if (!stateTransition.ToEmpty)
                {
                    // allow scheduling past finalization when throttling to ensure that we get a chance to unthrottle
                    this.scheduler.Schedule(this.syncContext, this.DeliverNext, this.awaitingDelivery.NextMessageTime, this.schedulerContext, true, this.awaitingDelivery.IsThrottling);
                }
            }
        }

        private void StartThrottling(QueueTransition stateTransition)
        {
            // if queue is full (as decided between local policy and global policy), lock the emitter.syncContext (which we might already own) until we make more room
            if (stateTransition.ToStartThrottling)
            {
                this.counters?.Increment(ReceiverCounters.ThrottlingRequests);
                this.Source.Pipeline.Scheduler.Freeze(this.Source.SyncContext);
                this.receiverDiagnosticsCollector.Value?.PipelineElementReceiverThrottle(true);
            }
        }

        private void StopThrottling(QueueTransition stateTransition)
        {
            if (stateTransition.ToStopThrottling)
            {
                this.Source.Pipeline.Scheduler.Thaw(this.Source.SyncContext);
                this.receiverDiagnosticsCollector.Value?.PipelineElementReceiverThrottle(false);
            }
        }

        private void OnUnsubscribed(DateTime lastOriginatingTime)
        {
            this.pipeline.DiagnosticsCollector?.PipelineElementReceiverUnsubscribe(this.pipeline, this.element, this, this.Source);
            this.lastEnvelope = new Envelope(DateTime.MaxValue, DateTime.MaxValue, this.Id, int.MaxValue);
            foreach (var handler in this.unsubscribedHandlers)
            {
                PipelineElement.TrackStateObjectOnContext(() => handler(lastOriginatingTime), this.Owner, this.pipeline).Invoke();
            }

            // clear the source only after all handlers have run to avoid this node being finalized prematurely
            this.Source = null;
        }

        /// <summary>
        /// Computes data size by running through serialization.
        /// </summary>
        /// <param name="data">Message data.</param>
        /// <returns>Data size (bytes).</returns>
        private int ComputeDataSize(T data)
        {
            if (this.computeDataSize == null)
            {
                var serializers = KnownSerializers.Default;
                var context = new SerializationContext(serializers);
                var handler = serializers.GetHandler<T>();
                var writer = new BufferWriter(16);
                this.computeDataSize = m =>
                {
                    writer.Reset();
                    context.Reset();
                    try
                    {
                        handler.Serialize(writer, m, context);
                    }
                    catch (NotSupportedException)
                    {
                        // cannot serialize Type, IntPtr, UIntPtr, MemberInfo, StackTrace, ...
                        this.computeDataSize = _ => 0; // stop trying
                        return 0;
                    }

                    return writer.Position;
                };
            }

            return this.computeDataSize(data);
        }

        private class NonSerializer : NonSerializer<Receiver<T>>
        {
        }
    }
}
