// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over double values.
    /// </summary>
    [Summarizer]
    public class DoubleRangeSummarizer : Summarizer<double, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleRangeSummarizer"/> class.
        /// </summary>
        public DoubleRangeSummarizer()
            : base(NumericRangeSummarizer.Summarizer)
        {
        }
    }
}
