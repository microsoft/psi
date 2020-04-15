// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;
    using Microsoft.Psi.Visualization.Common;

    /// <summary>
    /// Represents helper methods for psi indices.
    /// </summary>
    public static class IndexHelper
    {
        /// <summary>
        /// Gets the index id corresponding to a specified time for
        /// a collection of indices that are ordered by time.
        /// </summary>
        /// <param name="currentTime">The time to find the index for.</param>
        /// <param name="count">The number of items in the index collection.</param>
        /// <param name="timeAtIndex">A function that returns the time for a given index id.</param>
        /// <returns>The index id closest to the specified time, using the specified interpolation style.</returns>
        public static int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            return GetIndexForTime(currentTime, count, timeAtIndex, InterpolationStyle.Direct);
        }

        /// <summary>
        /// Gets the index id corresponding to a specified time for
        /// a collection of indices that are ordered by time.
        /// </summary>
        /// <param name="currentTime">The time to find the index for.</param>
        /// <param name="count">The number of items in the index collection.</param>
        /// <param name="timeAtIndex">A function that returns the time for a given index id.</param>
        /// <param name="interpolationStyle">The type of interpolation (Direct or Step) to use when resolving indices that don't exactly lie at the specified time.</param>
        /// <returns>The index id closest to the specified time, using the specified interpolation style.</returns>
        public static int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex, InterpolationStyle interpolationStyle)
        {
            // Perform a binary search
            SearchResult result = SearchIndex(currentTime, count, timeAtIndex);
            if (result.ExactMatchFound)
            {
                return result.ExactIndex;
            }

            // If no exact match, lo and hi indicate ticks that
            // are right before and right after the time we're looking for.
            // If we're using Step interpolation, then we should return
            // lo, otherwise we should return whichever value is closest
            if (interpolationStyle == InterpolationStyle.Step)
            {
                return result.LowIndex;
            }

            // If the're only one point in the index, then return its index
            if (count == 1)
            {
                return 0;
            }

            // Return the index of whichever point is closest to the current time
            if ((timeAtIndex(result.HighIndex) - currentTime) < (currentTime - timeAtIndex(result.LowIndex)))
            {
                return result.HighIndex;
            }
            else
            {
                return result.LowIndex;
            }
        }

        /// <summary>
        /// Gets the index id corresponding to a specified time for
        /// a collection of indices that are ordered by time.
        /// </summary>
        /// <param name="currentTime">The time to find the index for.</param>
        /// <param name="count">The number of items in the index collection.</param>
        /// <param name="timeAtIndex">A function that returns the time for a given index id.</param>
        /// <param name="cursorEpsilon">The cursor epsilon to use to determine if an index is close enough to the specified time to be counted as a match.</param>
        /// <returns>The index id closest to the specified time, using the specified interpolation style.</returns>
        public static int GetIndexForTime(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex, RelativeTimeInterval cursorEpsilon)
        {
            // Perform a binary search
            SearchResult result = SearchIndex(currentTime, count, timeAtIndex);
            if (result.ExactMatchFound)
            {
                return result.ExactIndex;
            }

            TimeInterval interval = currentTime + cursorEpsilon;

            // If there's only one entry in the index, return it if it's within the epsilon
            if (count == 1)
            {
                return interval.PointIsWithin(timeAtIndex(0)) ? 0 : -1;
            }

            // Check if the high index is closer to the current time
            if ((timeAtIndex(result.HighIndex) - currentTime) < (currentTime - timeAtIndex(result.LowIndex)))
            {
                if (interval.PointIsWithin(timeAtIndex(result.HighIndex)))
                {
                    return result.HighIndex;
                }
                else if (interval.PointIsWithin(timeAtIndex(result.LowIndex)))
                {
                    return result.LowIndex;
                }
                else
                {
                    return -1;
                }
            }

            // Check if the low index is closer to the current time
            else
            {
                if (interval.PointIsWithin(timeAtIndex(result.LowIndex)))
                {
                    return result.LowIndex;
                }
                else if (interval.PointIsWithin(timeAtIndex(result.HighIndex)))
                {
                    return result.HighIndex;
                }
                else
                {
                    return -1;
                }
            }
        }

        private static SearchResult SearchIndex(DateTime currentTime, int count, Func<int, DateTime> timeAtIndex)
        {
            if (count == 0)
            {
                return new SearchResult() { ExactMatchFound = true, ExactIndex = -1 };
            }

            // do a binary search and return if exact match
            int lo = 0;
            int hi = count - 1;
            while ((lo != hi - 1) && (lo != hi))
            {
                var val = (lo + hi) / 2;
                if (timeAtIndex(val) < currentTime)
                {
                    lo = val;
                }
                else if (timeAtIndex(val) > currentTime)
                {
                    hi = val;
                }
                else
                {
                    return new SearchResult() { ExactMatchFound = true, ExactIndex = val };
                }
            }

            return new SearchResult() { ExactMatchFound = false, LowIndex = lo, HighIndex = hi };
        }

        private struct SearchResult
        {
            public bool ExactMatchFound { get; set; }

            public int ExactIndex { get; set; }

            public int LowIndex { get; set; }

            public int HighIndex { get; set; }
        }
    }
}
