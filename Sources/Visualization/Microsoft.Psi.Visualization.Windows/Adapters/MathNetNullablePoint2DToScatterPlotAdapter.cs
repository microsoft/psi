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
    /// Implements an adapter from streams of lists of nullable MathNet.Spatial.Euclidean.Point2Ds into lists named points.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullablePoint2DToScatterPlotAdapter : StreamAdapter<Point2D?, List<Tuple<Point, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Point, string, string>> GetAdaptedValue(Point2D? source, Envelope envelope)
        {
            var list = new List<Tuple<Point, string, string>>();
            if (source.HasValue)
            {
                list.Add(Tuple.Create(new Point(source.Value.X, source.Value.Y), default(string), default(string)));
            }

            return list;
        }
    }
}