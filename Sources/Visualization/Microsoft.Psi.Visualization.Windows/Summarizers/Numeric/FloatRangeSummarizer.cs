// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over float values.
    /// </summary>
    [Summarizer]
    public class FloatRangeSummarizer : Summarizer<float, float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatRangeSummarizer"/> class.
        /// </summary>
        public FloatRangeSummarizer()
            : base(NumericRangeSummarizer.Summarizer)
        {
        }
    }
}
