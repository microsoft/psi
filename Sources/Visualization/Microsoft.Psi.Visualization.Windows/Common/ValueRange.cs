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
    }
}
