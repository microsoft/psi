// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from list of <see cref="Line3D"/> to list of nullable <see cref="Line3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Line3DListToNullableAdapter : StreamAdapter<List<Line3D>, List<Line3D?>>
    {
        /// <inheritdoc/>
        public override List<Line3D?> GetAdaptedValue(List<Line3D> source, Envelope envelope)
            => source?.Select(p => p as Line3D?).ToList();
    }
}
