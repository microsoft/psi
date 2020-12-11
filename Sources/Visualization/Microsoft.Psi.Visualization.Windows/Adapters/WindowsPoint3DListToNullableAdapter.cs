// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of lists of <see cref="Point3D"/> to lists of nullable <see cref="Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class WindowsPoint3DListToNullableAdapter : StreamAdapter<List<Point3D>, List<Point3D?>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPoint3DListToNullableAdapter"/> class.
        /// </summary>
        public WindowsPoint3DListToNullableAdapter()
            : base(Adapter)
        {
        }

        private static List<Point3D?> Adapter(List<Point3D> value, Envelope env)
        {
            return value?.Select(p => p as Point3D?).ToList();
        }
    }
}
