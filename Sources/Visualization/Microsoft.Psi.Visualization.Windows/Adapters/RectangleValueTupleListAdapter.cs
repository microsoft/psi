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
    /// Used to adapt streams of lists of value tuple named rectangles into lists of System.Tuple named rectangles.
    /// </summary>
    [StreamAdapter]
    public class RectangleValueTupleListAdapter : StreamAdapter<List<(Rectangle, string)>, List<Tuple<Rectangle, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleValueTupleListAdapter"/> class.
        /// </summary>
        public RectangleValueTupleListAdapter()
            : base(Adapter)
        {
        }

        private static List<Tuple<Rectangle, string>> Adapter(List<(Rectangle, string)> value, Envelope env)
        {
            return value.Select(p => Tuple.Create(p.Item1, p.Item2)).ToList();
        }
    }
}
