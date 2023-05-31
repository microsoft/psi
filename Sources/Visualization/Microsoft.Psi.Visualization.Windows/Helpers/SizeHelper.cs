// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System.Collections.Generic;

    /// <summary>
    /// Implements a size formatter for data size objects.
    /// </summary>
    internal static class SizeHelper
    {
        /// <summary>
        /// Formats a data size specified as a long into a string, e.g. 23.1 MB, etc.
        /// </summary>
        /// <param name="size">The size to format.</param>
        /// <returns>A string representation of the data size.</returns>
        public static string FormatSize(double size)
        {
            if (double.IsNaN(size))
            {
                return "N/A";
            }
            else if (size < 1000)
            {
                return $"{size:0} B";
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

        /// <summary>
        /// Formats a latency specified in milliseconds.
        /// </summary>
        /// <param name="latencyMs">The latency to format.</param>
        /// <returns>A string representation of the latency.</returns>
        public static string FormatLatencyMs(double latencyMs)
        {
            if (double.IsNaN(latencyMs))
            {
                return "N/A";
            }
            else if (latencyMs < 1)
            {
                return "<1 ms";
            }
            else
            {
                return $"{latencyMs:0,0 ms}";
            }
        }

        /// <summary>
        /// Formats a data throughout specified as a double into a string, e.g. 23.1 MB, etc.
        /// </summary>
        /// <param name="throughput">The throughput to format.</param>
        /// <param name="timeUnit">The throughput time unit.</param>
        /// <returns>A string representation of the data size.</returns>
        public static string FormatThroughput(double throughput, string timeUnit)
        {
            if (throughput < 1000)
            {
                return $"{throughput:0.0} B/{timeUnit}";
            }
            else if (throughput < 1000000)
            {
                return $"{throughput / 1000.0:0.0} K/{timeUnit}";
            }
            else if (throughput < 1000000000)
            {
                return $"{throughput / 1000000.0:0.0} M/{timeUnit}";
            }
            else if (throughput < 1000000000000)
            {
                return $"{throughput / 1000000000.0:0.0} G/{timeUnit}";
            }
            else
            {
                return $"{throughput / 1000000000000.0:0.0} T/{timeUnit}";
            }
        }

        /// <summary>
        /// Computes the sum of a pair of nullable long instances.
        /// </summary>
        /// <param name="first">The first nullable long instance.</param>
        /// <param name="second">The second nullable long instance.</param>
        /// <returns>The maximum of the pair of nullable long instances.</returns>
        internal static long? NullableSum(long? first, long? second)
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
                return first.Value + second.Value;
            }
        }

        /// <summary>
        /// Computes the sum of an enumerable of nullable long instances.
        /// </summary>
        /// <param name="enumerable">The enumerable of nullable long instances.</param>
        /// <returns>The maximum of the enumerable of nullable long instances.</returns>
        internal static long? NullableSum(IEnumerable<long?> enumerable)
        {
            var result = default(long?);

            foreach (var item in enumerable)
            {
                result = NullableSum(result, item);
            }

            return result;
        }
    }
}
