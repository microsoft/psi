// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Common
{
    /// <summary>
    /// Encasulates an immutable labeled value
    /// </summary>
    /// <typeparam name="T">The type of the value</typeparam>
    public class LabeledValue<T>
        where T : struct
    {
        private readonly T value;

        private readonly string label;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledValue{T}"/> class.
        /// </summary>
        /// <param name="value">The value</param>
        /// <param name="label">The corresponding label</param>
        public LabeledValue(T value, string label)
        {
            this.value = value;
            this.label = label;
        }

        /// <summary>
        /// Gets the value
        /// </summary>
        public T Value => this.value;

        /// <summary>
        /// Gets the label
        /// </summary>
        public string Label => this.label;
    }
}
