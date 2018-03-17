// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Performacne counter collection.
    /// </summary>
    /// <typeparam name="TKey">Performance counter key type.</typeparam>
    public class PerfCounterCollection<TKey> : IPerfCounterCollection<TKey>
        where TKey : struct
    {
        private Dictionary<TKey, PerformanceCounter> counters;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerfCounterCollection{TKey}"/> class.
        /// </summary>
        /// <param name="category">Category name.</param>
        /// <param name="instance">Instance name.</param>
        public PerfCounterCollection(string category, string instance)
        {
            this.counters = PerfCounterManager.AddInstance<TKey>(category, instance);
        }

        /// <summary>
        /// Increment counter.
        /// </summary>
        /// <param name="counter">Counter to increment.</param>
        public void Increment(TKey counter)
        {
            this.counters[counter].Increment();
        }

        /// <summary>
        /// Increment counter by given value.
        /// </summary>
        /// <param name="counter">Counter to increment.</param>
        /// <param name="value">Value by which to increment.</param>
        public void IncrementBy(TKey counter, long value)
        {
            this.counters[counter].IncrementBy(value);
        }

        /// <summary>
        /// Decrement counter.
        /// </summary>
        /// <param name="counter">Counter to decrement.</param>
        public void Decrement(TKey counter)
        {
            this.counters[counter].Decrement();
        }

        /// <summary>
        /// Set counter raw value.
        /// </summary>
        /// <param name="counter">Counter to set.</param>
        /// <param name="value">Raw value.</param>
        public void RawValue(TKey counter, long value)
        {
            this.counters[counter].RawValue = value;
        }

        /// <summary>
        /// Clear collection.
        /// </summary>
        public void Clear()
        {
            foreach (var counter in this.counters.Values)
            {
                counter.RemoveInstance();
            }

            this.counters = null;
        }
    }
}