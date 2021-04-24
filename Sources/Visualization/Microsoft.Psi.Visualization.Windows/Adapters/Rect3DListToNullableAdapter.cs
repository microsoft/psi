// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from list of <see cref="Rect3D"/> to list of nullable <see cref="Rect3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Rect3DListToNullableAdapter : StreamAdapter<List<Rect3D>, List<Rect3D?>>
    {
        /// <inheritdoc/>
        public override List<Rect3D?> GetAdaptedValue(List<Rect3D> source, Envelope envelope)
            => source?.Select(p => p as Rect3D?).ToList();
    }
}
