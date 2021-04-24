// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements various helper methods for string processing.
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Returns a delimiter-separated string representation for an enumeration of objects.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the enumeration.</typeparam>
        /// <param name="enumerable">The enumeration to convert to a string.</param>
        /// <param name="delimiter">The delimiter to use.</param>
        /// <returns>A delimiter-separated string representation for the enumeration.</returns>
        public static string EnumerableToString<T>(this IEnumerable<T> enumerable, string delimiter)
        {
            var result = string.Empty;
            if (enumerable == null || !enumerable.Any())
            {
                return result;
            }

            result = enumerable.First().ToString();

            foreach (var item in enumerable.Skip(1))
            {
                result += delimiter + item.ToString();
            }

            return result;
        }
    }
}
