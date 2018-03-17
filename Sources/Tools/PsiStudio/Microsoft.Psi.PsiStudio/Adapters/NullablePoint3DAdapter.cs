// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of lists of nullable MathNet.Spatial.Euclidean.Point3Ds into lists named points
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class NullablePoint3DAdapter : StreamAdapter<Point3D?, List<System.Windows.Media.Media3D.Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullablePoint3DAdapter"/> class.
        /// </summary>
        public NullablePoint3DAdapter()
            : base(Adapter)
        {
        }

        private static List<System.Windows.Media.Media3D.Point3D> Adapter(Point3D? value, Envelope env)
        {
            return value.HasValue ?
                new List<System.Windows.Media.Media3D.Point3D>() { new System.Windows.Media.Media3D.Point3D(value.Value.X, value.Value.Y, value.Value.Z) } :
                new List<System.Windows.Media.Media3D.Point3D>() { };
        }
    }
}
