// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over a series of decimal values.
    /// </summary>
    [Summarizer]
    public class DecimalSeriesRangeSummarizer : Summarizer<Dictionary<string, decimal>, Dictionary<string, decimal>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalSeriesRangeSummarizer"/> class.
        /// </summary>
        public DecimalSeriesRangeSummarizer()
            : base(NumericSeriesRangeSummarizer.SeriesSummarizer, NumericSeriesRangeSummarizer.SeriesCombiner)
        {
        }
    }
}
