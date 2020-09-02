// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;

    /// <summary>
    /// Represents a string formatter for datetime objects.
    /// </summary>
    public static class DateTimeFormatHelper
    {
        private const string DateTimeFormat = "MM/dd/yyyy HH:mm:ss.ffff";

        /// <summary>
        /// Formats a nullable datetime object into a string.
        /// </summary>
        /// <param name="dateTime">The nullable datetime object to format.</param>
        /// <returns>A string representation of the datetime.</returns>
        public static string FormatDateTime(DateTime? dateTime)
        {
            return dateTime.HasValue ? dateTime.Value.ToString(DateTimeFormat) : string.Empty;
        }
    }
}
