// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements an adapter from streams of <see cref="Rect3D"/> into nullable <see cref="Rect3D"/>.
    /// </summary>
    [StreamAdapter]
    public class Rect3DToNullableAdapter : StreamAdapter<Rect3D, Rect3D?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Rect3DToNullableAdapter"/> class.
        /// </summary>
        public Rect3DToNullableAdapter()
            : base(Adapter)
        {
        }

        private static Rect3D? Adapter(Rect3D value, Envelope env)
        {
            return value;
        }
    }
}