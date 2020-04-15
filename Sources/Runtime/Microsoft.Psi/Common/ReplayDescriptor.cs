// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Descriptor for pipeline replay.
    /// </summary>
    public sealed class ReplayDescriptor
    {
        /// <summary>
        /// Replay all messages (not in real time, disregarding originating time and not enforcing replay clock).
        /// </summary>
        public static readonly ReplayDescriptor ReplayAll = new ReplayDescriptor(TimeInterval.Infinite, false);

        /// <summary>
        /// Replay all messages in real time (preserving originating time and enforcing replay clock).
        /// </summary>
        public static readonly ReplayDescriptor ReplayAllRealTime = new ReplayDescriptor(TimeInterval.Infinite, true);

        private readonly TimeInterval interval;
        private readonly bool enforceReplayClock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayDescriptor"/> class.
        /// </summary>
        /// <param name="start">Starting message time.</param>
        /// <param name="end">Ending message time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        public ReplayDescriptor(DateTime start, DateTime end, bool enforceReplayClock = true)
            : this(new TimeInterval(start, end), enforceReplayClock)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayDescriptor"/> class.
        /// </summary>
        /// <remarks>No ending message time (infinite).</remarks>
        /// <param name="start">Starting message time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock (optional).</param>
        public ReplayDescriptor(DateTime start, bool enforceReplayClock = true)
            : this(new TimeInterval(start, DateTime.MaxValue), enforceReplayClock)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayDescriptor"/> class.
        /// </summary>
        /// <param name="interval">Time interval to replay.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        public ReplayDescriptor(TimeInterval interval, bool enforceReplayClock = true)
        {
            this.interval = interval ?? TimeInterval.Infinite;
            this.enforceReplayClock = enforceReplayClock;
        }

        /// <summary>
        /// Gets time interval to replay.
        /// </summary>
        public TimeInterval Interval => this.interval;

        /// <summary>
        /// Gets starting message time.
        /// </summary>
        public DateTime Start => this.Interval.Left;

        /// <summary>
        /// Gets ending message time.
        /// </summary>
        public DateTime End => this.Interval.Right;

        /// <summary>
        /// Gets a value indicating whether to enforce replay clock.
        /// </summary>
        public bool EnforceReplayClock => this.enforceReplayClock;

        /// <summary>
        /// Reduce this replay descriptor to that which intersects the given time interval.
        /// </summary>
        /// <param name="interval">Intersecting time interval.</param>
        /// <returns>Reduced replay descriptor.</returns>
        public ReplayDescriptor Intersect(TimeInterval interval)
        {
            if (interval == null)
            {
                return this;
            }

            DateTime start = new DateTime(Math.Max(this.interval.Left.Ticks, interval.Left.Ticks));
            DateTime end = new DateTime(Math.Min(this.interval.Right.Ticks, interval.Right.Ticks));
            if (end < start)
            {
                end = start;
            }

            return new ReplayDescriptor(start, end, this.enforceReplayClock);
        }
    }
}