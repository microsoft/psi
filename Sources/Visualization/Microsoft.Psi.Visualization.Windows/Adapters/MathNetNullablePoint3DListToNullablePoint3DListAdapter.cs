// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;
    using MathNet = MathNet.Spatial.Euclidean;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a stream adapter from list of nullable <see cref="MathNet.Point3D"/> to list of nullable <see cref="Windows.Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullablePoint3DListToNullablePoint3DListAdapter : StreamAdapter<List<MathNet.Point3D?>, List<Windows.Point3D?>>
    {
        /// <inheritdoc/>
        public override List<Windows.Point3D?> GetAdaptedValue(List<MathNet.Point3D?> source, Envelope envelope)
            => source?.Select(p => p.HasValue ? new Windows.Point3D(p.Value.X, p.Value.Y, p.Value.Z) as Windows.Point3D? : null).ToList();
    }
}
