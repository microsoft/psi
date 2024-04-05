// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Visualization
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter for rectangle 3D user interface states.
    /// </summary>
    [StreamAdapter]
    public class Rectangle3DUserInterfacesAdapter : StreamAdapter<List<Rectangle3DUserInterfaceState>, List<Rectangle3D?>>
    {
        /// <inheritdoc />
        public override List<Rectangle3D?> GetAdaptedValue(List<Rectangle3DUserInterfaceState> source, Envelope envelope)
            => source?.Select(s => s?.Rectangle3D).ToList();
    }
}