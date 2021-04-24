// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;
    using MathNet = MathNet.Spatial.Euclidean;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a stream adapter from nullable <see cref="MathNet.Point3D"/> to nullable <see cref="Windows.Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullablePoint3DToNullablePoint3DAdapter : StreamAdapter<MathNet.Point3D?, Windows.Point3D?>
    {
        /// <inheritdoc/>
        public override Windows.Point3D? GetAdaptedValue(MathNet.Point3D? source, Envelope envelope)
            => source.HasValue ? new Windows.Point3D(source.Value.X, source.Value.Y, source.Value.Z) as Windows.Point3D? : null;
    }
}
