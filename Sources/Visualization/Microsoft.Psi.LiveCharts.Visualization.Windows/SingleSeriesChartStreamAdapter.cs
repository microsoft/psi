// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.LiveCharts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a list describing a single series chart to a dictionary
    /// describing a multiple-series chart.
    /// </summary>
    [StreamAdapter]
    public class SingleSeriesChartStreamAdapter : StreamAdapter<List<(string, double)>, Dictionary<string, (string, double)[]>>
    {
        /// <inheritdoc/>
        public override Dictionary<string, (string, double)[]> GetAdaptedValue(List<(string, double)> source, Envelope envelope)
            => new Dictionary<string, (string, double)[]>()
            {
                { "Data", source != null ? source.ToArray() : Array.Empty<(string, double)>() },
            };
    }
}
