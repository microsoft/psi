// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Defines the possible ways in which maximum lag can be enforced
    /// </summary>
    public enum LagConstraints : byte
    {
        /// <summary>
        /// Don't enforce lag constraints
        /// </summary>
        None,

        /// <summary>
        /// Process all the items that satisfy the constraints. If no items satisfy the constraint, process the latest item.
        /// </summary>
        BestEffort,

        /// <summary>
        /// Do not process any items that don't satisfy the constraints
        /// </summary>
        Strict
    }

    /// <summary>
    /// Encapsulates the options for message delivery behavior.
    /// </summary>
    public class DeliveryPolicy
    {
        /// <summary>
        /// The default size of  receiver queues.
        /// </summary>
        private const int DefaultQueueSize = 16;

        /// <summary>
        /// The default maximum age of items waiting to be delivered. Items older than this are discarded.
        /// </summary>
        private const int DefaultMaximumLag = 100;

        /// <summary>
        /// The default policy is to drop messages once they are too old (older than an allowed lag limit), using a shallow queue to amortize CPU spikes
        /// </summary>
        private static DeliveryPolicy defaultPolicy = new DeliveryPolicy();

        /// <summary>
        /// The throttled policy queue everything but throttle callers once the queue becomes too big
        /// </summary>
        private static DeliveryPolicy throttledPolicy = new DeliveryPolicy() { ThrottleWhenFull = true, LagEnforcement = LagConstraints.None, MaximumQueueSize = 1, QueueSize = 1 };

        /// <summary>
        /// The unbounded delivery policy lets the queue grow as much as needed, with no lag constraints
        /// </summary>
        private static DeliveryPolicy unboundedPolicy = new DeliveryPolicy() { ThrottleWhenFull = false, LagEnforcement = LagConstraints.None };

        /// <summary>
        /// The delivery policy which limits the queue to one message, with no lag constraints
        /// </summary>
        private static DeliveryPolicy latestMessage = new DeliveryPolicy() { ThrottleWhenFull = false, LagEnforcement = LagConstraints.None, MaximumQueueSize = 1, QueueSize = 1 };

        /// <summary>
        /// The delivery policy which enforces synchronous message passing
        /// </summary>
#if DEBUG
        private static DeliveryPolicy immediate = new DeliveryPolicy() { ThrottleWhenFull = true, LagEnforcement = LagConstraints.None, QueueSize = 1, IsSynchronous = false };
#else
        private static DeliveryPolicy immediate = new DeliveryPolicy() { ThrottleWhenFull = true, LagEnforcement = LagConstraints.None, QueueSize = 1, IsSynchronous = true };
#endif

        /// <summary>
        /// The delivery policy which enforces synchronous message passing with throttling once queue size (16) is exceeded
        /// </summary>
#if DEBUG
        private static DeliveryPolicy immediateOrThrottle16 = new DeliveryPolicy() { MaximumQueueSize = 16, ThrottleWhenFull = true, IsSynchronous = false };
#else
        private static DeliveryPolicy immediateOrThrottle16 = new DeliveryPolicy() { MaximumQueueSize = 16, ThrottleWhenFull = true, IsSynchronous = true };
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPolicy"/> class.
        /// </summary>
        internal DeliveryPolicy()
        {
            this.QueueSize = DefaultQueueSize;
            this.MaximumQueueSize = int.MaxValue;
            this.MaximumLag = TimeSpan.FromMilliseconds(DefaultMaximumLag);
            this.ThrottleWhenFull = false;
            this.LagEnforcement = LagConstraints.BestEffort;
            this.IsSynchronous = false;
        }

        /// <summary>
        /// Gets a lossy default policy, which is to drop messages once they are too old (older than 100ms), using a shallow queue to amortize CPU spikes
        /// </summary>
        public static DeliveryPolicy Default
        {
            get { return DeliveryPolicy.defaultPolicy; }
        }

        /// <summary>
        /// Gets a lossless throttled delivery policy, which queues everything but throttles callers once the queue becomes too big
        /// </summary>
        public static DeliveryPolicy Throttled
        {
            get { return DeliveryPolicy.throttledPolicy; }
        }

        /// <summary>
        /// Gets a lossless unbounded delivery policy which lets the queue grow as much as needed, with no lag constraints
        /// </summary>
        public static DeliveryPolicy Unlimited
        {
            get { return DeliveryPolicy.unboundedPolicy; }
        }

        /// <summary>
        /// Gets a lossy delivery policy which limits the queue to one message, with no lag constraints
        /// </summary>
        public static DeliveryPolicy LatestMessage
        {
            get { return DeliveryPolicy.latestMessage; }
        }

        /// <summary>
        /// Gets a lossless delivery policy which enforces synchronous message delivery
        /// </summary>
        public static DeliveryPolicy Immediate
        {
            get { return DeliveryPolicy.immediate; }
        }

        /// <summary>
        /// Gets a lossless delivery policy which enforces synchronous message delivery with throttling once queue size (16) is exceeded
        /// </summary>
        public static DeliveryPolicy ImmediateOrThrottle16
        {
            get { return DeliveryPolicy.immediateOrThrottle16; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to block the publisher if the receiver queue is full.
        /// The default is to not block, and drop the oldest message instead.
        /// Use with care, as it affects all other subscribers to the same publisher and can introduce deadlocks (a blocked publisher cannot process control messages anymore).
        /// </summary>
        public bool ThrottleWhenFull { get; set; }

        /// <summary>
        /// Gets or sets the initial size of the queue that holds the messages pending delivery. 0 means used default value.
        /// </summary>
        public int QueueSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the queue that holds the messages pending delivery. 0 means used default value.
        /// </summary>
        public int MaximumQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum age of items waiting to be delivered. Items older than this are discarded.
        /// </summary>
        public TimeSpan MaximumLag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether lag matters or not.
        /// </summary>
        public LagConstraints LagEnforcement { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the delivery should by synchronous.
        /// </summary>
        public bool IsSynchronous { get; set; }

        /// <summary>
        /// Creates a lag-limiting delivery policy. Messages older than the specified maximum lag are dropped.
        /// </summary>
        /// <param name="maximumLag">The maximum tolerable lag</param>
        /// <param name="lagConstraint">One of the LagConstraints options</param>
        /// <returns>A lag-limited policy</returns>
        public static DeliveryPolicy LagConstrained(TimeSpan maximumLag, LagConstraints lagConstraint = LagConstraints.BestEffort)
        {
            return new DeliveryPolicy() { LagEnforcement = lagConstraint, MaximumLag = maximumLag };
        }

        /// <summary>
        /// Converts the string representation of the name to a static delivery policy instance.
        /// </summary>
        /// <param name="value">The string representation of the delivery policy.</param>
        /// <param name="result">If method succeeds, result contains the corresponding delivery policy. If it fails, the value is null.</param>
        /// <returns>Whether the conversion succeeded.</returns>
        public static bool TryParse(string value, out DeliveryPolicy result)
        {
            var success = false;

            switch (value)
            {
                case nameof(Default):
                    result = DeliveryPolicy.Default;
                    success = true;
                    break;

                case nameof(Throttled):
                    result = DeliveryPolicy.Throttled;
                    success = true;
                    break;

                case nameof(Unlimited):
                    result = DeliveryPolicy.Unlimited;
                    success = true;
                    break;

                case nameof(LatestMessage):
                    result = DeliveryPolicy.LatestMessage;
                    success = true;
                    break;

                case nameof(Immediate):
                    result = DeliveryPolicy.Immediate;
                    success = true;
                    break;

                case "ImmediateOrThrottle16":
                    result = DeliveryPolicy.ImmediateOrThrottle16;
                    success = true;
                    break;

                default:
                    result = null;
                    break;
            }

            return success;
        }

        /// <summary>
        /// Combines two delivery policies. Each setting in the resulting policy is set to the stricter of the two parent policies.
        /// </summary>
        /// <param name="other">The policy to combine with</param>
        /// <returns>The new policy</returns>
        public DeliveryPolicy Merge(DeliveryPolicy other)
        {
            var result = new DeliveryPolicy();
            result.LagEnforcement = (LagConstraints)Math.Max((byte)this.LagEnforcement, (byte)other.LagEnforcement);
            result.MaximumLag = TimeSpan.FromTicks(Math.Min(this.MaximumLag.Ticks, other.MaximumLag.Ticks));
            result.MaximumQueueSize = Math.Min(this.MaximumQueueSize, other.MaximumQueueSize);
            result.QueueSize = Math.Min(this.QueueSize, other.QueueSize);
            result.ThrottleWhenFull = this.ThrottleWhenFull || other.ThrottleWhenFull;
            return result;
        }
    }
}
