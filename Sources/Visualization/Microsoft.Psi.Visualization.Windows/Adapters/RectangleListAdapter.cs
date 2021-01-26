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
    /// Implements an adapter from streams of lists of rectangles into lists of named rectangles.
    /// </summary>
    [StreamAdapter]
    public class RectangleListAdapter : StreamAdapter<List<Rectangle>, List<Tuple<Rectangle, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleListAdapter"/> class.
        /// </summary>
        public RectangleListAdapter()
            : base(Adapter)
        {
        }

        private static List<Tuple<Rectangle, string>> Adapter(List<Rectangle> value, Envelope env)
        {
            return value.Select(p => Tuple.Create(p, string.Empty)).ToList();
        }
    }
}
