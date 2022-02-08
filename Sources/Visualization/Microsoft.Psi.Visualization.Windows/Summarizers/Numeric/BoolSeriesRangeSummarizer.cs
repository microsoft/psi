// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over a series of bool values.
    /// </summary>
    [Summarizer]
    public class BoolSeriesRangeSummarizer : Summarizer<Dictionary<string, bool>, Dictionary<string, bool>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoolSeriesRangeSummarizer"/> class.
        /// </summary>
        public BoolSeriesRangeSummarizer()
            : base(NumericSeriesRangeSummarizer.SeriesSummarizer, NumericSeriesRangeSummarizer.SeriesCombiner)
        {
        }
    }
}
