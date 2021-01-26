// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of <see cref="Line3D"/> into nullable <see cref="Line3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Line3DToNullableAdapter : StreamAdapter<Line3D, Line3D?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Line3DToNullableAdapter"/> class.
        /// </summary>
        public Line3DToNullableAdapter()
            : base(Adapter)
        {
        }

        private static Line3D? Adapter(Line3D value, Envelope env)
        {
            return value;
        }
    }
}