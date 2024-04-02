// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// Implements a stream adapter from a single Line2D into a list of nullable Line2Ds.
    /// </summary>
    [StreamAdapter]
    public class Line2DToLine2DNullableListAdapter : StreamAdapter<Line2D, List<Line2D?>>
    {
        /// <inheritdoc/>
        public override List<Line2D?> GetAdaptedValue(Line2D source, Envelope envelope) => new () { source };
    }

    /// <summary>
    /// Implements a stream adapter from a single nullable Line2D into a list of nullable Line2Ds.
    /// </summary>
    [StreamAdapter]
    public class Line2DNullableToLine2DNullableListAdapter : StreamAdapter<Line2D?, List<Line2D?>>
    {
        /// <inheritdoc/>
        public override List<Line2D?> GetAdaptedValue(Line2D? source, Envelope envelope) => new () { source };
    }

    /// <summary>
    /// Implements a stream adapter from a list of Line2Ds into a list of nullable Line2Ds.
    /// </summary>
    [StreamAdapter]
    public class Line2DListToLine2DNullableListAdapter : StreamAdapter<List<Line2D>, List<Line2D?>>
    {
        /// <inheritdoc/>
        public override List<Line2D?> GetAdaptedValue(List<Line2D> source, Envelope envelope)
            => source?.Select(l => l as Line2D?).ToList();
    }

#pragma warning restore SA1649 // File name should match first type name
#pragma warning restore SA1402 // File may only contain a single type
}
