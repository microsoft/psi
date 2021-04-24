// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of lists of value tuple labeled rectangles into lists of System.Tuple labeled rectangles.
    /// </summary>
    [StreamAdapter]
    public class LabeledRectangleWithTooltipValueTupleListAdapter : StreamAdapter<List<(Rectangle, string, string)>, List<Tuple<Rectangle, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Rectangle, string, string>> GetAdaptedValue(List<(Rectangle, string, string)> source, Envelope envelope)
            => source?.Select(p => Tuple.Create(p.Item1, p.Item2, p.Item3)).ToList();
    }
}
