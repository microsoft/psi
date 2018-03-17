// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Represents a performance counter collection.
    /// </summary>
    /// <typeparam name="TKey">Performance counter key type.</typeparam>
    public interface IPerfCounterCollection<TKey>
        where TKey : struct
    {
        /// <summary>
        /// Increment counter.
        /// </summary>
        /// <param name="counter">Counter to increment.</param>
        void Increment(TKey counter);

        /// <summary>
        /// Increment counter by given value.
        /// </summary>
        /// <param name="counter">Counter to increment.</param>
        /// <param name="value">Value by which to increment.</param>
        void IncrementBy(TKey counter, long value);

        /// <summary>
        /// Decrement counter.
        /// </summary>
        /// <param name="counter">Counter to decrement.</param>
        void Decrement(TKey counter);

        /// <summary>
        /// Set counter raw value.
        /// </summary>
        /// <param name="counter">Counter to set.</param>
        /// <param name="value">Raw value.</param>
        void RawValue(TKey counter, long value);

        /// <summary>
        /// Clear collection.
        /// </summary>
        void Clear();
    }
}
