// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements helper methods related to <see cref="TimeInterval"/> objects.
    /// </summary>
    public static class TimeIntervalHelper
    {
        /// <summary>
        /// Computes the set of necessary time intervals to cover a specified time interval, given a preexisting set of time intervals.
        /// </summary>
        /// <param name="startTime">The start time of the interval to cover.</param>
        /// <param name="endTime">The end time of the interval to cover.</param>
        /// <param name="existingIntervals">The set of existing intervals.</param>
        /// <returns>The set of necessary intervals to cover the specified time interval.</returns>
        /// <remarks>
        /// This function was primarily constructed to enable determining which time-interval reads should
        /// happen to cover a desired time-interval, given that a number of other reads are already in
        /// progress.
        /// </remarks>
        internal static List<(DateTime StartTime, DateTime EndTime)> ComputeRemainingIntervals(
            DateTime startTime,
            DateTime endTime,
            IEnumerable<(DateTime StartTime, DateTime EndTime)> existingIntervals)
        {
            if (!existingIntervals.Any())
            {
                return new List<(DateTime Start, DateTime End)>() { (startTime, endTime) };
            }

            var (firstIntervalStartTime, firstIntervalEndTime) = existingIntervals.First();

            // If the existing interval is before or after the requested interval
            if (firstIntervalEndTime < startTime || firstIntervalStartTime > endTime)
            {
                // Then simply skip it and compute the remaining intervals on the rest of the
                // existing intervals.
                return ComputeRemainingIntervals(startTime, endTime, existingIntervals.Skip(1));
            }

            // O/w if the existing request completely overlaps the current region of interest,
            // i.e, is larger than it, i.e., starts earlier and ends later
            else if (firstIntervalStartTime <= startTime && firstIntervalEndTime >= endTime)
            {
                // Then we are done and we don't need to read anything else since the
                // existing internval is already covering the requested interval
                return new List<(DateTime StartTime, DateTime EndTime)>();
            }

            // O/w if the existing interval starts before the requested startTime (which b/c
            // we are not in the condition above also means that it ends before the
            // specified endTime)
            else if (firstIntervalStartTime <= startTime)
            {
                // Then we don't need to cover the portion between the start time of the
                // existing interval and the current specified startTime, since that region
                // is already handled by the existing interval. We simply move the
                // startTime of interest to the end time of the existing request.
                return ComputeRemainingIntervals(firstIntervalEndTime, endTime, existingIntervals.Skip(1));
            }

            // O/w if the existing interval ends after the requested endTime (which b/c
            // we are not in the condition above also means that it starts after the
            // specified startTime)
            else if (firstIntervalEndTime >= endTime)
            {
                // Then we don't need to cover the portion between the end time of the
                // existing interval and the current specified endTime, since that region
                // is already handled by the existing interval. We simply move the
                // endTime of interest to the start time of the existing request.
                return ComputeRemainingIntervals(startTime, firstIntervalStartTime, existingIntervals.Skip(1));
            }

            // O/w we have an overlap but the existing interval only covers a portion
            // of the requested time interval, i.e., it starts after the requested startTime
            // and ends before the requested endTime
            else
            {
                // compute read requests for first new range
                var result = ComputeRemainingIntervals(startTime, firstIntervalStartTime, existingIntervals.Skip(1));
                result.AddRange(ComputeRemainingIntervals(firstIntervalEndTime, endTime, existingIntervals.Skip(1)));
                return result;
            }
        }
    }
}
