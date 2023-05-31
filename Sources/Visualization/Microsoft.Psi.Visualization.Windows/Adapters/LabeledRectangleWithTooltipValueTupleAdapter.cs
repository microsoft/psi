// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from value tuple labeled rectangle into lists of System.Tuple labeled rectangles.
    /// </summary>
    [StreamAdapter]
    public class LabeledRectangleWithTooltipValueTupleAdapter : StreamAdapter<(Rectangle, string, string),  List<Tuple<Rectangle, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Rectangle, string, string>> GetAdaptedValue((Rectangle, string, string) source, Envelope envelope)
            => new List<Tuple<Rectangle, string, string>>() { Tuple.Create(source.Item1, source.Item2, source.Item3) };
    }
}
