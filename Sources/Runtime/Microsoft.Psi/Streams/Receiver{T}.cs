// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Common;
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
    public class Receiver<T> : IReceiver, IConsumer<T>
    {
        private readonly Action<Message<T>> onReceived;
        private readonly int id;
        private readonly string name;
        private readonly PipelineElement element;
        private readonly object owner;
        private readonly Pipeline pipeline;
        private readonly Scheduler scheduler;
        private readonly SchedulerContext schedulerContext;
        private readonly SynchronizationLock syncContext;
        private readonly IRecyclingPool<T> cloner;
        private readonly bool enforceIsolation;
        private readonly List<UnsubscribedHandler> unsubscribedHandlers = new List<UnsubscribedHandler>();

        private Emitter<T> source;
        private DeliveryQueue<T> awaitingDelivery;
        private IPerfCounterCollection<ReceiverCounters> counters;
        private DeliveryPolicy policy;
        private Func<T, int> computeDataSize = null;

        internal Receiver(int id, string name, PipelineElement element, object owner, Action<Message<T>> onReceived, SynchronizationLock context, Pipeline pipeline, bool enforceIsolation = false)
        {
            this.scheduler = pipeline.Scheduler;
            this.schedulerContext = pipeline.SchedulerContext;
            this.onReceived = PipelineElement.TrackStateObjectOnContext<Message<T>>(onReceived, owner, pipeline);
            this.id = id;
            this.name = name;
            this.element = element;
            this.owner = owner;
            this.syncContext = context;
            this.enforceIsolation = enforceIsolation;
            this.cloner = RecyclingPool.Create<T>();
            this.pipeline = pipeline;
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
        IEmitter IReceiver.Source => this.source;

        /// <inheritdoc />
        public int Id => this.id;

        /// <inheritdoc />
        public string Name => this.name;

        /// <inheritdoc />
        public Type Type => typeof(T);

        /// <inheritdoc />
        public object Owner => this.owner;

        /// <summary>
        /// Gets receiver message recycler.
        /// </summary>
        public IRecyclingPool<T> Recycler => this.cloner;

        /// <inheritdoc />
        Receiver<T> IConsumer<T>.In => this;

        internal Emitter<T> Source => this.source;

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
            this.cloner.Recycle(item);
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

        internal void OnSubscribe(Emitter<T> source, bool allowSubscribeWhileRunning, DeliveryPolicy policy)
        {
            if (this.source != null)
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

            this.source = source;
            this.policy = policy;
            this.awaitingDelivery = new DeliveryQueue<T>(this.pipeline, this.element, this, policy, this.cloner);
            this.pipeline.DiagnosticsCollector?.PipelineElementReceiverSubscribe(this.pipeline, this.element, this, source);
        }

        internal void OnUnsubscribe()
        {
            if (this.source != null)
            {
                this.source = null;
                this.OnUnsubscribed(this.pipeline.GetCurrentTime());
            }
        }

        internal void Receive(Message<T> message)
        {
            if (this.counters != null)
            {
                var messageTimeReal = this.scheduler.Clock.ToRealTime(message.Time);
                var messageOriginatingTimeReal = this.scheduler.Clock.ToRealTime(message.OriginatingTime);
                this.counters.Increment(ReceiverCounters.Total);
                this.counters.RawValue(ReceiverCounters.IngestTime, (Time.GetCurrentTime() - messageTimeReal).Ticks / 10);
                this.counters.RawValue(ReceiverCounters.PipelineExclusiveDelay, (message.Time - messageOriginatingTimeReal).Ticks / 10);
                this.counters.RawValue(ReceiverCounters.OutstandingUnrecycled, this.cloner.OutstandingAllocationCount);
                this.counters.RawValue(ReceiverCounters.AvailableRecycled, this.cloner.AvailableAllocationCount);
            }

            // First, only clone the message if the component requires isolation, to allow for clone-free operation on the fast path
            // Optimization is release-only, to make sure the cloning path is exercised in tests
            if (this.enforceIsolation)
            {
                message.Data = message.Data.DeepClone(this.cloner);
            }

            if (this.policy.AttemptSynchronous && this.awaitingDelivery.IsEmpty && message.SequenceId != int.MaxValue)
            {
                // fast path - try to deliver synchronously for as long as we can
                // however, if this thread already has a lock on the owner it means some other receiver is in our call stack (we have a delivery loop),
                // so bail out because executing the delegate would break the exclusive execution promise of the receivers
                // An existing lock can also indicate that a downstream component wants us to slow down (throttle)
                bool delivered = this.scheduler.TryExecute(this.syncContext, this.onReceived, message, message.OriginatingTime, this.schedulerContext);
                if (delivered)
                {
                    this.pipeline.DiagnosticsCollector?.MessageProcessedSynchronously(
                        this.pipeline,
                        this.element,
                        this,
                        this.awaitingDelivery.Count,
                        message.Envelope,
                        this.pipeline.DiagnosticsConfiguration.TrackMessageSize ? this.ComputeDataSize(message.Data) : 0);
                    return;
                }
            }

            // slow path - we need to queue the message, and let the scheduler do the rest
            // we need to clone the message before queuing, but only if we didn't already
            if (!this.enforceIsolation)
            {
                message.Data = message.Data.DeepClone(this.cloner);
            }

            this.awaitingDelivery.Enqueue(message, out QueueTransition stateTransition);

            // if the queue was empty or if the next message is a closing message, we need to schedule delivery
            if (stateTransition.ToNotEmpty || stateTransition.ToClosing)
            {
                this.scheduler.Schedule(this.syncContext, this.DeliverNext, message.OriginatingTime, this.schedulerContext);
            }

            // if queue is full (as decided between local policy and global policy), lock the emitter.syncContext (which we might already own) until we make more room
            if (stateTransition.ToStartThrottling)
            {
                this.counters?.Increment(ReceiverCounters.ThrottlingRequests);
                this.source.Pipeline.Scheduler.Freeze(this.source.SyncContext);
                this.pipeline.DiagnosticsCollector?.PipelineElementReceiverThrottle(this.pipeline, this.element, this, true);
            }
        }

        internal void DeliverNext()
        {
            if (this.awaitingDelivery.TryDequeue(out Message<T> message, out QueueTransition stateTransition, this.scheduler.Clock.GetCurrentTime()))
            {
                if (stateTransition.ToStopThrottling)
                {
                    this.source.Pipeline.Scheduler.Thaw(this.source.SyncContext);
                    this.pipeline.DiagnosticsCollector?.PipelineElementReceiverThrottle(this.pipeline, this.element, this, false);
                }

                if (message.SequenceId == int.MaxValue)
                {
                    // emitter was closed
                    this.OnUnsubscribed(message.OriginatingTime);
                    return;
                }

                // remember the object we dequeued, and make a clone if the component requests isolation (the component will have to recycle this clone when done)
                var data = message.Data;
                if (this.enforceIsolation)
                {
                    message.Data = message.Data.DeepClone(this.cloner);
                }

                DateTime start = (this.counters != null) ? Time.GetCurrentTime() : default(DateTime);
                this.pipeline.DiagnosticsCollector?.MessageProcessStart(
                    this.pipeline,
                    this.element,
                    this,
                    this.awaitingDelivery.Count,
                    message.Envelope,
                    this.pipeline.DiagnosticsConfiguration.TrackMessageSize ? this.ComputeDataSize(message.Data) : 0);
                this.onReceived(message);
                this.pipeline.DiagnosticsCollector?.MessageProcessComplete(this.pipeline, this.element, this, message.Envelope);

                if (this.counters != null)
                {
                    var end = Time.GetCurrentTime();
                    var messageTimeReal = this.scheduler.Clock.ToRealTime(message.Time);
                    var messageOriginatingTimeReal = this.scheduler.Clock.ToRealTime(message.OriginatingTime);
                    this.counters.RawValue(ReceiverCounters.TimeInQueue, (start - messageTimeReal).Ticks / 10);
                    this.counters.RawValue(ReceiverCounters.ProcessingTime, (end - start).Ticks / 10);
                    this.counters.Increment(ReceiverCounters.Processed);
                    this.counters.RawValue(ReceiverCounters.ProcessingDelay, (end - messageTimeReal).Ticks / 10);
                    this.counters.RawValue(ReceiverCounters.PipelineInclusiveDelay, (end - messageOriginatingTimeReal).Ticks / 10);
                }

                // recycle the item we dequeued
                this.cloner.Recycle(data);

                if (!stateTransition.ToEmpty)
                {
                    this.scheduler.Schedule(this.syncContext, this.DeliverNext, this.awaitingDelivery.NextMessageTime, this.schedulerContext);
                }
            }
        }

        private void OnUnsubscribed(DateTime lastOriginatingTime)
        {
            this.pipeline.DiagnosticsCollector?.PipelineElementReceiverUnsubscribe(this.pipeline, this.element, this, this.source);
            foreach (var handler in this.unsubscribedHandlers)
            {
                PipelineElement.TrackStateObjectOnContext(() => handler(lastOriginatingTime), this.Owner, this.pipeline).Invoke();
            }

            // clear the source only after all handlers have run to avoid this node being finalized prematurely
            this.source = null;
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
