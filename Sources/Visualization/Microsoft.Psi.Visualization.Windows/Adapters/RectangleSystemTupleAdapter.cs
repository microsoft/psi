// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of single System.Tuple named rectangle into lists of System.Tuple named rectangles.
    /// </summary>
    [StreamAdapter]
    public class RectangleSystemTupleAdapter : StreamAdapter<Tuple<Rectangle, string>, List<Tuple<Rectangle, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleSystemTupleAdapter"/> class.
        /// </summary>
        public RectangleSystemTupleAdapter()
            : base(Adapter)
        {
        }

        private static List<Tuple<Rectangle, string>> Adapter(Tuple<Rectangle, string> value, Envelope env)
        {
            return new List<Tuple<Rectangle, string>>() { value };
        }
    }
}
