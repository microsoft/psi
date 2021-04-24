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
    /// Implements a stream adapter from list of <see cref="MathNet.Point3D"/> to list of <see cref="Windows.Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetPoint3DListToPoint3DListAdapter : StreamAdapter<List<MathNet.Point3D>, List<Windows.Point3D>>
    {
        /// <inheritdoc/>
        public override List<Windows.Point3D> GetAdaptedValue(List<MathNet.Point3D> source, Envelope envelope)
            => source?.Select(p => new Windows.Point3D(p.X, p.Y, p.Z)).ToList();
    }
}
