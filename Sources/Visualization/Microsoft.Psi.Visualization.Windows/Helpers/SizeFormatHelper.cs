// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    /// <summary>
    /// Implements a size formatter for data size objects.
    /// </summary>
    internal static class SizeFormatHelper
    {
        /// <summary>
        /// Formats a data size specified as a long into a string, e.g. 23.1 MB, etc.
        /// </summary>
        /// <param name="size">The size to format.</param>
        /// <returns>A string representation of the data size.</returns>
        public static string FormatSize(long size)
        {
            if (size < 1000)
            {
                return $"{size} B";
            }
            else if (size < 1000000)
            {
                return $"{size / 1000.0:0.0} K";
            }
            else if (size < 1000000000)
            {
                return $"{size / 1000000.0:0.0} M";
            }
            else if (size < 1000000000000)
            {
                return $"{size / 1000000000.0:0.0} G";
            }
            else
            {
                return $"{size / 1000000000000.0:0.0} T";
            }
        }
    }
}
