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
    /// Implements a stream adapter from lists of rectangles into lists of named rectangles.
    /// </summary>
    [StreamAdapter]
    public class RectangleListAdapter : StreamAdapter<List<Rectangle>, List<Tuple<Rectangle, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Rectangle, string, string>> GetAdaptedValue(List<Rectangle> source, Envelope envelope)
            => source?.Select(p => Tuple.Create(p, default(string), default(string))).ToList();
    }
}
