// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Windows;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from <see cref="Point2D"/> to <see cref="Point"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetPoint2DToPoint : StreamAdapter<Point2D, Point>
    {
        /// <inheritdoc/>
        public override Point GetAdaptedValue(Point2D source, Envelope envelope)
            => new (source.X, source.Y);
    }
}