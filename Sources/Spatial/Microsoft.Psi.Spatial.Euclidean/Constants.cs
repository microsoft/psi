// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Spatial.Euclidean
{
    using MathNet.Spatial.Euclidean;

    /// <summary>
    /// Defines spatial constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The horizontal plane.
        /// </summary>
        public static readonly Plane HorizontalPlane = new (UnitVector3D.ZAxis);
    }
}
