// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Represents virtual time.
    /// </summary>
    public class Clock
    {
        private readonly TimeSpan offsetInRealTime;
        private readonly DateTime originInRealTime;
        private readonly double virtualTimeDilateFactor;
        private readonly double virtualTimeDilateFactorInverse;

        /// <summary>
        /// Initializes a new instance of the <see cref="Clock"/> class.
        /// </summary>
        /// <param name="virtualTimeOffset">The delta between virtual time and real time. A negative value will result in times in the past, a positive value will result in times in the future.</param>
        /// <param name="timeDilationFactor">if set to a value greater than 1, virtual time passes faster than real time by this factor.</param>
        public Clock(TimeSpan virtualTimeOffset = default(TimeSpan), float timeDilationFactor = 1)
        {
            this.offsetInRealTime = virtualTimeOffset;
            this.originInRealTime = Time.GetCurrentTime();
            this.virtualTimeDilateFactorInverse = timeDilationFactor;
            this.virtualTimeDilateFactor = (timeDilationFactor == 0) ? 0 : 1.0 / timeDilationFactor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Clock"/> class.
        /// </summary>
        /// <param name="virtualNow">The desired current virtual time.</param>
        /// <param name="replaySpeedFactor">if set to a value greater than 1, virtual time passes faster than real time by this factor.</param>
        public Clock(DateTime virtualNow, float replaySpeedFactor = 1)
        {
            var now = Time.GetCurrentTime();
            this.offsetInRealTime = virtualNow - now;
            this.originInRealTime = now;
            this.virtualTimeDilateFactorInverse = replaySpeedFactor;
            this.virtualTimeDilateFactor = (replaySpeedFactor == 0) ? 0 : 1.0 / replaySpeedFactor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Clock"/> class.
        /// </summary>
        /// <param name="clock">Clock from which to take parameters.</param>
        internal Clock(Clock clock)
        {
            this.offsetInRealTime = clock.offsetInRealTime;
            this.originInRealTime = clock.originInRealTime;
            this.virtualTimeDilateFactorInverse = clock.virtualTimeDilateFactorInverse;
            this.virtualTimeDilateFactor = clock.virtualTimeDilateFactor;
        }

        /// <summary>
        /// Gets the origin in real time.
        /// </summary>
        public DateTime RealTimeOrigin => this.originInRealTime;

        /// <summary>
        /// Gets the offset origin in real time.
        /// </summary>
        public DateTime Origin => this.originInRealTime + this.offsetInRealTime;

        /// <summary>
        /// Returns the virtual time with high resolution (1us), in the virtual time frame of reference.
        /// </summary>
        /// <returns>The current time in the adjusted frame of reference.</returns>
        public DateTime GetCurrentTime()
        {
            return this.ToVirtualTime(Time.GetCurrentTime());
        }

        /// <summary>
        /// Returns the absolute time represented by the number of 100ns ticks from system boot.
        /// </summary>
        /// <param name="ticksFromSystemBoot">The number of 100ns ticks since system boot.</param>
        /// <returns>The absolute time.</returns>
        public DateTime GetTimeFromElapsedTicks(long ticksFromSystemBoot)
        {
            return this.ToVirtualTime(Time.GetTimeFromElapsedTicks(ticksFromSystemBoot));
        }

        /// <summary>
        /// Returns the virtual time, given the current time mapping.
        /// </summary>
        /// <param name="realTime">A time in the real time frame.</param>
        /// <returns>The corresponding time in the adjusted frame of reference.</returns>
        public DateTime ToVirtualTime(DateTime realTime)
        {
            return this.originInRealTime + this.ToVirtualTime(realTime - this.originInRealTime) + this.offsetInRealTime;
        }

        /// <summary>
        /// Returns the virtual time span, given a real time span.
        /// </summary>
        /// <param name="realTimeInterval">Real time span.</param>
        /// <returns>Virtual time span.</returns>
        public TimeSpan ToVirtualTime(TimeSpan realTimeInterval)
        {
            return new TimeSpan((long)(realTimeInterval.Ticks * this.virtualTimeDilateFactor));
        }

        /// <summary>
        /// Returns the real time corresponding to the virtual time, given the current time mapping.
        /// </summary>
        /// <param name="virtualTime">A time in the virtual time frame.</param>
        /// <returns>The corresponding time in the real time frame of reference.</returns>
        public DateTime ToRealTime(DateTime virtualTime)
        {
            return this.originInRealTime + this.ToRealTime(virtualTime - this.originInRealTime - this.offsetInRealTime);
        }

        /// <summary>
        /// Returns the real time span, given a virtual time span.
        /// </summary>
        /// <param name="virtualTimeInterval">Virtual time span.</param>
        /// <returns>Real time span.</returns>
        public TimeSpan ToRealTime(TimeSpan virtualTimeInterval)
        {
            return new TimeSpan((long)(virtualTimeInterval.Ticks * this.virtualTimeDilateFactorInverse));
        }
    }
}