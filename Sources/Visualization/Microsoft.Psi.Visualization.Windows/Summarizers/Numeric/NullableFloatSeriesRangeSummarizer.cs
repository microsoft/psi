// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents a range summarizer that performs interval-based data summarization over a series of nullable float values.
    /// </summary>
    [Summarizer]
    public class NullableFloatSeriesRangeSummarizer : Summarizer<Dictionary<string, float?>, Dictionary<string, float?>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableFloatSeriesRangeSummarizer"/> class.
        /// </summary>
        public NullableFloatSeriesRangeSummarizer()
            : base(NumericSeriesRangeSummarizer.SeriesSummarizer, NumericSeriesRangeSummarizer.SeriesCombiner)
        {
        }
    }
}
