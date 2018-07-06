// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// Internal class to hold native P/Invoke methods.
    /// </summary>
    internal static class Platform
    {
        #pragma warning disable SA1600 // Elements must be documented

        internal interface ITimer
        {
            void Stop();
        }

        internal interface IHighResolutionTime
        {
            long TimeStamp();

            long TimeFrequency();

            long SystemTime();

            ITimer TimerStart(uint delay, Time.TimerDelegate handler, bool periodic);
        }

        internal interface IThreading
        {
            void SetApartmentState(Thread thread, ApartmentState state);
        }

        #pragma warning restore SA1600 // Elements must be documented

        public static class Specific
        {
            private static readonly IHighResolutionTime PlatformHighResolutionTime;
            private static readonly IThreading PlatformThreading;

            static Specific()
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    PlatformHighResolutionTime = new Windows.HighResolutionTime();
                    PlatformThreading = new Windows.Threading();
                }
                else
                {
                    PlatformHighResolutionTime = new Standard.HighResolutionTime();
                    PlatformThreading = new Standard.Threading();
                }
            }

            public static long SystemTime()
            {
                return PlatformHighResolutionTime.SystemTime();
            }

            internal static ITimer TimerStart(uint delay, Time.TimerDelegate handler, bool periodic = true)
            {
                return PlatformHighResolutionTime.TimerStart(delay, handler, periodic);
            }

            internal static void SetApartmentState(Thread thread, ApartmentState state)
            {
                PlatformThreading.SetApartmentState(thread, state);
            }

            internal static long TimeStamp()
            {
                return PlatformHighResolutionTime.TimeStamp();
            }

            internal static long TimeFrequency()
            {
                return PlatformHighResolutionTime.TimeFrequency();
            }
        }

        private static class Windows
        {
            internal sealed class Timer : ITimer
            {
                private readonly uint id;

                public Timer(uint delay, Time.TimerDelegate handler, bool periodic)
                {
                    uint ignore = 0;
                    this.id = TimeSetEvent(delay, 0, handler, ref ignore, (uint)(periodic ? 1 : 0));
                }

                public void Stop()
                {
                    if (TimeKillEvent(this.id) != 0)
                    {
                        throw new ArgumentException("Invalid timer event ID.");
                    }
                }

                [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
                private static extern uint TimeSetEvent(uint delay, uint resolution, Time.TimerDelegate handler, ref uint userCtx, uint eventType);

                [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
                private static extern uint TimeKillEvent(uint timerEventId);
            }

            internal sealed class HighResolutionTime : IHighResolutionTime
            {
                public long TimeStamp()
                {
                    long time;
                    if (!QueryPerformanceCounter(out time))
                    {
                        throw new NotImplementedException("QueryPerformanceCounter failed (supported on Windows XP and later)");
                    }

                    return time;
                }

                public long TimeFrequency()
                {
                    long freq;
                    if (!QueryPerformanceFrequency(out freq))
                    {
                        throw new NotImplementedException("QueryPerformanceFrequency failed (supported on Windows XP and later)");
                    }

                    return freq;
                }

                public long SystemTime()
                {
                    long time;
                    GetSystemTimePreciseAsFileTime(out time);
                    return time;
                }

                public ITimer TimerStart(uint delay, Time.TimerDelegate handler, bool periodic)
                {
                    return new Timer(delay, handler, periodic);
                }

                [DllImport("kernel32.dll")]
                private static extern void GetSystemTimePreciseAsFileTime(out long systemTimeAsFileTime);

                [DllImport("kernel32.dll")]
                private static extern bool QueryPerformanceCounter(out long performanceCount);

                [DllImport("kernel32.dll")]
                private static extern bool QueryPerformanceFrequency(out long frequency);
            }

            internal sealed class Threading : IThreading
            {
                public void SetApartmentState(Thread thread, ApartmentState state)
                {
                    thread.SetApartmentState(state);
                }
            }
        }

        private static class Standard // Linux, Mac, ...
        {
            internal sealed class Timer : ITimer
            {
                private System.Timers.Timer timer;

                public Timer(uint delay, Time.TimerDelegate handler, bool periodic)
                {
                    this.timer = new System.Timers.Timer(delay);
                    this.timer.AutoReset = periodic;
                    this.timer.Elapsed += (sender, args) =>
                    {
                        lock (this.timer)
                        {
                            handler(0, 0, UIntPtr.Zero, UIntPtr.Zero, UIntPtr.Zero);
                        }
                    };
                    this.timer.Start();
                }

                public void Stop()
                {
                    this.timer?.Stop();
                    this.timer?.Dispose();
                }

                private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
                {
                    throw new NotImplementedException();
                }
            }

            internal sealed class HighResolutionTime : IHighResolutionTime
            {
                private static readonly decimal TicksPerStopwatch = (decimal)TimeSpan.TicksPerSecond / (decimal)Stopwatch.Frequency;
                private static readonly long ResetAfterSwTicks = TimeSpan.FromMinutes(1).Ticks; // account for stopwatch drift
                private static long lastTicks = 0;
                private static long systemTimeStart = DateTime.UtcNow.ToFileTimeUtc();
                private static long stopwatchStart = Stopwatch.GetTimestamp();

                public long TimeStamp()
                {
                    // http://aakinshin.net/blog/post/stopwatch/
                    // http://referencesource.microsoft.com/#System/services/monitoring/system/diagnosticts/Stopwatch.cs,135
                    return Stopwatch.GetTimestamp();
                }

                public long TimeFrequency()
                {
                    // http://aakinshin.net/blog/post/stopwatch/
                    // http://referencesource.microsoft.com/#System/services/monitoring/system/diagnosticts/Stopwatch.cs,135
                    return Stopwatch.Frequency;
                }

                public long SystemTime()
                {
                    var stamp = Stopwatch.GetTimestamp();
                    var elapsed = (long)(((decimal)(stamp - stopwatchStart) * TicksPerStopwatch) + 0.5m);
                    if (elapsed > ResetAfterSwTicks)
                    {
                        do
                        {
                            systemTimeStart = DateTime.UtcNow.ToFileTimeUtc();
                            stopwatchStart = Stopwatch.GetTimestamp();
                            if (systemTimeStart <= lastTicks)
                            {
                                // let reality catch up
                                // experiments have shown that in practice with 1 minute resets, the drift averages to ~4500 ticks (< 1/2 millisecond)
                                Thread.Sleep(TimeSpan.FromTicks(lastTicks - systemTimeStart));
                            }
                        }
                        while (systemTimeStart <= lastTicks);

                        lastTicks = systemTimeStart;
                    }
                    else
                    {
                        lastTicks = systemTimeStart + elapsed;
                    }

                    return lastTicks;
                }

                public ITimer TimerStart(uint delay, Time.TimerDelegate handler, bool periodic)
                {
                    return new Timer(delay, handler, periodic);
                }

                public void TimerStop(ITimer timer)
                {
                    timer.Stop();
                }
            }

            internal sealed class Threading : IThreading
            {
                public void SetApartmentState(Thread thread, ApartmentState state)
                {
                    // do nothing (COM feature)
                }
            }
        }
    }
}
