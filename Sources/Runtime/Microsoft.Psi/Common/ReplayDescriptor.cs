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
        public static readonly ReplayDescriptor ReplayAll = new ReplayDescriptor(TimeInterval.Infinite, false, false, 1);

        /// <summary>
        /// Replay all messages in real time (preserving originating time and enforcing replay clock).
        /// </summary>
        public static readonly ReplayDescriptor ReplayAllRealTime = new ReplayDescriptor(TimeInterval.Infinite);

        private readonly TimeInterval interval;
        private readonly float replaySpeedFactor;
        private readonly bool enforceReplayClock;
        private readonly bool useOriginatingTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayDescriptor"/> class.
        /// </summary>
        /// <param name="start">Starting message time.</param>
        /// <param name="end">Ending message time.</param>
        /// <param name="useOriginatingTime">Whether to use originating time.</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock.</param>
        /// <param name="replaySpeedFactor">Speed factor by which to replay (e.g. 2 for double-speed, 0.5 for half-speed).</param>
        public ReplayDescriptor(DateTime start, DateTime end, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
            : this(new TimeInterval(start, end), useOriginatingTime, enforceReplayClock, replaySpeedFactor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayDescriptor"/> class.
        /// </summary>
        /// <remarks>No ending message time (infinite).</remarks>
        /// <param name="start">Starting message time.</param>
        /// <param name="useOriginatingTime">Whether to use originating time (optional).</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock (optional).</param>
        /// <param name="replaySpeedFactor">Speed factor by which to replay (optional, e.g. 2 for double-speed, 0.5 for half-speed).</param>
        public ReplayDescriptor(DateTime start, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
            : this(new TimeInterval(start, DateTime.MaxValue), useOriginatingTime, enforceReplayClock, replaySpeedFactor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayDescriptor"/> class.
        /// </summary>
        /// <param name="interval">Time interval to replay.</param>
        /// <param name="useOriginatingTime">Whether to use originating time (optional).</param>
        /// <param name="enforceReplayClock">Whether to enforce replay clock (optional).</param>
        /// <param name="replaySpeedFactor">Speed factor by which to replay (optional, e.g. 2 for double-speed, 0.5 for half-speed).</param>
        public ReplayDescriptor(TimeInterval interval, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            this.interval = interval ?? TimeInterval.Infinite;
            this.useOriginatingTime = useOriginatingTime;
            this.replaySpeedFactor = replaySpeedFactor;
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
        /// Gets speed factor by which to replay (e.g. 2 for double-speed, 0.5 for half-speed).
        /// </summary>
        public float ReplaySpeedFactor => this.replaySpeedFactor;

        /// <summary>
        /// Gets a value indicating whether to enforce replay clock.
        /// </summary>
        public bool EnforceReplayClock => this.enforceReplayClock;

        /// <summary>
        /// Gets a value indicating whether to use originating time.
        /// </summary>
        public bool UseOriginatingTime => this.useOriginatingTime;

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

            return new ReplayDescriptor(start, end, this.useOriginatingTime, this.enforceReplayClock, this.replaySpeedFactor);
        }
    }
}