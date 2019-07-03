// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Struct representing a summary of data over a time span.
    /// </summary>
    /// <typeparam name="T">The type of the interval data.</typeparam>
    public struct IntervalData<T>
    {
        private T value;
        private T minimum;
        private T maximum;
        private DateTime originatingTime;
        private TimeSpan interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalData{T}"/> struct.
        /// </summary>
        /// <param name="value">The representative value in the range.</param>
        public IntervalData(T value)
            : this(value, value, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalData{T}"/> struct.
        /// </summary>
        /// <param name="value">The representative value in the range.</param>
        /// <param name="originatingTime">The start time of the range.</param>
        public IntervalData(T value, DateTime originatingTime)
            : this(value, value, value, originatingTime)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalData{T}"/> struct.
        /// </summary>
        /// <param name="value">The representative value in the range.</param>
        /// <param name="minimum">The minimum value in the range.</param>
        /// <param name="maximum">The maximum value in the range.</param>
        public IntervalData(T value, T minimum, T maximum)
            : this(value, minimum, maximum, default(DateTime))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalData{T}"/> struct.
        /// </summary>
        /// <param name="value">The representative value in the range.</param>
        /// <param name="minimum">The minimum value in the range.</param>
        /// <param name="maximum">The maximum value in the range.</param>
        /// <param name="originatingTime">The start time of the range.</param>
        public IntervalData(T value, T minimum, T maximum, DateTime originatingTime)
            : this(value, minimum, maximum, originatingTime, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalData{T}"/> struct.
        /// </summary>
        /// <param name="value">The representative value in the range.</param>
        /// <param name="minimum">The minimum value in the range.</param>
        /// <param name="maximum">The maximum value in the range.</param>
        /// <param name="originatingTime">The start time of the range.</param>
        /// <param name="interval">The interval of the range.</param>
        public IntervalData(T value, T minimum, T maximum, DateTime originatingTime, TimeSpan interval)
        {
            this.originatingTime = originatingTime;
            this.interval = interval;
            this.value = value;
            this.minimum = minimum;
            this.maximum = maximum;
        }

        /// <summary>
        /// Gets the end time of the range.
        /// </summary>
        public DateTime EndTime => this.originatingTime + this.interval;

        /// <summary>
        /// Gets the interval of the range.
        /// </summary>
        public TimeSpan Interval => this.interval;

        /// <summary>
        /// Gets the maximum value in the range.
        /// </summary>
        public T Maximum => this.maximum;

        /// <summary>
        /// Gets the minimum value in the range.
        /// </summary>
        public T Minimum => this.minimum;

        /// <summary>
        /// Gets the start time of the range.
        /// </summary>
        public DateTime OriginatingTime => this.originatingTime;

        /// <summary>
        /// Gets the representative value in the range.
        /// </summary>
        public T Value => this.value;

        /// <summary>
        /// Determines whether two instances are equal.
        /// </summary>
        /// <param name="first">The first object to compare.</param>
        /// <param name="second">The object to compare to.</param>
        /// <returns>True if the instances are equal.</returns>
        public static bool operator ==(IntervalData<T> first, IntervalData<T> second)
        {
            return
                (first.originatingTime == second.originatingTime) &&
                (first.interval == second.interval) &&
                EqualityComparer<T>.Default.Equals(first.value, second.value) &&
                EqualityComparer<T>.Default.Equals(first.minimum, second.minimum) &&
                EqualityComparer<T>.Default.Equals(first.maximum, second.maximum);
        }

        /// <summary>
        /// Determines whether two instances are equal.
        /// </summary>
        /// <param name="first">The first object to compare.</param>
        /// <param name="second">The object to compare to.</param>
        /// <returns>True if the instances are equal.</returns>
        public static bool operator !=(IntervalData<T> first, IntervalData<T> second)
        {
            return !(first == second);
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            if (!(other is IntervalData<T>))
            {
                return false;
            }

            return this == (IntervalData<T>)other;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return
                this.originatingTime.GetHashCode() ^
                this.interval.GetHashCode() ^
                (EqualityComparer<T>.Default.Equals(default(T), this.value) ? 0 : this.value.GetHashCode()) ^
                (EqualityComparer<T>.Default.Equals(default(T), this.minimum) ? 0 : this.minimum.GetHashCode()) ^
                (EqualityComparer<T>.Default.Equals(default(T), this.maximum) ? 0 : this.maximum.GetHashCode());
        }
    }
}
