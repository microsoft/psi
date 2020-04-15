// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using Microsoft.Psi.Visualization.Data;

    using MathNet = MathNet.Spatial.Euclidean;
    using Windows = System.Windows.Media.Media3D;

    /// <summary>
    /// Used to adapt streams of nullable MathNet.Spatial.Euclidean.Point3Ds into lists of System.Windows.Media.Media32.Point3Ds.
    /// </summary>
    [StreamAdapter]
    public class MathNetNullablePoint3DToWindowsPoint3DListAdapter : StreamAdapter<MathNet.Point3D?, List<Windows.Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetNullablePoint3DToWindowsPoint3DListAdapter"/> class.
        /// </summary>
        public MathNetNullablePoint3DToWindowsPoint3DListAdapter()
            : base(Adapter)
        {
        }

        private static List<Windows.Point3D> Adapter(MathNet.Point3D? value, Envelope env)
        {
            return value.HasValue ?
                new List<Windows.Point3D>() { new Windows.Point3D(value.Value.X, value.Value.Y, value.Value.Z) } :
                new List<Windows.Point3D>() { };
        }
    }
}
