// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over long values.
    /// </summary>
    [Summarizer]
    public class LongRangeSummarizer : Summarizer<long, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LongRangeSummarizer"/> class.
        /// </summary>
        public LongRangeSummarizer()
            : base(NumericRangeSummarizer.Summarizer)
        {
        }
    }
}
