// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over a series of nullable int values.
    /// </summary>
    [Summarizer]
    public class NullableIntSeriesRangeSummarizer : Summarizer<Dictionary<string, int?>, Dictionary<string, int?>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableIntSeriesRangeSummarizer"/> class.
        /// </summary>
        public NullableIntSeriesRangeSummarizer()
            : base(NumericSeriesRangeSummarizer.SeriesSummarizer, NumericSeriesRangeSummarizer.SeriesCombiner)
        {
        }
    }
}
