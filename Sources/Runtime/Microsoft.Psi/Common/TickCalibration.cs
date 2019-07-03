// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Provides functionality for synchronizing and calibrating tick values to the system clock.
    /// </summary>
    internal class TickCalibration
    {
        // The performance counter to 100 ns tick conversion factor
        private static double qpcToHns;

        private readonly object syncRoot = new object();

        // The maximum performance counter ticks a QPC sync should take, otherwise it is rejected
        private long qpcSyncPrecision;

        // The maximum amount the QPC clock is allowed to drift since the last calibration
        private long maxClockDrift;

        // The high-water marks for elapsed ticks and system file time
        private long tickHighWaterMark;
        private long fileTimeHighWaterMark;

        // The calibration data array, which will be treated as a circular buffer containing the
        // latest calibration points, up to the specified capacity.
        private CalibrationData[] calibrationData;

        // Head and tail indices for the calibration data array
        private int headIndex;
        private int tailIndex;

        // The current number of calibration points
        private int calibrationCount;

        /// <summary>
        /// Initializes static members of the <see cref="TickCalibration"/> class.
        /// </summary>
        static TickCalibration()
        {
            long qpcFrequency = Platform.Specific.TimeFrequency();
            qpcToHns = 10000000.0 / qpcFrequency;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TickCalibration"/> class.
        /// </summary>
        /// <param name="capacity">The capacity of the calibration data array.</param>
        /// <param name="tickSyncPrecision">The maximum number of 100 ns ticks allowed for a sync operation.</param>
        /// <param name="maxClockDrift">The maximum allowable clock drift which, if exceeded, will update the calibration.</param>
        internal TickCalibration(
            int capacity = 512,
            long tickSyncPrecision = 10,
            long maxClockDrift = 10000)
        {
            this.calibrationData = new CalibrationData[capacity];
            this.calibrationCount = 0;
            this.headIndex = 0;
            this.tailIndex = 0;

            // Convert the sync precision in 100 ns ticks to QPC ticks
            this.qpcSyncPrecision = (long)(tickSyncPrecision / qpcToHns);
            this.maxClockDrift = maxClockDrift;

            // Force an initial calibration
            this.Recalibrate(true);
        }

        /// <summary>
        /// Returns the system file time corresponding to the number of 100ns ticks from system boot.
        /// </summary>
        /// <param name="ticks">The number of 100ns ticks since system boot.</param>
        /// <param name="recalibrate">Recalibrates if necessary before conversion.</param>
        /// <returns>The system file time.</returns>
        public long ConvertToFileTime(long ticks, bool recalibrate = true)
        {
            if (recalibrate)
            {
                // Recalibrate only if clocks have drifted by more than the threshold since the last calibration
                this.Recalibrate();
            }

            lock (this.syncRoot)
            {
                // Find the calibration entry to use for the conversion. Start with the most recent and walk
                // backwards, since we will normally be converting recent tick values going forward in time.
                // Assumes at least one entry, which will be the case due to the initial calibration on construction.
                int calIndex = (this.headIndex + this.calibrationCount - 1) % this.calibrationData.Length;

                // Save the index of the next calibration point (if any) as we may need it for the conversion
                int nextCalIndex = this.tailIndex;

                // Walk the circular buffer beginning with the most recent calibration point until we find
                // the one that applies to the ticks which we are trying to convert.
                while (calIndex != this.headIndex && ticks < this.calibrationData[calIndex].Ticks)
                {
                    nextCalIndex = calIndex;

                    // This just decrements the index in the circular buffer
                    if (--calIndex < 0)
                    {
                        calIndex = this.calibrationData.Length - 1;
                    }
                }

                // Do the conversion using the calibration point we just found.
                long fileTime = this.calibrationData[calIndex].FileTime + (ticks - this.calibrationData[calIndex].Ticks);

                // Clamp the result to the system file time of the following calibration point (if any).
                // This ensures monotonicity of the converted system file times.
                if (nextCalIndex != this.tailIndex && fileTime > this.calibrationData[nextCalIndex].FileTime)
                {
                    fileTime = this.calibrationData[nextCalIndex].FileTime;
                }

                // Bump up the high-water marks to guarantee stability of the current converted value,
                // irrespective of any future calibration adjustment.
                if (ticks > this.tickHighWaterMark)
                {
                    this.tickHighWaterMark = ticks;
                    this.fileTimeHighWaterMark = fileTime;
                }

                return fileTime;
            }
        }

        /// <summary>
        /// Attempts to recalibrate elapsed ticks against the system time. The current elapsed ticks from
        /// the performance counter will be compared against the current system time and the calibration
        /// data will be modified only if it is determined that the times have drifted by more than the
        /// maxmimum allowed amount since the last calibration.
        /// </summary>
        /// <param name="force">Forces the calibration data to be modified regardless of the observed drift.</param>
        internal void Recalibrate(bool force = false)
        {
            long ft, qpc, qpc0;
            do
            {
                // Sync QPC and system time to within the specified precision. In order for the
                // sync to be precise, both calls to get system time and QPC should ideally occur
                // at exactly the same instant, as one atomic operation, to prevent a possible
                // thread context switch which would throw the calibration off. Since that is
                // not possible, we measure the time it took to sync and if that exceeds a maximum,
                // we repeat the process until both calls complete within the time limit.
                qpc0 = Platform.Specific.TimeStamp();
                ft = Platform.Specific.SystemTime();
                qpc = Platform.Specific.TimeStamp();
            }
            while ((qpc - qpc0) > this.qpcSyncPrecision);

            // Convert raw QPC value to 100 ns ticks
            long ticks = (long)(qpcToHns * qpc);

            lock (this.syncRoot)
            {
                // Only recalibrate above the high-water mark
                if (ticks > this.tickHighWaterMark)
                {
                    // Calculate the current time using the most recent calibration data
                    long fileTimeCal = 0;
                    if (this.calibrationCount > 0)
                    {
                        int last = (this.headIndex + this.calibrationCount - 1) % this.calibrationData.Length;
                        fileTimeCal = this.calibrationData[last].FileTime + (ticks - this.calibrationData[last].Ticks);
                    }

                    // Drift is the difference between the observed and calculated current file time
                    long fileTimeDrift = ft - fileTimeCal;
                    long fileTimeDriftAbs = fileTimeDrift < 0 ? -fileTimeDrift : fileTimeDrift;

                    // Add the new calibration data if force is true or max drift exceeded
                    if (force || fileTimeDriftAbs > this.maxClockDrift)
                    {
                        this.AddCalibrationData(ticks, ft);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new calibration data point, adjusted accordingly in order to guarantee stability and monotonicity.
        /// </summary>
        /// <param name="ticks">The elapsed ticks.</param>
        /// <param name="fileTime">The corresponding system file time.</param>
        internal void AddCalibrationData(long ticks, long fileTime)
        {
            // Check that the new calibration data does not overlap with existing calibration data
            int last = (this.headIndex + this.calibrationCount - 1) % this.calibrationData.Length;
            if (this.calibrationCount == 0 || ticks > this.calibrationData[last].Ticks)
            {
                // Once a high-water mark has been established for system file times on the existing calibration
                // data, ensure that the new calibration data does not affect results below this point to preserve
                // stability and monotonicity by shifting the new calibration point forward in time to the point
                // on the line that corresponds to the high-water mark if necessary.
                if (fileTime < this.fileTimeHighWaterMark)
                {
                    ticks = ticks + (this.fileTimeHighWaterMark - fileTime);
                    fileTime = this.fileTimeHighWaterMark;
                }

                // Insert the new calibration data in the circular buffer. The oldest entry will
                // first be removed if the buffer is full.
                this.EnsureCapacity();
                this.calibrationData[this.tailIndex] = new CalibrationData(ticks, fileTime);
                this.tailIndex = (this.tailIndex + 1) % this.calibrationData.Length;
                this.calibrationCount++;
            }
        }

        /// <summary>
        /// Ensures that the calibration buffer has enough space for at least one new entry.
        /// If not, the oldest entry is removed.
        /// </summary>
        private void EnsureCapacity()
        {
            // Check if existing array is full
            if (this.calibrationCount == this.calibrationData.Length)
            {
                // Remove the head (oldest) calibration data
                this.headIndex = (this.headIndex + 1) % this.calibrationData.Length;
                this.calibrationCount--;
            }
        }

        /// <summary>
        /// Defines a single calibration point between elapsed ticks and the system file time.
        /// </summary>
        internal struct CalibrationData
        {
            private long ticks;
            private long fileTime;

            /// <summary>
            /// Initializes a new instance of the <see cref="CalibrationData"/> struct.
            /// </summary>
            /// <param name="ticks">The elapsed ticks.</param>
            /// <param name="fileTime">The system file time.</param>
            public CalibrationData(long ticks, long fileTime)
            {
                this.ticks = ticks;
                this.fileTime = fileTime;
            }

            /// <summary>
            /// Gets the calibration tick value.
            /// </summary>
            public long Ticks => this.ticks;

            /// <summary>
            /// Gets the calibration system file time.
            /// </summary>
            public long FileTime => this.fileTime;
        }
    }
}
