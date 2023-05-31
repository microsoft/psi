// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from <see cref="Rect3D"/> to nullable <see cref="Rect3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Rect3DToNullableAdapter : StreamAdapter<Rect3D, Rect3D?>
    {
        /// <inheritdoc/>
        public override Rect3D? GetAdaptedValue(Rect3D source, Envelope envelope)
            => source;
    }
}