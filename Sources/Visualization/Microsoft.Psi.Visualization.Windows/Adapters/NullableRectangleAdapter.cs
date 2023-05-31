// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from rectangle into lists of named rectangles.
    /// </summary>
    [StreamAdapter]
    public class NullableRectangleAdapter : StreamAdapter<Rectangle?, List<Tuple<Rectangle, string, string>>>
    {
        /// <inheritdoc/>
        public override List<Tuple<Rectangle, string, string>> GetAdaptedValue(Rectangle? source, Envelope envelope)
            => source.HasValue ?
                new List<Tuple<Rectangle, string, string>>() { Tuple.Create(source.Value, default(string), default(string)) } :
                new List<Tuple<Rectangle, string, string>>();
    }
}
