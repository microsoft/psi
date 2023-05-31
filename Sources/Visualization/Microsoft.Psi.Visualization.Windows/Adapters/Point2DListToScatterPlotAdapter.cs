// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of lists of MathNet.Spatial.Euclidean.Point2Ds list of labeled points.
    /// </summary>
    [StreamAdapter]
    public class Point2DListToScatterPlotAdapter : StreamAdapter<List<Point2D>, List<Tuple<Point, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Point, string, string>> GetAdaptedValue(List<Point2D> source, Envelope envelope)
            => source?.Select(point2D => Tuple.Create(new Point(point2D.X, point2D.Y), default(string), default(string))).ToList();
    }
}