// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Static class containing factory methods for IntervalData{T}.
    /// </summary>
    public static class IntervalData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalData{T}"/> struct.
        /// </summary>
        /// <typeparam name="T">The type of the interval data.</typeparam>
        /// <param name="value">The representative value in the range.</param>
        /// <param name="originatingTime">The start time of the range.</param>
        /// <returns>A new instance of the <see cref="IntervalData{T}"/> struct.</returns>
        public static IntervalData<T> Create<T>(T value, DateTime originatingTime)
        {
            return new IntervalData<T>(value, originatingTime);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalData{T}"/> struct.
        /// </summary>
        /// <typeparam name="T">The type of the interval data.</typeparam>
        /// <param name="value">The representative value in the range.</param>
        /// <param name="minimum">The minimum value in the range.</param>
        /// <param name="maximum">The maximum value in the range.</param>
        /// <param name="originatingTime">The start time of the range.</param>
        /// <param name="interval">The interval of the range.</param>
        /// <returns>A new instance of the <see cref="IntervalData{T}"/> struct.</returns>
        public static IntervalData<T> Create<T>(T value, T minimum, T maximum, DateTime originatingTime, TimeSpan? interval = null)
        {
            return new IntervalData<T>(value, minimum, maximum, originatingTime, interval ?? TimeSpan.Zero);
        }
    }
}
