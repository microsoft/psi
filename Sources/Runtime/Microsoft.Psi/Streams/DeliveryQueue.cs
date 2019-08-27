// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Streams
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Executive;

#pragma warning disable SA1649 // File name must match first type name
    /// <summary>
    /// Queue state transition.
    /// </summary>
    public struct QueueTransition
    {
        /// <summary>
        /// Queue state transition to empty.
        /// </summary>
        public bool ToEmpty;

        /// <summary>
        /// Queue state transition to no longer empty.
        /// </summary>
        public bool ToNotEmpty;

        /// <summary>
        /// Queue state transition to start throttling.
        /// </summary>
        public bool ToStartThrottling;

        /// <summary>
        /// Queue state transition to stop throttling.
        /// </summary>
        public bool ToStopThrottling;

        /// <summary>
        /// Queue state transition to closing.
        /// </summary>
        public bool ToClosing;
    }
#pragma warning restore SA1649 // File name must match first type name

    /// <summary>
    /// Single producer single consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of data in the queue.</typeparam>
    internal sealed class DeliveryQueue<T>
    {
        private readonly Pipeline pipeline;
        private readonly PipelineElement element;
        private readonly IReceiver receiver;
        private readonly Queue<Message<T>> queue; // not ConcurrentQueue because it performs an allocation for each Enqueue. We want to be allocation free.
        private readonly IRecyclingPool<T> cloner;
        private DeliveryPolicy policy;
        private bool isEmpty = true;
        private bool isThrottling;
        private Envelope nextMessageEnvelope;
        private IPerfCounterCollection<ReceiverCounters> counters;
        private int maxQueueSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryQueue{T}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this delivery queue belongs.</param>
        /// <param name="element">PipelineElement to which the receiver owning this delivery queue belongs.</param>
        /// <param name="receiver">Receiver to which this delivery queue belongs.</param>
        /// <param name="policy">The delivery policy dictating message queuing and delivery behavior.</param>
        /// <param name="cloner">The recycling pool to recycle dropped messages to.</param>
        public DeliveryQueue(Pipeline pipeline, PipelineElement element, IReceiver receiver, DeliveryPolicy policy, IRecyclingPool<T> cloner)
        {
            this.pipeline = pipeline;
            this.element = element;
            this.receiver = receiver;
            this.policy = policy;
            this.cloner = cloner;
            this.queue = new Queue<Message<T>>(policy.InitialQueueSize);
        }

        public bool IsEmpty => this.isEmpty;

        public bool IsThrottling => this.isThrottling;

        public DateTime NextMessageTime => this.nextMessageEnvelope.OriginatingTime;

        internal int Count => this.queue.Count;

        /// <summary>
        /// Try to dequeue the oldest message that obeys the defined delivery policy.
        /// </summary>
        /// <param name="message">The oldest message if it exists, or default otherwise.</param>
        /// <param name="stateTransition">Struct that describes the status of the internal queue after the dequeue.</param>
        /// <param name="currentTime">The current time of the pipeline that is used to calculate latency.</param>
        /// <returns>True if oldest message that satisfies the policy is found.</returns>
        public bool TryDequeue(out Message<T> message, out QueueTransition stateTransition, DateTime currentTime)
        {
            message = default(Message<T>);
            bool found = false;

            lock (this.queue)
            {
                // loop through the queue
                while (this.queue.Count != 0)
                {
                    var oldest = this.queue.Dequeue();

                    // check if we have a maximum-latency policy and a message that exceeds that latency
                    if (this.policy.MaximumLatency.HasValue && (currentTime - oldest.OriginatingTime) > this.policy.MaximumLatency.Value)
                    {
                        // Because this message has a latency that is larger than the policy allowed, we are dropping this message and
                        // finding the next message with smaller latency.
                        this.pipeline.DiagnosticsCollector?.MessageDropped(this.pipeline, this.element, this.receiver, this.Count, message.Envelope);
                        this.cloner.Recycle(oldest.Data);
                        this.counters?.Increment(ReceiverCounters.Dropped);
                    }
                    else
                    {
                        message = oldest;
                        found = true;
                        break;
                    }
                }

                this.counters?.RawValue(ReceiverCounters.QueueSize, this.queue.Count);

                stateTransition = this.UpdateState();
                return found;
            }
        }

        /// <summary>
        /// Enqueue the message while respecting the defined delivery policy.
        /// </summary>
        /// <param name="message">The new message.</param>
        /// <param name="stateTransition">The struct describing the status of the internal queue.</param>
        public void Enqueue(Message<T> message, out QueueTransition stateTransition)
        {
            lock (this.queue)
            {
                // If the queue size is more than the allowed size in the policy, recycle the oldest object in the queue.
                if (this.queue.Count >= this.policy.MaximumQueueSize)
                {
                    var item = this.queue.Dequeue(); // discard unprocessed items if the policy requires it
                    this.pipeline.DiagnosticsCollector?.MessageDropped(this.pipeline, this.element, this.receiver, this.Count, item.Envelope);
                    this.cloner.Recycle(item.Data);
                    this.counters?.Increment(ReceiverCounters.Dropped);
                }

                // special closing message
                if (message.SequenceId == int.MaxValue)
                {
                    // queued messages with an originating time past the closing time should be dropped
                    while (this.queue.Count > 0 && this.queue.Peek().OriginatingTime > message.OriginatingTime)
                    {
                        var item = this.queue.Dequeue(); // discard unprocessed items which occur after the closing message
                        this.pipeline.DiagnosticsCollector?.MessageDropped(this.pipeline, this.element, this.receiver, this.Count, item.Envelope);
                        this.cloner.Recycle(item.Data);
                        this.counters?.Increment(ReceiverCounters.Dropped);
                    }
                }

                // enqueue the new message
                this.queue.Enqueue(message);

                // Update a bunch of variables that helps with diagnostics and performance measurement.
                if (this.queue.Count > this.maxQueueSize)
                {
                    this.maxQueueSize = this.queue.Count;
                }

                if (this.counters != null)
                {
                    this.counters.RawValue(ReceiverCounters.QueueSize, this.queue.Count);
                    this.counters.RawValue(ReceiverCounters.MaxQueueSize, this.maxQueueSize);
                }

                // computes the new state that indicates the status of the internal queue and whether the queue needs to be locked and expanded.
                stateTransition = this.UpdateState();
                this.pipeline.DiagnosticsCollector?.MessageEnqueued(this.pipeline, this.element, this.receiver, this.Count, message.Envelope);
            }
        }

        public void EnablePerfCounters(IPerfCounterCollection<ReceiverCounters> counters)
        {
            this.counters = counters;
        }

        /// <summary>
        /// Updates the status of the <see cref="DeliveryQueue{T}"/> object by comparing different properties of the object (before update) with the
        /// status of the internal Queue object.
        /// </summary>
        /// <returns>A <see cref="QueueTransition"/> struct that describe the current state of the <see cref="DeliveryQueue{T}"/>.</returns>
        private QueueTransition UpdateState()
        {
            // save the previous state information locally.
            int count = this.queue.Count;
            bool wasEmpty = this.isEmpty;
            bool wasThrottling = this.isThrottling;
            bool wasClosing = this.nextMessageEnvelope.SequenceId == int.MaxValue;

            // update the local variables.
            this.isEmpty = count == 0;
            this.isThrottling = this.policy.ThrottleWhenFull && count > this.policy.MaximumQueueSize;
            this.nextMessageEnvelope = (count == 0) ? default(Envelope) : this.queue.Peek().Envelope;

            // create the Transition object by comparing the current and previous local state variables.
            return new QueueTransition()
            {
                ToEmpty = !wasEmpty && this.isEmpty,
                ToNotEmpty = wasEmpty && !this.IsEmpty,
                ToStartThrottling = !wasThrottling && this.isThrottling,
                ToStopThrottling = wasThrottling && !this.isThrottling,
                ToClosing = !wasClosing && this.nextMessageEnvelope.SequenceId == int.MaxValue,
            };
        }
    }
}