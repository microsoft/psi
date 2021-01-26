// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    using MathNet = MathNet.Spatial.Euclidean;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Implements an adapter from streams of nullable <see cref="MathNet.Point3D"/> into nullable <see cref="Windows.Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullablePoint3DToNullablePoint3DAdapter : StreamAdapter<MathNet.Point3D?, Windows.Point3D?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetNullablePoint3DToNullablePoint3DAdapter"/> class.
        /// </summary>
        public MathNetNullablePoint3DToNullablePoint3DAdapter()
            : base(Adapter)
        {
        }

        private static Windows.Point3D? Adapter(MathNet.Point3D? value, Envelope env)
        {
            return value.HasValue ? new Windows.Point3D(value.Value.X, value.Value.Y, value.Value.Z) as Windows.Point3D? : null;
        }
    }
}
