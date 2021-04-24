// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter <see cref="Line3D"/> to nullable <see cref="Line3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Line3DToNullableAdapter : StreamAdapter<Line3D, Line3D?>
    {
        /// <inheritdoc/>
        public override Line3D? GetAdaptedValue(Line3D source, Envelope envelope)
            => source;
    }
}