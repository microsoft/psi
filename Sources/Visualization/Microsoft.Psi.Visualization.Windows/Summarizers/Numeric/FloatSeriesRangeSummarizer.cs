// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over a series of float values.
    /// </summary>
    [Summarizer]
    public class FloatSeriesRangeSummarizer : Summarizer<Dictionary<string, float>, Dictionary<string, float>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatSeriesRangeSummarizer"/> class.
        /// </summary>
        public FloatSeriesRangeSummarizer()
            : base(NumericSeriesRangeSummarizer.SeriesSummarizer, NumericSeriesRangeSummarizer.SeriesCombiner)
        {
        }
    }
}
