// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of rectangle into lists of named rectangles.
    /// </summary>
    [StreamAdapter]
    public class RectangleAdapter : StreamAdapter<Rectangle, List<Tuple<Rectangle, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleAdapter"/> class.
        /// </summary>
        public RectangleAdapter()
            : base(Adapter)
        {
        }

        private static List<Tuple<Rectangle, string>> Adapter(Rectangle value, Envelope env)
        {
            return new List<Tuple<Rectangle, string>>() { Tuple.Create(value, string.Empty) };
        }
    }
}
