// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a string formatter for <see cref="DateTime"/> objects.
    /// </summary>
    public static class DateTimeHelper
    {
        private const string DateTimeFormat = "MM/dd/yyyy HH:mm:ss.ffff";
        private const string TimeFormat = "HH:mm:ss.ffff";

        /// <summary>
        /// Formats a nullable datetime object into a string.
        /// </summary>
        /// <param name="dateTime">The nullable datetime object to format.</param>
        /// <param name="renderDateTimeMinMax">If true, then DateTime.MinValue and DateTimeMaxValue are rendered explicitly, otherwise they are rendered as empty strings.</param>
        /// <returns>A string representation of the datetime.</returns>
        public static string FormatDateTime(DateTime? dateTime, bool renderDateTimeMinMax = true)
        {
            if (!dateTime.HasValue || (!renderDateTimeMinMax && (dateTime.Value == DateTime.MinValue || dateTime.Value == DateTime.MaxValue)))
            {
                return string.Empty;
            }

            return dateTime.Value.ToString(DateTimeFormat);
        }

        /// <summary>
        /// Formats a nullable time object into a string.
        /// </summary>
        /// <param name="dateTime">The nullable datetime object to format.</param>
        /// <param name="renderDateTimeMinMax">If true, then DateTime.MinValue and DateTimeMaxValue are rendered explicitly, otherwise they are rendered as empty strings.</param>
        /// <returns>A string representation of the datetime.</returns>
        public static string FormatTime(DateTime? dateTime, bool renderDateTimeMinMax = true)
        {
            if (!dateTime.HasValue || (!renderDateTimeMinMax && (dateTime.Value == DateTime.MinValue || dateTime.Value == DateTime.MaxValue)))
            {
                return string.Empty;
            }

            return dateTime.Value.ToString(TimeFormat);
        }

        /// <summary>
        /// Computes the minimum of a pair of <see cref="Nullable{DateTime}"/> instances.
        /// </summary>
        /// <param name="first">The first <see cref="Nullable{DateTime}"/> instance.</param>
        /// <param name="second">The second <see cref="Nullable{DateTime}"/> instance.</param>
        /// <returns>The minimum of the pair of <see cref="Nullable{DateTime}"/> instances.</returns>
        internal static DateTime? MinDateTime(DateTime? first, DateTime? second)
        {
            if (first == null)
            {
                return second;
            }
            else if (second == null)
            {
                return first;
            }
            else
            {
                return new (Math.Min(first.Value.Ticks, second.Value.Ticks));
            }
        }

        /// <summary>
        /// Computes the minimum of an enumerable of <see cref="Nullable{DateTime}"/> instances.
        /// </summary>
        /// <param name="enumerable">The enumerable of <see cref="Nullable{DateTime}"/> instances.</param>
        /// <returns>The minimum of the enumerable of <see cref="Nullable{DateTime}"/> instances.</returns>
        internal static DateTime? MinDateTime(IEnumerable<DateTime?> enumerable)
        {
            var result = default(DateTime?);

            foreach (var item in enumerable)
            {
                result = MinDateTime(result, item);
            }

            return result;
        }

        /// <summary>
        /// Computes the maximum of a pair of <see cref="Nullable{DateTime}"/> instances.
        /// </summary>
        /// <param name="first">The first <see cref="Nullable{DateTime}"/> instance.</param>
        /// <param name="second">The second <see cref="Nullable{DateTime}"/> instance.</param>
        /// <returns>The maximum of the pair of <see cref="Nullable{DateTime}"/> instances.</returns>
        internal static DateTime? MaxDateTime(DateTime? first, DateTime? second)
        {
            if (first == null)
            {
                return second;
            }
            else if (second == null)
            {
                return first;
            }
            else
            {
                return new (Math.Max(first.Value.Ticks, second.Value.Ticks));
            }
        }

        /// <summary>
        /// Computes the maximum of an enumerable of <see cref="Nullable{DateTime}"/> instances.
        /// </summary>
        /// <param name="enumerable">The enumerable of <see cref="Nullable{DateTime}"/> instances.</param>
        /// <returns>The maximum of the enumerable of <see cref="Nullable{DateTime}"/> instances.</returns>
        internal static DateTime? MaxDateTime(IEnumerable<DateTime?> enumerable)
        {
            var result = default(DateTime?);

            foreach (var item in enumerable)
            {
                result = MaxDateTime(result, item);
            }

            return result;
        }
    }
}
