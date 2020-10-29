// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of <see cref="Point3D"/> to nullable <see cref="Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class WindowsPoint3DToNullableAdapter : StreamAdapter<Point3D, Point3D?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPoint3DToNullableAdapter"/> class.
        /// </summary>
        public WindowsPoint3DToNullableAdapter()
            : base(Adapter)
        {
        }

        private static Point3D? Adapter(Point3D value, Envelope env)
        {
            return value;
        }
    }
}
