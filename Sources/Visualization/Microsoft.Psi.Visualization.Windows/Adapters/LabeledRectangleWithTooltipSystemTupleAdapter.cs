// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of single System.Tuple labeled rectangle into lists of System.Tuple labeled rectangles.
    /// </summary>
    [StreamAdapter]
    public class LabeledRectangleWithTooltipSystemTupleAdapter : StreamAdapter<Tuple<Rectangle, string, string>, List<Tuple<Rectangle, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Rectangle, string, string>> GetAdaptedValue(Tuple<Rectangle, string, string> source, Envelope envelope)
            => source != null ? new List<Tuple<Rectangle, string, string>>() { source } : new List<Tuple<Rectangle, string, string>>();
    }
}
