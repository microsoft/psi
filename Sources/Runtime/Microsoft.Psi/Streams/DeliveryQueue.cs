// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Streams
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Diagnostics;

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
        private readonly Queue<Message<T>> queue; // not ConcurrentQueue because it performs an allocation for each Enqueue. We want to be allocation free.
        private readonly IRecyclingPool<T> cloner;
        private readonly DeliveryPolicy<T> policy;
        private bool isEmpty = true;
        private bool isThrottling;
        private Envelope nextMessageEnvelope;
        private IPerfCounterCollection<ReceiverCounters> counters;
        private int maxQueueSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryQueue{T}"/> class.
        /// </summary>
        /// <param name="policy">The delivery policy dictating message queuing and delivery behavior.</param>
        /// <param name="cloner">The recycling pool to recycle dropped messages to.</param>
        public DeliveryQueue(DeliveryPolicy<T> policy, IRecyclingPool<T> cloner)
        {
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
        /// <param name="receiverCollector">Diagnostics collector for this receiver.</param>
        /// <param name="stateTransitionAction">Action to perform after the queue state transition has been evaluated.</param>
        /// <returns>True if oldest message that satisfies the policy is found.</returns>
        public bool TryDequeue(out Message<T> message, out QueueTransition stateTransition, DateTime currentTime, DiagnosticsCollector.ReceiverCollector receiverCollector, Action<QueueTransition> stateTransitionAction)
        {
            message = default;
            bool found = false;

            lock (this.queue)
            {
                // loop through the queue
                while (this.queue.Count != 0)
                {
                    var oldest = this.queue.Dequeue();

                    // compute whether we should drop this message, i.e. if the message exceeds the maximum latency and it
                    // does not have guaranteed delivery
                    var shouldDrop = this.policy.MaximumLatency.HasValue &&
                        (currentTime - oldest.OriginatingTime) > this.policy.MaximumLatency.Value &&
                        ((this.policy.GuaranteeDelivery == null) || !this.policy.GuaranteeDelivery(oldest.Data));

                    if (shouldDrop)
                    {
                        // Because this message has a latency that is larger than the policy allowed, we are dropping this message and
                        // finding the next message with smaller latency.
                        receiverCollector?.MessageDropped(currentTime);
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
                stateTransitionAction?.Invoke(stateTransition);

                receiverCollector?.QueueSizeUpdate(this.queue.Count, currentTime);

                return found;
            }
        }

        /// <summary>
        /// Enqueue the message while respecting the defined delivery policy.
        /// </summary>
        /// <param name="message">The new message.</param>
        /// <param name="receiverDiagnosticsCollector">Diagnostics collector for this receiver.</param>
        /// <param name="diagnosticsTime">The time at which diagnostic information is captured about the message being enqueued.</param>
        /// <param name="stateTransitionAction">Action to perform after the queue state transition has been evaluated.</param>
        /// <param name="stateTransition">The struct describing the status of the internal queue.</param>
        public void Enqueue(
            Message<T> message,
            DiagnosticsCollector.ReceiverCollector receiverDiagnosticsCollector,
            DateTime diagnosticsTime,
            Action<QueueTransition> stateTransitionAction,
            out QueueTransition stateTransition)
        {
            lock (this.queue)
            {
                bool dropIncomingMessage = false;
                bool IsClosingMessage(Message<T> m) => m.SequenceId == int.MaxValue;

                // If the queue size is more than the allowed size in the policy, try to drop messages in the queue
                // if possible, until we create enough space to hold the new message
                if (this.queue.Count >= this.policy.MaximumQueueSize)
                {
                    if (this.policy.GuaranteeDelivery == null)
                    {
                        // if we have no guarantees on the policy, then just drop the oldest message
                        var item = this.queue.Dequeue();
                        receiverDiagnosticsCollector?.MessageDropped(diagnosticsTime);
                        this.cloner.Recycle(item.Data);
                        this.counters?.Increment(ReceiverCounters.Dropped);
                    }
                    else
                    {
                        // if we have a policy with delivery guarantees, we need to go through the queue
                        // and inspect which messages can be removed.

                        // if the queue has already grown to a size strictly larger that MaximumQueueSize
                        if (this.queue.Count > this.policy.MaximumQueueSize)
                        {
                            // that means all messages in the queue have guaranteed delivery and cannot
                            // be dropped. In this case, we avoid further growing the queue by simply
                            // dropping the incoming message if it does not have guaranteed delivery
                            // (unless it is a closing message, which we should never drop)
                            dropIncomingMessage = !IsClosingMessage(message) && !this.policy.GuaranteeDelivery(message.Data);
                        }
                        else
                        {
                            // otherwise, the queue has exactly the maximum size, in which case we need
                            // to look through it to find whether there is a non-guaranteed message that
                            // we can drop. Start by traversing the queue and moving the messages into
                            // a temporary queue. Once we find a first message that we can free, stop
                            // the traversal.

                            // There is also an optimization here where instead of using messages into the
                            // a temporary queue, we move them into the same queue, wrapping around. We do
                            // a for loop that traverses the entire queue, taking each element and re-adding
                            // it to the queue (with the exception of the element that we drop, if we find
                            // one that can be dropped).
                            var queueSize = this.queue.Count;

                            // the hasDropped variable indicates if a message was already dropped in the
                            // traversal.
                            bool hasDropped = false;
                            for (int i = 0; i < queueSize; i++)
                            {
                                // get the top item
                                var item = this.queue.Dequeue();

                                // if no messages has been dropped yet and this message can be dropped
                                if (!hasDropped && !IsClosingMessage(item) && !this.policy.GuaranteeDelivery(item.Data))
                                {
                                    // then drop it
                                    receiverDiagnosticsCollector?.MessageDropped(diagnosticsTime);
                                    this.cloner.Recycle(item.Data);
                                    this.counters?.Increment(ReceiverCounters.Dropped);
                                    hasDropped = true;
                                }
                                else
                                {
                                    // o/w push it back at the end of the queue
                                    this.queue.Enqueue(item);
                                }
                            }

                            // if at this point we haven't found something to drop, we should drop the
                            // incoming message, if it does not have guaranteed delivery and is not a
                            // closing message
                            dropIncomingMessage = !hasDropped && !IsClosingMessage(message) && !this.policy.GuaranteeDelivery(message.Data);
                        }
                    }
                }

                // special closing message
                if (IsClosingMessage(message))
                {
                    // queued messages with an originating time past the closing time should be dropped, regardless
                    // of their guaranteed delivery.
                    while (this.queue.Count > 0 && this.queue.Peek().OriginatingTime > message.OriginatingTime)
                    {
                        var item = this.queue.Dequeue(); // discard unprocessed items which occur after the closing message
                        receiverDiagnosticsCollector?.MessageDropped(diagnosticsTime);
                        this.cloner.Recycle(item.Data);
                        this.counters?.Increment(ReceiverCounters.Dropped);
                    }
                }

                // enqueue the new message, unless we've decided it should be dropped
                if (!dropIncomingMessage)
                {
                    this.queue.Enqueue(message);
                }
                else
                {
                    // o/w capture that we have dropped the incoming message
                    receiverDiagnosticsCollector?.MessageDropped(diagnosticsTime);
                }

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
                stateTransitionAction?.Invoke(stateTransition);

                // update diagnostic information about the queue size
                receiverDiagnosticsCollector?.QueueSizeUpdate(this.queue.Count, diagnosticsTime);
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
            this.isThrottling = this.policy.ThrottleQueueSize.HasValue && count >= this.policy.ThrottleQueueSize.Value;
            this.nextMessageEnvelope = (count == 0) ? default : this.queue.Peek().Envelope;

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