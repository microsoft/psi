// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from list of nullable <see cref="Point3D"/> to list of <see cref="Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class NullablePoint3DListToPoint3DListAdapter : StreamAdapter<List<Point3D?>, List<Point3D>>
    {
        /// <inheritdoc/>
        public override List<Point3D> GetAdaptedValue(List<Point3D?> source, Envelope envelope)
            => source?.Where(p => p.HasValue).Select(p => p.Value).ToList();
    }
}
