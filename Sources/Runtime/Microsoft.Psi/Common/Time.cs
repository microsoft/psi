// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Static class providing access and methods to handle absolute time.
    /// </summary>
    internal static class Time
    {
        /// <summary>
        /// Ticks to system file time calibration.
        /// </summary>
        private static TickCalibration tickCalibration = new TickCalibration(
            512,    // max calibration capacity
            10,     // sync precision (1 us)
            10000); // max drift (1 ms)

        /// <summary>
        /// Delegate definition for the API callback.
        /// </summary>
        /// <param name="timerID">The identifier of the timer. The identifier is returned by the timeSetEvent function.</param>
        /// <param name="msg">The parameter is not used.</param>
        /// <param name="userCtx">The value that was specified for the parameter of the timeSetEvent function.</param>
        /// <param name="dw1">The parameter is not used.</param>
        /// <param name="dw2">The parameter is not used.</param>
        internal delegate void TimerDelegate(uint timerID, uint msg, UIntPtr userCtx, UIntPtr dw1, UIntPtr dw2);

        /// <summary>
        /// Returns the current time with high resolution (1us).
        /// </summary>
        /// <returns>The current time.</returns>
        internal static DateTime GetCurrentTime()
        {
            long ft = Platform.Specific.SystemTime();
            return DateTime.FromFileTimeUtc(ft);
        }

        /// <summary>
        /// Returns the absolute time relative to the current time, with high resolution (1us).
        /// </summary>
        /// <param name="offset">An offset from current time.</param>
        /// <returns>The absolute time.</returns>
        internal static DateTime GetTime(TimeSpan offset)
        {
            return GetCurrentTime() + offset;
        }

        /// <summary>
        /// Returns the system UTC time represented by the number of 100ns ticks from system boot.
        /// The tick counter is calibrated against system time to a precision that is determined
        /// by the tickSyncPrecision argument of the <see cref="TickCalibration"/> constructor
        /// (1 microsecond by default). To account for OS system clock adjustments which may cause
        /// the tick counter to drift relative to the system clock, the calibration is repeated
        /// whenever the drift exceeds a predefined maximum (1 millisecond by default).
        /// </summary>
        /// <param name="ticksFromSystemBoot">The number of 100ns ticks since system boot.</param>
        /// <returns>The corresponding system UTC time.</returns>
        internal static DateTime GetTimeFromElapsedTicks(long ticksFromSystemBoot)
        {
            long ft = tickCalibration.ConvertToFileTime(ticksFromSystemBoot);
            return DateTime.FromFileTimeUtc(ft);
        }

        /// <summary>
        /// Wraps the action into a timer delegate that can be used with TimeSetEvent and RunOnce.
        /// The delegate needs to be kept alive (referenced) until the timer is done firing.
        /// </summary>
        /// <param name="handler">The handler to wrap.</param>
        /// <returns>A timer delegate.</returns>
        internal static TimerDelegate MakeTimerDelegate(Action handler)
        {
            return (tid, msg, uctx, dw1, dw2) => handler();
        }

        /// <summary>
        /// Schedules a timer that calls the specified delegate once after the given time has elapsed.
        /// </summary>
        /// <param name="delay">The amount of time to wait before invoking the delegate, rounded to ms.</param>
        /// <param name="handler">The delegate to call once time is up.</param>
        internal static void RunOnce(TimeSpan delay, TimerDelegate handler)
        {
            var ms = (uint)Math.Round(delay.TotalMilliseconds);
            Platform.Specific.TimerStart(ms, handler, false);
        }
    }
}
