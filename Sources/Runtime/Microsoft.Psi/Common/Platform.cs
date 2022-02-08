// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;

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

        internal interface IFileHelper
        {
            bool CanOpenFile(string filePath);
        }

        #pragma warning restore SA1600 // Elements must be documented

        public static class Specific
        {
            private static readonly IHighResolutionTime PlatformHighResolutionTime;
            private static readonly IThreading PlatformThreading;
            private static readonly IFileHelper FileHelper;

            static Specific()
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // Windows high-resolution timer APIs (e.g. TimeSetEvent in winmm.dll) are unavaliable on ARM
                    PlatformHighResolutionTime = new Windows.HighResolutionTime();
                    PlatformThreading = new Windows.Threading();
                    FileHelper = new Windows.FileHelper();
                }
                else
                {
                    PlatformHighResolutionTime = new Standard.HighResolutionTime();
                    PlatformThreading = new Standard.Threading();
                    FileHelper = new Standard.FileHelper();
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

            internal static bool CanOpenFile(string filePath)
            {
                return FileHelper.CanOpenFile(filePath);
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
                    uint eventType = (uint)(periodic ? 1 : 0);

                    // TIME_KILL_SYNCHRONOUS flag prevents timer event from occurring after timeKillEvent is called
                    eventType |= 0x100;

                    this.id = NativeMethods.TimeSetEvent(delay, 0, handler, ref ignore, eventType);
                }

                public void Stop()
                {
                    if (NativeMethods.TimeKillEvent(this.id) != 0)
                    {
                        throw new ArgumentException("Invalid timer event ID.");
                    }
                }
            }

            internal sealed class HighResolutionTime : IHighResolutionTime
            {
                private bool isArm = RuntimeInformation.OSArchitecture == Architecture.Arm || RuntimeInformation.OSArchitecture == Architecture.Arm64;

                public long TimeStamp()
                {
                    long time;
                    if (!NativeMethods.QueryPerformanceCounter(out time))
                    {
                        throw new NotImplementedException("QueryPerformanceCounter failed (supported on Windows XP and later)");
                    }

                    return time;
                }

                public long TimeFrequency()
                {
                    long freq;
                    if (!NativeMethods.QueryPerformanceFrequency(out freq))
                    {
                        throw new NotImplementedException("QueryPerformanceFrequency failed (supported on Windows XP and later)");
                    }

                    return freq;
                }

                public long SystemTime()
                {
                    long time;
                    NativeMethods.GetSystemTimePreciseAsFileTime(out time);
                    return time;
                }

                public ITimer TimerStart(uint delay, Time.TimerDelegate handler, bool periodic)
                {
                    return
                        this.isArm ?
                        new Standard.Timer(delay, handler, periodic) : // TimeSet/KillEvent API unavailable on ARM
                        new Timer(delay, handler, periodic);
                }
            }

            internal sealed class Threading : IThreading
            {
                public void SetApartmentState(Thread thread, ApartmentState state)
                {
                    thread.SetApartmentState(state);
                }
            }

            internal sealed class FileHelper : IFileHelper
            {
                public bool CanOpenFile(string filePath)
                {
                    // Try to open the marker file using Win32 api so that we don't
                    // get an exception if the writer still has exclusive access.
                    SafeFileHandle fileHandle = NativeMethods.CreateFile(
                        filePath,
                        0x80000000, // GENERIC_READ
                        (uint)FileShare.Read,
                        IntPtr.Zero,
                        (uint)FileMode.Open,
                        0,
                        IntPtr.Zero);

                    bool canOpenFile = !fileHandle.IsInvalid;
                    fileHandle.Dispose();
                    return canOpenFile;
                }
            }

            private static class NativeMethods
            {
                [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
                internal static extern uint TimeSetEvent(uint delay, uint resolution, Time.TimerDelegate handler, ref uint userCtx, uint eventType);

                [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
                internal static extern uint TimeKillEvent(uint timerEventId);

                [DllImport("kernel32.dll")]
                internal static extern void GetSystemTimePreciseAsFileTime(out long systemTimeAsFileTime);

                [DllImport("kernel32.dll")]
                internal static extern bool QueryPerformanceCounter(out long performanceCount);

                [DllImport("kernel32.dll")]
                internal static extern bool QueryPerformanceFrequency(out long frequency);

                [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                internal static extern SafeFileHandle CreateFile(
                    string fileName,
                    uint desiredAccess,
                    uint shareMode,
                    IntPtr securityAttributes,
                    uint creationDisposition,
                    uint flagsAndAttributes,
                    IntPtr templateFile);
            }
        }

        private static class Standard // Linux, Mac, ...
        {
            internal sealed class Timer : ITimer, IDisposable
            {
                private readonly object timerDelegateLock = new object();
                private System.Timers.Timer timer;

                public Timer(uint delay, Time.TimerDelegate handler, bool periodic)
                {
                    this.timer = new System.Timers.Timer(delay);
                    this.timer.AutoReset = periodic;
                    this.timer.Elapsed += (sender, args) =>
                    {
                        lock (this.timerDelegateLock)
                        {
                            // prevents handler from being called if timer has been stopped
                            if (this.timer != null)
                            {
                                handler(0, 0, UIntPtr.Zero, UIntPtr.Zero, UIntPtr.Zero);
                            }
                        }
                    };
                    this.timer.Start();
                }

                public void Dispose()
                {
                    this.Stop();
                }

                public void Stop()
                {
                    if (this.timer != null)
                    {
                        lock (this.timerDelegateLock)
                        {
                            this.timer.Stop();
                            this.timer.Dispose();
                            this.timer = null;
                        }
                    }
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

            internal sealed class FileHelper : IFileHelper
            {
                public bool CanOpenFile(string filePath)
                {
                    // Encode the file path to a null terminated UTF8 string
                    byte[] encodedBytes = new byte[Encoding.UTF8.GetByteCount(filePath) + 1];
                    Encoding.UTF8.GetBytes(filePath, 0, filePath.Length, encodedBytes, 0);

                    // Try to open the file
                    int fileDescriptor = NativeMethods.Open(encodedBytes, 0);

                    // If a valid file descriptor is returned (not -1), then the file was successfully opened.
                    bool canOpenFile = fileDescriptor > -1;

                    // Close the file if we managed to open it
                    if (canOpenFile)
                    {
                        NativeMethods.Close(fileDescriptor);
                    }

                    return canOpenFile;
                }
            }

            private static class NativeMethods
            {
                [DllImport("libc", SetLastError = true, EntryPoint = "open")]
                public static extern int Open([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)] byte[] fileNameAsUtf8ByteArray, int flags);

                [DllImport("libc", SetLastError = true, EntryPoint = "close")]
                public static extern int Close(int fileDescriptor);
            }
        }
    }
}
