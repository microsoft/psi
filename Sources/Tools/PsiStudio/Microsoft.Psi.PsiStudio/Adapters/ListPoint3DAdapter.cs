// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of lists of MathNet.Spatial.Eudlidean.Point3Ds into lists of System.Windows.Media.Media32.Point3Ds.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ListPoint3DAdapter : StreamAdapter<List<Point3D>, List<System.Windows.Media.Media3D.Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListPoint3DAdapter"/> class.
        /// </summary>
        public ListPoint3DAdapter()
            : base(Adapter)
        {
        }

        private static List<System.Windows.Media.Media3D.Point3D> Adapter(List<Point3D> value, Envelope env)
        {
            return value.Select(p => new System.Windows.Media.Media3D.Point3D(p.X, p.Y, p.Z)).ToList();
        }
    }
}
