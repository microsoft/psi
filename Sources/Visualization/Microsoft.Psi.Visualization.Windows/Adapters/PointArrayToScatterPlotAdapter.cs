// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of point arrays into lists named points.
    /// </summary>
    [StreamAdapter]
    public class PointArrayToScatterPlotAdapter : StreamAdapter<Point[], List<Tuple<Point, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointArrayToScatterPlotAdapter"/> class.
        /// </summary>
        public PointArrayToScatterPlotAdapter()
            : base(Adapter)
        {
        }

        private static List<Tuple<Point, string>> Adapter(Point[] value, Envelope env)
        {
            return value.Select(p => Tuple.Create(p, default(string))).ToList();
        }
    }
}