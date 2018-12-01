// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Streams
{
    using System;
    using System.Collections.Generic;

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
    }
#pragma warning restore SA1649 // File name must match first type name

    /// <summary>
    /// Single producer single consumer queue.
    /// </summary>
    /// <typeparam name="T">The type of data in the queue</typeparam>
    internal sealed class DeliveryQueue<T>
    {
        private readonly Queue<Message<T>> queue; // not ConcurrentQueue because it performs an allocation for each Enqueue. We want to be allocation free.
        private readonly IRecyclingPool<T> cloner;
        private DeliveryPolicy policy;
        private bool isEmpty = true;
        private bool isThrottling;
        private DateTime nextMessageTime;
        private IPerfCounterCollection<ReceiverCounters> counters;
        private int maxQueueSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryQueue{T}"/> class.
        /// </summary>
        /// <param name="policy">The delivery policy dictating message queuing and delivery behavior</param>
        /// <param name="cloner">The recycling pool to recycle dropped messages to</param>
        public DeliveryQueue(DeliveryPolicy policy, IRecyclingPool<T> cloner)
        {
            this.policy = policy;
            this.cloner = cloner;
            this.queue = new Queue<Message<T>>(policy.InitialQueueSize);
        }

        public bool IsEmpty => this.isEmpty;

        public bool IsThrottling => this.isThrottling;

        public DateTime NextMessageTime => this.nextMessageTime;

        public bool TryDequeue(out Message<T> message, out QueueTransition stateTransition, DateTime currentTime)
        {
            message = default(Message<T>);
            bool found = false;

            lock (this.queue)
            {
                while (this.queue.Count != 0)
                {
                    var oldest = this.queue.Dequeue();

                    // check if we have a maximum-latency policy and a message that exceeds that latency
                    if (this.policy.MaximumLatency.HasValue && (currentTime - oldest.OriginatingTime) > this.policy.MaximumLatency.Value)
                    {
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

        public void Enqueue(Message<T> message, out QueueTransition stateTransition)
        {
            lock (this.queue)
            {
                if (this.queue.Count > this.policy.MaximumQueueSize)
                {
                    var item = this.queue.Dequeue(); // discard unprocessed items if the policy requires it
                    this.cloner.Recycle(item.Data);
                    this.counters?.Increment(ReceiverCounters.Dropped);
                }

                this.queue.Enqueue(message);
                if (this.queue.Count > this.maxQueueSize)
                {
                    this.maxQueueSize = this.queue.Count;
                }

                if (this.counters != null)
                {
                    this.counters.RawValue(ReceiverCounters.QueueSize, this.queue.Count);
                    this.counters.RawValue(ReceiverCounters.MaxQueueSize, this.maxQueueSize);
                }

                stateTransition = this.UpdateState();
            }
        }

        public void EnablePerfCounters(IPerfCounterCollection<ReceiverCounters> counters)
        {
            this.counters = counters;
        }

        private QueueTransition UpdateState()
        {
            int count = this.queue.Count;
            bool wasEmpty = this.isEmpty;
            bool wasThrottling = this.isThrottling;
            this.isEmpty = count == 0;
            this.isThrottling = this.policy.ThrottleWhenFull && count > this.policy.MaximumQueueSize;
            this.nextMessageTime = (count == 0) ? DateTime.MinValue : this.queue.Peek().Envelope.OriginatingTime;

            return new QueueTransition()
            {
                ToEmpty = !wasEmpty && this.isEmpty,
                ToNotEmpty = wasEmpty && !this.IsEmpty,
                ToStartThrottling = !wasThrottling && this.isThrottling,
                ToStopThrottling = wasThrottling && !this.isThrottling
            };
        }
    }
}