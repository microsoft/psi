// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of lists of <see cref="Line3D"/> to lists of nullable <see cref="Line3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Line3DListToNullableAdapter : StreamAdapter<List<Line3D>, List<Line3D?>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Line3DListToNullableAdapter"/> class.
        /// </summary>
        public Line3DListToNullableAdapter()
            : base(Adapter)
        {
        }

        private static List<Line3D?> Adapter(List<Line3D> value, Envelope env)
        {
            return value?.Select(p => p as Line3D?).ToList();
        }
    }
}
