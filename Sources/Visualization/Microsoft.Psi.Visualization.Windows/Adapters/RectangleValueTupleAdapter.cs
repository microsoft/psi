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
    /// Implements an adapter from streams of of value tuple named rectangle into lists of System.Tuple named rectangles.
    /// </summary>
    [StreamAdapter]
    public class RectangleValueTupleAdapter : StreamAdapter<(Rectangle, string),  List<Tuple<Rectangle, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleValueTupleAdapter"/> class.
        /// </summary>
        public RectangleValueTupleAdapter()
            : base(Adapter)
        {
        }

        private static List<Tuple<Rectangle, string>> Adapter((Rectangle, string) value, Envelope env)
        {
            return new List<Tuple<Rectangle, string>>() { Tuple.Create(value.Item1, value.Item2) };
        }
    }
}
