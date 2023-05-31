// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;

    /// <summary>
    /// Represents helper methods for psi indices.
    /// </summary>
    public static class IndexHelper
    {
        /// <summary>
        /// Gets the index corresponding to a specified time for a collection of indices that are ordered by time.
        /// </summary>
        /// <param name="dateTime">The time to find the index for.</param>
        /// <param name="count">The number of items in the collection.</param>
        /// <param name="timeAtIndex">A function that returns the time for a given index.</param>
        /// <param name="nearestType">Specifies whether to look for nearest, next, or previous index.</param>
        /// <returns>The index id closest to the specified time, using the specified interpolation style.</returns>
        public static int GetIndexForTime(DateTime dateTime, int count, Func<int, DateTime> timeAtIndex, NearestType nearestType = NearestType.Nearest)
        {
            // If there's only one point in the index, then return its index
            if (count == 1)
            {
                return 0;
            }

            // Perform a binary search
            // If no exact match, lo and hi indicate ticks that
            // are right before and right after the time we're looking for.
            SearchResult result = SearchIndex(dateTime, count, timeAtIndex);

            if (result.ExactMatchFound)
            {
                return result.ExactIndex;
            }

            return nearestType switch
            {
                NearestType.Previous => result.LowIndex,
                NearestType.Next => result.HighIndex,

                // o/w return the index of whichever point is closest to the current time
                _ => (timeAtIndex(result.HighIndex) - dateTime) < (dateTime - timeAtIndex(result.LowIndex)) ? result.HighIndex : result.LowIndex,
            };
        }

        /// <summary>
        /// Gets the index id corresponding to a specified time for
        /// a collection of indices that are ordered by time.
        /// </summary>
        /// <param name="dateTime">The time to find the index for.</param>
        /// <param name="epsilonTimeInterval">The epsilon interval used to determine if an index is close enough to the specified time to be counted as a match.</param>
        /// <param name="count">The number of items in the index collection.</param>
        /// <param name="timeAtIndex">A function that returns the time for a given index id.</param>
        /// <returns>The index id closest to the specified time, using the specified interpolation style.</returns>
        public static int GetIndexForTime(DateTime dateTime, RelativeTimeInterval epsilonTimeInterval, int count, Func<int, DateTime> timeAtIndex)
        {
            // Perform a binary search
            SearchResult result = SearchIndex(dateTime, count, timeAtIndex);
            if (result.ExactMatchFound)
            {
                return result.ExactIndex;
            }

            TimeInterval interval = dateTime + epsilonTimeInterval;

            // If there's only one entry in the index, return it if it's within the epsilon
            if (count == 1)
            {
                return interval.PointIsWithin(timeAtIndex(0)) ? 0 : -1;
            }

            // Check if the high index is closer to the current time
            if ((timeAtIndex(result.HighIndex) - dateTime) < (dateTime - timeAtIndex(result.LowIndex)))
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

        private static SearchResult SearchIndex(DateTime dateTime, int count, Func<int, DateTime> timeAtIndex)
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
                if (timeAtIndex(val) < dateTime)
                {
                    lo = val;
                }
                else if (timeAtIndex(val) > dateTime)
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
