// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    public sealed class ReplayDescriptor
    {
        public static readonly ReplayDescriptor ReplayAll = new ReplayDescriptor(TimeInterval.Infinite, false, false, 1);
        public static readonly ReplayDescriptor ReplayAllRealTime = new ReplayDescriptor(TimeInterval.Infinite);

        private readonly TimeInterval interval;
        private readonly float replaySpeedFactor;
        private readonly bool enforceReplayClock;
        private readonly bool useOriginatingTime;

        public ReplayDescriptor(DateTime start, DateTime end, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
            : this(new TimeInterval(start, end), useOriginatingTime, enforceReplayClock, replaySpeedFactor)
        {
        }

        public ReplayDescriptor(DateTime start, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
            : this(new TimeInterval(start, DateTime.MaxValue), useOriginatingTime, enforceReplayClock, replaySpeedFactor)
        {
        }

        public ReplayDescriptor(TimeInterval interval, bool useOriginatingTime = false, bool enforceReplayClock = true, float replaySpeedFactor = 1)
        {
            this.interval = interval ?? TimeInterval.Infinite;
            this.useOriginatingTime = useOriginatingTime;
            this.replaySpeedFactor = replaySpeedFactor;
            this.enforceReplayClock = enforceReplayClock;
        }

        public TimeInterval Interval => this.interval;

        public DateTime Start => this.Interval.Left;

        public DateTime End => this.Interval.Right;

        public float ReplaySpeedFactor => this.replaySpeedFactor;

        public bool EnforceReplayClock => this.enforceReplayClock;

        public bool UseOriginatingTime => this.useOriginatingTime;

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