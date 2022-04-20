// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from list of <see cref="Ray3D"/> to list of nullable <see cref="Ray3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Ray3DListToNullableAdapter : StreamAdapter<List<Ray3D>, List<Ray3D?>>
    {
        /// <inheritdoc/>
        public override List<Ray3D?> GetAdaptedValue(List<Ray3D> source, Envelope envelope)
            => source?.Select(p => p as Ray3D?).ToList();
    }
}
