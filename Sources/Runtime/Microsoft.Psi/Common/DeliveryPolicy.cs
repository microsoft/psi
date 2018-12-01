// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Encapsulates the options for message delivery behavior.
    /// </summary>
    public class DeliveryPolicy
    {
        /// <summary>
        /// The default initial size of receiver queues.
        /// </summary>
        private const int DefaultInitialQueueSize = 16;

        /// <summary>
        /// A delivery policy which lets the queue grow as much as needed, with no latency constraints.
        /// </summary>
        private static DeliveryPolicy unlimitedPolicy = new DeliveryPolicy(
            initialQueueSize: DefaultInitialQueueSize,
            maximumQueueSize: int.MaxValue,
            maximumLatency: null,
            throttleWhenFull: false,
            attemptSynchronous: false);

        /// <summary>
        /// A delivery policy which limits the queue to one message, with no latency constraints.
        /// </summary>
        private static DeliveryPolicy latestMessage = new DeliveryPolicy(
            initialQueueSize: 1,
            maximumQueueSize: 1,
            maximumLatency: null,
            throttleWhenFull: false,
            attemptSynchronous: false);

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPolicy"/> class.
        /// </summary>
        /// <param name="initialQueueSize">The initial receiver queue size.</param>
        /// <param name="maximumQueueSize">The maximum receiver queue size.</param>
        /// <param name="maximumLatency">The maximum latency allowable for messages to be delivered.</param>
        /// <param name="throttleWhenFull">A value indicating whether to block the upstream producer if the receiver queue is full.</param>
        /// <param name="attemptSynchronous">A value indicating whether to attempt synchronous delivery.</param>
        internal DeliveryPolicy(
            int initialQueueSize = DefaultInitialQueueSize,
            int maximumQueueSize = int.MaxValue,
            TimeSpan? maximumLatency = null,
            bool throttleWhenFull = false,
            bool attemptSynchronous = false)
        {
            this.InitialQueueSize = initialQueueSize;
            this.MaximumQueueSize = maximumQueueSize;
            this.MaximumLatency = maximumLatency;
            this.ThrottleWhenFull = throttleWhenFull;
            this.AttemptSynchronous = attemptSynchronous;
        }

        /// <summary>
        /// Gets a lossless, unlimited delivery policy which lets the receiver queue grow as much as needed, with no latency constraints.
        /// </summary>
        public static DeliveryPolicy Unlimited => DeliveryPolicy.unlimitedPolicy;

        /// <summary>
        /// Gets a lossy delivery policy which limits the receiver queue to one message, with no latency constraints.
        /// </summary>
        public static DeliveryPolicy LatestMessage => DeliveryPolicy.latestMessage;

        /// <summary>
        /// Gets a value indicating whether to block the upstream producer if the receiver queue is full.
        /// </summary>
        /// <remarks>Use with care, as it affects all other subscribers to the same producer and can introduce deadlocks (a blocked producer cannot process control messages anymore).</remarks>
        public bool ThrottleWhenFull { get; private set; }

        /// <summary>
        /// Gets the initial size of the receiver queue that holds the messages pending delivery.
        /// </summary>
        public int InitialQueueSize { get; private set; }

        /// <summary>
        /// Gets the maximum size of the receiver queue that holds the messages pending delivery.
        /// </summary>
        public int MaximumQueueSize { get; private set; }

        /// <summary>
        /// Gets the maximum latency of items to be delivered. Items with a latency larger than this are discarded.
        /// </summary>
        public TimeSpan? MaximumLatency { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the runtime should attempt synchronous delivery when possible.
        /// </summary>
        public bool AttemptSynchronous { get; private set; }

        /// <summary>
        /// Creates a latency-constrained delivery policy. Messages older than the specified maximum latency are discarded.
        /// </summary>
        /// <param name="maximumLatency">The maximum latency for messages to be delivered.</param>
        /// <returns>A latency-constrained delivery policy.</returns>
        public static DeliveryPolicy LatencyConstrained(TimeSpan maximumLatency)
        {
            return new DeliveryPolicy(DefaultInitialQueueSize, int.MaxValue, maximumLatency, false, false);
        }

        /// <summary>
        /// Creates a queue-size constrained delivery policy. Messages will accumulate only up to the specified
        /// maximum queue size, after which they will be discarded.
        /// </summary>
        /// <param name="maximumQueueSize">The maximum queue size.</param>
        /// <returns>A queue-size constrained delivery policy.</returns>
        public static DeliveryPolicy QueueSizeConstrained(int maximumQueueSize)
        {
            return new DeliveryPolicy(1, maximumQueueSize, null, false, false);
        }
    }
}
