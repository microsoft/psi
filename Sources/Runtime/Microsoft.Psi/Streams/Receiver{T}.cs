// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Diagnostics;
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
    /// <typeparam name="T">The type of messages that can be received</typeparam>
    [Serializer(typeof(Receiver<>.NonSerializer))]
    public class Receiver<T> : IReceiver, IConsumer<T>
    {
        private readonly Action<Message<T>> onReceived;
        private readonly object owner;
        private readonly Scheduler scheduler;
        private readonly SynchronizationLock syncContext;
        private readonly IRecyclingPool<T> cloner;
        private readonly bool enforceIsolation;

        private Emitter<T> source;
        private DeliveryQueue<T> awaitingDelivery;
        private IPerfCounterCollection<ReceiverCounters> counters;
        private DeliveryPolicy policy;
#if DEBUG
        private StackTrace debugTrace;
#endif

        internal Receiver(object owner, Action<Message<T>> onReceived, SynchronizationLock context, Pipeline pipeline, bool enforceIsolation = false)
        {
            this.scheduler = pipeline.Scheduler;
            this.onReceived = onReceived;
            this.owner = owner;
            this.syncContext = context;
            this.enforceIsolation = enforceIsolation;
            this.cloner = RecyclingPool.Create<T>();
#if DEBUG
            this.debugTrace = new StackTrace(true);
#endif
        }

        /// <inheritdoc />
        IEmitter IReceiver.Source => this.source;

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
            if (this.source != null)
            {
                this.source.Unsubscribe(this);
            }
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
                    Tuple.Create(ReceiverCounters.AvailableRecycled, "Recycled messages", "The number of messages that are available for recycling", PerfCounterType.NumberOfItems32)
                });
#pragma warning restore SA1118 // Parameter must not span multiple lines

            this.counters = perf.Enable(Category, name);
            this.awaitingDelivery.EnablePerfCounters(this.counters);
        }

        internal void OnSubscribe(Emitter<T> source, DeliveryPolicy policy)
        {
            if (this.source != null)
            {
                throw new InvalidOperationException("This receiver is already connected to a source emitter.");
            }

            this.source = source;
            this.policy = policy;

            var combinedPolicy = this.policy.Merge(this.scheduler.GlobalPolicy);
            this.awaitingDelivery = new DeliveryQueue<T>(combinedPolicy, this.cloner);
        }

        internal void OnUnsubscribe()
        {
            this.source = null;
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

            if (this.policy.IsSynchronous && this.awaitingDelivery.IsEmpty)
            {
                // fast path - try to deliver synchronously for as long as we can
                // however, if this thread already has a lock on the owner it means some other receiver is in our call stack (we have a delivery loop),
                // so bail out because executing the delegate would break the exclusive execution promise of the receivers
                // An existing lock can also indicate that a downstream component wants us to slow down (throttle)
                bool delivered = this.scheduler.TryExecute(this.syncContext, this.onReceived, message, message.OriginatingTime);
                if (delivered)
                {
                    return;
                }
            }

            // slow path - we need to queue the message, and let the scheduler do the rest
            // we need to clone the message before queuing, but only if we didn't already
            if (!this.enforceIsolation)
            {
                message.Data = message.Data.DeepClone(this.cloner);
            }

            QueueTransition stateTransition;
            this.awaitingDelivery.Enqueue(message, out stateTransition);

            // if the queue was empty, we need to schedule delivery
            if (stateTransition.ToNotEmpty)
            {
                this.scheduler.Schedule(this.syncContext, this.DeliverNext, message.OriginatingTime);
            }

            // if queue is full (as decided between local policy and global policy), lock the emitter.syncContext (which we might already own) until we make more room
            if (stateTransition.ToFull)
            {
                this.counters?.Increment(ReceiverCounters.ThrottlingRequests);
                this.source.Pipeline.Scheduler.Freeze(this.source.SyncContext);
            }
        }

        internal void DeliverNext()
        {
            Message<T> message;
            QueueTransition stateTransition;
            if (this.awaitingDelivery.TryDequeue(out message, out stateTransition, this.scheduler.Clock.GetCurrentTime()))
            {
                if (stateTransition.ToNotFull)
                {
                    this.source.Pipeline.Scheduler.Thaw(this.source.SyncContext);
                }

                // remember the object we dequeued, and make a clone if the component requests isolation (the component will have to recycle this clone when done)
                var data = message.Data;
                if (this.enforceIsolation)
                {
                    message.Data = message.Data.DeepClone(this.cloner);
                }

                DateTime start = (this.counters != null) ? Time.GetCurrentTime() : default(DateTime);
                this.onReceived(message);

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
            }

            if (!stateTransition.ToEmpty)
            {
                this.scheduler.Schedule(this.syncContext, this.DeliverNext, this.awaitingDelivery.NextMessageTime);
            }
        }

        private class NonSerializer : NonSerializer<Receiver<T>>
        {
        }
    }
}
