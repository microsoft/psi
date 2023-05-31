// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of lists of MathNet.Spatial.Euclidean.Point2Ds into lists named points.
    /// </summary>
    [StreamAdapter]
    public class MathNetPoint2DToScatterPlotAdapter : StreamAdapter<Point2D, List<Tuple<Point, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Point, string, string>> GetAdaptedValue(Point2D source, Envelope envelope)
            => new () { Tuple.Create(new Point(source.X, source.Y), default(string), default(string)) };
    }
}