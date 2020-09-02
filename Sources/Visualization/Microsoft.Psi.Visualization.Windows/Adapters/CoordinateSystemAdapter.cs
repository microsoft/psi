// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of coordinate systems into lists of coordinate systems.
    /// </summary>
    [StreamAdapter]
    public class CoordinateSystemAdapter : StreamAdapter<CoordinateSystem, List<CoordinateSystem>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateSystemAdapter"/> class.
        /// </summary>
        public CoordinateSystemAdapter()
            : base(Adapter)
        {
        }

        private static List<CoordinateSystem> Adapter(CoordinateSystem value, Envelope env)
        {
            return new List<CoordinateSystem>() { value };
        }
    }
}