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
    /// Implements an adapter from streams of lists of <see cref="MathNet.Point3D"/> into lists of <see cref="Windows.Point3D"/>.
    /// </summary>
    [StreamAdapter]
    public class MathNetPoint3DListToPoint3DListAdapter : StreamAdapter<List<MathNet.Point3D>, List<Windows.Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MathNetPoint3DListToPoint3DListAdapter"/> class.
        /// </summary>
        public MathNetPoint3DListToPoint3DListAdapter()
            : base(Adapter)
        {
        }

        private static List<Windows.Point3D> Adapter(List<MathNet.Point3D> value, Envelope env)
        {
            return value?.Select(p => new Windows.Point3D(p.X, p.Y, p.Z)).ToList();
        }
    }
}
