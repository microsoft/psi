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
    /// Used to adapt streams of lists of nullable MathNet.Spatial.Euclidean.Point2Ds into lists named points.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullablePoint2DToScatterPlotAdapter : StreamAdapter<Point2D?, List<Tuple<Point, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetNullablePoint2DToScatterPlotAdapter"/> class.
        /// </summary>
        public MathNetNullablePoint2DToScatterPlotAdapter()
            : base(Adapter)
        {
        }

        private static List<Tuple<Point, string>> Adapter(Point2D? value, Envelope env)
        {
            var list = new List<Tuple<Point, string>>();
            if (value.HasValue)
            {
                list.Add(Tuple.Create(new Point(value.Value.X, value.Value.Y), default(string)));
            }

            return list;
        }
    }
}