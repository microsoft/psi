// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Windows;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from <see cref="Point2D"/> to a labeled point.
    /// </summary>
    [StreamAdapter]
    public class MathNetPoint2DToLabeledPoint : StreamAdapter<Point2D, Tuple<Point, string, string>>
    {
        /// <inheritdoc/>
        public override Tuple<Point, string, string> GetAdaptedValue(Point2D source, Envelope envelope)
            => Tuple.Create<Point, string, string>(new (source.X, source.Y), null, null);
    }
}