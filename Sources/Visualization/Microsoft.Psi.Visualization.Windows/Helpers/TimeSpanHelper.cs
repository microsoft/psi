// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;

    /// <summary>
    /// Represents a string formatter for <see cref="TimeSpan"/> objects.
    /// </summary>
    public static class TimeSpanHelper
    {
        /// <summary>
        /// Formats a time span as an readable (approximate value).
        /// </summary>
        /// <param name="timeSpan">The timespan object to format.</param>
        /// <returns>A string representation of the timespan.</returns>
        public static string FormatTimeSpanApproximate(TimeSpan timeSpan)
        {
            var result = string.Empty;

            if (timeSpan.Days > 1)
            {
                result += $"{timeSpan.Days} days, ";
            }
            else if (timeSpan.Days == 1)
            {
                result += $"{timeSpan.Days} day, ";
            }

            if (timeSpan.Hours > 1)
            {
                result += $"{timeSpan.Hours} hours, ";
            }
            else if (timeSpan.Hours == 1)
            {
                result += $"{timeSpan.Hours} hour, ";
            }

            if (timeSpan.Days < 1)
            {
                if (timeSpan.Minutes > 1)
                {
                    result += $"{timeSpan.Minutes} minutes, ";
                }
                else if (timeSpan.Minutes == 1)
                {
                    result += $"{timeSpan.Minutes} minute, ";
                }

                if (timeSpan.Hours < 1)
                {
                    if (timeSpan.Seconds > 1 || timeSpan.Seconds == 0)
                    {
                        result += $"{timeSpan.Seconds} seconds, ";
                    }
                    else if (timeSpan.Seconds == 1)
                    {
                        result += $"{timeSpan.Seconds} second, ";
                    }
                }
            }

            return result.EndsWith(", ") ? result.TrimEnd(new char[] { ',', ' ' }) + "." : result;
        }
    }
}
