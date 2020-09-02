// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.LiveCharts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams a list describing a single series chart to a cartesian chart visualization object.
    /// </summary>
    [StreamAdapter]
    public class SingleSeriesChartStreamAdapter : StreamAdapter<List<(string, double)>, Dictionary<string, (string, double)[]>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleSeriesChartStreamAdapter"/> class.
        /// </summary>
        public SingleSeriesChartStreamAdapter()
            : base(Adapter)
        {
        }

        private static Dictionary<string, (string, double)[]> Adapter(List<(string, double)> value, Envelope envelope)
        {
            return new Dictionary<string, (string, double)[]>()
            {
                { "Data", value != null ? value.ToArray() : Array.Empty<(string, double)>() },
            };
        }
    }
}
