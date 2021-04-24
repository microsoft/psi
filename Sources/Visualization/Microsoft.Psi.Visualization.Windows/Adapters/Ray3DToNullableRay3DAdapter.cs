// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="Ray3D"/> to nullable <see cref="Ray3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Ray3DToNullableRay3DAdapter : StreamAdapter<Ray3D, Ray3D?>
    {
        /// <inheritdoc/>
        public override Ray3D? GetAdaptedValue(Ray3D source, Envelope envelope) => source;
    }
}
