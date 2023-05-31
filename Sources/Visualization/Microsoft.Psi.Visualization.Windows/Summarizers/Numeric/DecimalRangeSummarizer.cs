// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over decimal values.
    /// </summary>
    [Summarizer]
    public class DecimalRangeSummarizer : Summarizer<decimal, decimal>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalRangeSummarizer"/> class.
        /// </summary>
        public DecimalRangeSummarizer()
            : base(NumericRangeSummarizer.Summarizer)
        {
        }
    }
}
