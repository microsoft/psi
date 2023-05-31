// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from single System.Tuple labeled rectangle into lists of System.Tuple labeled rectangles.
    /// </summary>
    [StreamAdapter]
    public class LabeledRectangleSystemTupleAdapter : StreamAdapter<Tuple<Rectangle, string>, List<Tuple<Rectangle, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Rectangle, string, string>> GetAdaptedValue(Tuple<Rectangle, string> source, Envelope envelope)
            => source != null ?
            new List<Tuple<Rectangle, string, string>>() { Tuple.Create(source.Item1, source.Item2, default(string)) } :
            new List<Tuple<Rectangle, string, string>>();
    }
}
