// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Data;

    using MathNet = MathNet.Spatial.Euclidean;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Used to adapt streams of lists of nullable <see cref="MathNet.Point3D"/> into lists of nullable <see cref="Windows.Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullablePoint3DListToNullablePoint3DListAdapter : StreamAdapter<List<MathNet.Point3D?>, List<Windows.Point3D?>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetNullablePoint3DListToNullablePoint3DListAdapter"/> class.
        /// </summary>
        public MathNetNullablePoint3DListToNullablePoint3DListAdapter()
            : base(Adapter)
        {
        }

        private static List<Windows.Point3D?> Adapter(List<MathNet.Point3D?> value, Envelope env)
        {
            return value?.Select(p => p.HasValue ? new Windows.Point3D(p.Value.X, p.Value.Y, p.Value.Z) as Windows.Point3D? : null).ToList();
        }
    }
}
