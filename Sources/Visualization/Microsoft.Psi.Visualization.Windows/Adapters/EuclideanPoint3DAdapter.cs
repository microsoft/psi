// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Collections.Generic;
    using System.Windows.Media.Media3D;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Extensions;

    /// <summary>
    /// Used to adapt streams of floats into doubles.
    /// </summary>
    [StreamAdapter]
    public class EuclideanPoint3DAdapter : StreamAdapter<MathNet.Spatial.Euclidean.Point3D, List<Point3D>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EuclideanPoint3DAdapter"/> class.
        /// </summary>
        public EuclideanPoint3DAdapter()
            : base(Adapter)
        {
        }

        private static List<Point3D> Adapter(MathNet.Spatial.Euclidean.Point3D value, Envelope env)
        {
            return new List<Point3D>() { value.ToPoint3D() };
        }
    }
}