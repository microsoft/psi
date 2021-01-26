// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    using MathNet = MathNet.Spatial.Euclidean;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements an adapter from streams of <see cref="MathNet.Point3D"/> into nullable <see cref="Windows.Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetPoint3DToNullablePoint3DAdapter : StreamAdapter<MathNet.Point3D, Windows.Point3D?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetPoint3DToNullablePoint3DAdapter"/> class.
        /// </summary>
        public MathNetPoint3DToNullablePoint3DAdapter()
            : base(Adapter)
        {
        }

        private static Windows.Point3D? Adapter(MathNet.Point3D value, Envelope env)
        {
            return new Windows.Point3D(value.X, value.Y, value.Z);
        }
    }
}
