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
        internal const int DefaultInitialQueueSize = 16;

        /// <summary>
        /// A delivery policy which lets the queue grow as much as needed, with no latency constraints.
        /// </summary>
        private static readonly DeliveryPolicy UnlimitedPolicy = new DeliveryPolicy(
            initialQueueSize: DefaultInitialQueueSize,
            maximumQueueSize: int.MaxValue,
            maximumLatency: null,
            throttleQueueSize: null,
            attemptSynchronous: false,
            nameof(Unlimited));

        /// <summary>
        /// A delivery policy which limits the queue to one message, with no latency constraints.
        /// </summary>
        private static readonly DeliveryPolicy LatestMessagePolicy = new DeliveryPolicy(
            initialQueueSize: 1,
            maximumQueueSize: 1,
            maximumLatency: null,
            throttleQueueSize: null,
            attemptSynchronous: false,
            nameof(LatestMessage));

        /// <summary>
        /// The throttle policy limits the queue to one message and throttles its source as long as
        /// there is a message in the queue waiting to be processed.
        /// </summary>
        private static readonly DeliveryPolicy ThrottlePolicy = new DeliveryPolicy(
            initialQueueSize: 1,
            maximumQueueSize: int.MaxValue,
            maximumLatency: null,
            throttleQueueSize: 1,
            attemptSynchronous: false,
            nameof(Throttle));

        /// <summary>
        /// A delivery policy which attempts synchronous message delivery; if synchronous delivery fails, the source is throttled.
        /// </summary>
        private static readonly DeliveryPolicy SynchronousOrThrottlePolicy = new DeliveryPolicy(
            initialQueueSize: 1,
            maximumQueueSize: int.MaxValue,
            maximumLatency: null,
            throttleQueueSize: 1,
            attemptSynchronous: true,
            nameof(SynchronousOrThrottle));

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPolicy"/> class.
        /// </summary>
        /// <param name="initialQueueSize">The initial receiver queue size.</param>
        /// <param name="maximumQueueSize">The maximum receiver queue size.</param>
        /// <param name="maximumLatency">The maximum latency allowable for messages to be delivered.</param>
        /// <param name="throttleQueueSize">The number of messages in the receiver queue at and above which the upstream producer will be blocked.</param>
        /// <param name="attemptSynchronous">A value indicating whether to attempt synchronous delivery.</param>
        /// <param name="name">Name used for debugging and diagnostics.</param>
        internal DeliveryPolicy(
            int initialQueueSize = DefaultInitialQueueSize,
            int maximumQueueSize = int.MaxValue,
            TimeSpan? maximumLatency = null,
            int? throttleQueueSize = null,
            bool attemptSynchronous = false,
            string name = null)
        {
            this.InitialQueueSize = initialQueueSize;
            this.MaximumQueueSize = maximumQueueSize;
            this.MaximumLatency = maximumLatency;
            this.ThrottleQueueSize = throttleQueueSize;
            this.AttemptSynchronousDelivery = attemptSynchronous;
            this.Name = name ?? $"{nameof(DeliveryPolicy)}({nameof(initialQueueSize)}={initialQueueSize}, {nameof(maximumQueueSize)}={maximumQueueSize}, {nameof(maximumLatency)}={maximumLatency}, {nameof(throttleQueueSize)}={throttleQueueSize}, {nameof(attemptSynchronous)}={attemptSynchronous})";
        }

        /// <summary>
        /// Gets a lossless, unlimited delivery policy which lets the receiver queue grow as much as needed, with no latency constraints.
        /// </summary>
        public static DeliveryPolicy Unlimited => DeliveryPolicy.UnlimitedPolicy;

        /// <summary>
        /// Gets a lossy delivery policy which limits the receiver queue to one message, with no latency constraints.
        /// </summary>
        public static DeliveryPolicy LatestMessage => DeliveryPolicy.LatestMessagePolicy;

        /// <summary>
        /// Gets a throttling delivery policy, which attempts to throttle its source as long as there is a message in the queue waiting to be processed.
        /// </summary>
        public static DeliveryPolicy Throttle => DeliveryPolicy.ThrottlePolicy;

        /// <summary>
        /// Gets a delivery policy which attempts synchronous message delivery; if synchronous delivery fails, the source is throttled.
        /// </summary>
        public static DeliveryPolicy SynchronousOrThrottle => DeliveryPolicy.SynchronousOrThrottlePolicy;

        /// <summary>
        /// Gets the number of messages in the receiver queue at and above which the upstream producer will be blocked.
        /// </summary>
        /// <remarks>Use with care, as it affects all other subscribers to the same producer and can introduce deadlocks (a blocked producer cannot process control messages anymore).</remarks>
        public int? ThrottleQueueSize { get; private set; }

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
        public bool AttemptSynchronousDelivery { get; private set; }

        /// <summary>
        /// Gets name used for debugging and diagnostics.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a latency-constrained delivery policy. Messages older than the specified maximum latency are discarded.
        /// </summary>
        /// <param name="maximumLatency">The maximum latency for messages to be delivered.</param>
        /// <returns>A latency-constrained delivery policy.</returns>
        public static DeliveryPolicy LatencyConstrained(TimeSpan maximumLatency)
        {
            return new DeliveryPolicy(
                DefaultInitialQueueSize,
                int.MaxValue,
                maximumLatency,
                null,
                false,
                $"{nameof(LatencyConstrained)}({nameof(maximumLatency)}={maximumLatency})");
        }

        /// <summary>
        /// Creates a queue-size constrained delivery policy. Messages will accumulate only up to the specified
        /// maximum queue size, after which they will be discarded.
        /// </summary>
        /// <param name="maximumQueueSize">The maximum queue size.</param>
        /// <param name="throttleWhenFull">A value indicating whether to block the upstream producer if the receiver queue is full.</param>
        /// <param name="attemptSynchronous">A value indicating whether to attempt synchronous delivery.</param>
        /// <returns>A queue-size constrained delivery policy.</returns>
        public static DeliveryPolicy QueueSizeConstrained(int maximumQueueSize, bool throttleWhenFull = false, bool attemptSynchronous = false)
        {
            return new DeliveryPolicy(
                1,
                maximumQueueSize,
                null,
                throttleWhenFull ? maximumQueueSize : null,
                attemptSynchronous,
                $"{nameof(QueueSizeConstrained)}({nameof(maximumQueueSize)}={maximumQueueSize})");
        }

        /// <summary>
        /// Creates a queue-size throttled delivery policy. Messages will accumulate only up to the specified
        /// maximum queue size, after which the upstream producer will be blocked until the number of messages
        /// in the queue falls below this value.
        /// </summary>
        /// <param name="throttleQueueSize">A value indicating whether to block the upstream producer if the receiver queue is full.</param>
        /// <returns>A queue-size throttled delivery policy.</returns>
        public static DeliveryPolicy QueueSizeThrottled(int throttleQueueSize)
        {
            return new DeliveryPolicy(
                1,
                int.MaxValue,
                null,
                throttleQueueSize,
                false,
                $"{nameof(Throttle)}({nameof(throttleQueueSize)}={throttleQueueSize})");
        }

        /// <summary>
        /// Creates a typed delivery policy with guarantees by adding a message guaranteed function to an existing untyped delivery policy.
        /// </summary>
        /// <typeparam name="T">The type of the messages in the resulting delivery policy.</typeparam>
        /// <param name="deliveryPolicy">The untyped delivery policy.</param>
        /// <param name="guaranteeDelivery">A function that evaluates whether the delivery of a given message should be guaranteed.</param>
        /// <returns>The typed delivery policy with guarantees.</returns>
        public static DeliveryPolicy<T> WithGuarantees<T>(DeliveryPolicy deliveryPolicy, Func<T, bool> guaranteeDelivery)
        {
            return new DeliveryPolicy<T>(
                DeliveryPolicy.DefaultInitialQueueSize,
                deliveryPolicy.MaximumQueueSize,
                deliveryPolicy.MaximumLatency,
                deliveryPolicy.ThrottleQueueSize,
                deliveryPolicy.AttemptSynchronousDelivery,
                guaranteeDelivery,
                $"{deliveryPolicy.Name}.WithGuarantees");
        }

        /// <summary>
        /// Creates a typed delivery policy with guarantees by adding a message guaranteed function to an existing untyped delivery policy.
        /// </summary>
        /// <typeparam name="T">The type of the messages in the resulting delivery policy.</typeparam>
        /// <param name="guaranteeDelivery">A function that evaluates whether the delivery of a given message should be guaranteed.</param>
        /// <returns>The typed delivery policy with guarantees.</returns>
        public DeliveryPolicy<T> WithGuarantees<T>(Func<T, bool> guaranteeDelivery)
        {
            return DeliveryPolicy.WithGuarantees(this, guaranteeDelivery);
        }
    }
}
