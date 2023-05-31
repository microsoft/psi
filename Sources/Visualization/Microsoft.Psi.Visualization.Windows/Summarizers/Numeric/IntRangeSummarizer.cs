// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over int values.
    /// </summary>
    [Summarizer]
    public class IntRangeSummarizer : Summarizer<int, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntRangeSummarizer"/> class.
        /// </summary>
        public IntRangeSummarizer()
            : base(NumericRangeSummarizer.Summarizer)
        {
        }
    }
}
