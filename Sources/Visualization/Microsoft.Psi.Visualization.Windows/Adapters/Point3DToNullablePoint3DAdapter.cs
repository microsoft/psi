// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="Point3D"/> to nullable <see cref="Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Point3DToNullablePoint3DAdapter : StreamAdapter<Point3D, Point3D?>
    {
        /// <inheritdoc/>
        public override Point3D? GetAdaptedValue(Point3D source, Envelope envelope)
            => source;
    }
}
