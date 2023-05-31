// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    /// <summary>
    /// Represents a value range.
    /// </summary>
    /// <typeparam name="T">The type of data in the range.</typeparam>
    public class ValueRange<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueRange{T}"/> class.
        /// </summary>
        /// <param name="minimum">The minimum value of the range.</param>
        /// <param name="maximum">The maximum value of the range.</param>
        public ValueRange(T minimum, T maximum)
        {
            this.Minimum = minimum;
            this.Maximum = maximum;
        }

        /// <summary>
        /// Gets the minimum value of the range.
        /// </summary>
        public T Minimum { get; private set; }

        /// <summary>
        /// Gets the maximum value of the range.
        /// </summary>
        public T Maximum { get; private set; }

        /// <summary>
        /// Equality comparer. Returns true if the two ranges have the same start and end, false otherwise.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <returns>True if the two ranges have the same start and end, false otherwise.</returns>
        public static bool operator ==(ValueRange<T> first, ValueRange<T> second)
        {
            if (first is null && second is null)
            {
                return true;
            }

            if (first is null || second is null)
            {
                return false;
            }

            return first.Minimum.Equals(second.Minimum) && first.Maximum.Equals(second.Maximum);
        }

        /// <summary>
        /// Inequality comparer. Returns true if the two ranges have a different start and/or end, false otherwise.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <returns>True if the two ranges have a different start and/or end, false otherwise.</returns>
        public static bool operator !=(ValueRange<T> first, ValueRange<T> second)
        {
            return !(first == second);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is ValueRange<T>)
            {
                return this == (ValueRange<T>)obj;
            }

            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Minimum.GetHashCode() ^ this.Maximum.GetHashCode();
        }
    }
}
