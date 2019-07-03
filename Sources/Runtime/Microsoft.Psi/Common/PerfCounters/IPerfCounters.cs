// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Specifies the formula used to update the performance counters.
    /// </summary>
    public enum PerfCounterType
    {
        /// <summary>
        /// A difference counter that shows the average number of operations completed during each second of the sample interval. Counters of this type measure time in ticks of the system clock.
        /// </summary>
        RateOfCountsPerSecond32,

        /// <summary>
        /// An instantaneous counter that shows the most recently observed value. Used, for example, to maintain a simple count of items or operations.
        /// </summary>
        NumberOfItems32,

        /// <summary>
        /// An average counter that shows how many items are processed, on average, during an operation. Counters of this type display a ratio of the items processed to the number of operations
        /// completed. The ratio is calculated by comparing the number of items processed during the last interval to the number of operations completed during the last interval.
        /// </summary>
        AverageCount64,

        /// <summary>
        /// A base counter that is used in the calculation of time or count averages, such as <see cref="AverageCount64"/>. Stores the denominator for calculating a counter to present
        /// "time per operation" or "count per operation".
        /// </summary>
        AverageBase,
    }

    /// <summary>
    /// Represents methods for creating and updating the performance counters.
    /// </summary>
    /// <typeparam name="TKey">Performance counter key type.</typeparam>
    public interface IPerfCounters<TKey>
        where TKey : struct
    {
        /// <summary>
        /// Enable performance counters.
        /// </summary>
        /// <param name="category">Category name.</param>
        /// <param name="instance">Instance name.</param>
        /// <returns>Performance counter collection.</returns>
        IPerfCounterCollection<TKey> Enable(string category, string instance);

        /// <summary>
        /// Add performance counter definitions.
        /// </summary>
        /// <param name="category">Category name.</param>
        /// <param name="definitions">Performance counter definitions (key, name, help, type).</param>
        void AddCounterDefinitions(string category, IEnumerable<Tuple<TKey, string, string, PerfCounterType>> definitions);
    }
}
