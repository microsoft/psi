// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over a series of long values.
    /// </summary>
    [Summarizer]
    public class LongSeriesRangeSummarizer : Summarizer<Dictionary<string, long>, Dictionary<string, long>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LongSeriesRangeSummarizer"/> class.
        /// </summary>
        public LongSeriesRangeSummarizer()
            : base(NumericSeriesRangeSummarizer.SeriesSummarizer, NumericSeriesRangeSummarizer.SeriesCombiner)
        {
        }
    }
}
