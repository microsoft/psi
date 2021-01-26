// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of lists of <see cref="Rect3D"/> to lists of nullable <see cref="Rect3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Rect3DListToNullableAdapter : StreamAdapter<List<Rect3D>, List<Rect3D?>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rect3DListToNullableAdapter"/> class.
        /// </summary>
        public Rect3DListToNullableAdapter()
            : base(Adapter)
        {
        }

        private static List<Rect3D?> Adapter(List<Rect3D> value, Envelope env)
        {
            return value?.Select(p => p as Rect3D?).ToList();
        }
    }
}
