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
    /// Implements a stream adapter from point arrays into lists named points.
    /// </summary>
    [StreamAdapter]
    public class PointArrayToScatterPlotAdapter : StreamAdapter<Point[], List<Tuple<Point, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Point, string, string>> GetAdaptedValue(Point[] source, Envelope envelope)
            => source?.Select(p => Tuple.Create(p, default(string), default(string))).ToList();
    }
}