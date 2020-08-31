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
    /// Used to adapt streams of lists of MathNet.Spatial.Eudlidean.Point3Ds into lists of System.Windows.Media.Media32.Point3Ds.
    /// </summary>
    [StreamAdapter]
    public class MathNetPoint3DListToWindowsPoint3DListAdapter : StreamAdapter<List<MathNet.Point3D>, List<Windows.Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetPoint3DListToWindowsPoint3DListAdapter"/> class.
        /// </summary>
        public MathNetPoint3DListToWindowsPoint3DListAdapter()
            : base(Adapter)
        {
        }

        private static List<Windows.Point3D> Adapter(List<MathNet.Point3D> value, Envelope env)
        {
            return value.Select(p => new Windows.Point3D(p.X, p.Y, p.Z)).ToList();
        }
    }
}
