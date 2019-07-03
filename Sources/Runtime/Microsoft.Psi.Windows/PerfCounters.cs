// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Provides methods for creating and updating the performance counters.
    /// </summary>
    /// <typeparam name="TKey">Performance counter key type.</typeparam>
    public class PerfCounters<TKey> : IPerfCounters<TKey>
        where TKey : struct
    {
        /// <summary>
        /// Enable performance counters.
        /// </summary>
        /// <param name="category">Category name.</param>
        /// <param name="instance">Instance name.</param>
        /// <returns>Performance counter collection.</returns>
        public IPerfCounterCollection<TKey> Enable(string category, string instance)
        {
            return new PerfCounterCollection<TKey>(category, instance);
        }

        /// <summary>
        /// Add performance counter defintions.
        /// </summary>
        /// <param name="category">Category name.</param>
        /// <param name="definitions">Performance counter definitions (key, name, help, type).</param>
        public void AddCounterDefinitions(string category, IEnumerable<Tuple<TKey, string, string, PerfCounterType>> definitions)
        {
            Dictionary<int, CounterCreationData> counterDefinitions = new Dictionary<int, CounterCreationData>();
            foreach (var def in definitions)
            {
                var key = def.Item1;
                var name = def.Item2;
                var help = def.Item3;
                var type = this.ConcretePerfCounterType(def.Item4);
                counterDefinitions[(int)(object)key] = new CounterCreationData(name, help, type);
            }

            PerfCounterManager.AddCounterDefinitions(category, counterDefinitions);
        }

        private PerformanceCounterType ConcretePerfCounterType(PerfCounterType type)
        {
            switch (type)
            {
                case PerfCounterType.NumberOfItems32: return PerformanceCounterType.NumberOfItems32;
                case PerfCounterType.RateOfCountsPerSecond32: return PerformanceCounterType.RateOfCountsPerSecond32;
                case PerfCounterType.AverageCount64: return PerformanceCounterType.AverageCount64;
                case PerfCounterType.AverageBase: return PerformanceCounterType.AverageBase;
                default: throw new ArgumentException($"Unexpected performance counter type: {type}");
            }
        }
    }
}
