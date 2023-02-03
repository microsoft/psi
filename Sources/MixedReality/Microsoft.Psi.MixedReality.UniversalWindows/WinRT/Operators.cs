// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.WinRT
{
    using MathNet.Spatial.Euclidean;
    using Windows.Perception.People;
    using Windows.Perception.Spatial;

    /// <summary>
    /// Implements operators.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Converts a <see cref="SpatialRay"/> in HoloLens basis to a <see cref="Ray3D"/> in \psi basis.
        /// </summary>
        /// <param name="spatialRay">The <see cref="SpatialRay"/>.</param>
        /// <returns>The <see cref="Ray3D"/>.</returns>
        public static Ray3D ToRay3D(this SpatialRay spatialRay) => new (spatialRay.Origin.ToPoint3D(), spatialRay.Direction.ToVector3D());

        /// <summary>
        /// Converts a <see cref="HeadPose"/> in HoloLens basis to a <see cref="CoordinateSystem"/> pose in \psi basis.
        /// </summary>
        /// <param name="headPose">The <see cref="HeadPose"/>.</param>
        /// <returns>The <see cref="CoordinateSystem"/> pose.</returns>
        public static CoordinateSystem ToCoordinateSystem(this HeadPose headPose)
        {
            var forward = headPose.ForwardDirection.ToVector3D();
            var left = headPose.UpDirection.ToVector3D().CrossProduct(forward);
            var up = forward.CrossProduct(left);
            return new (headPose.Position.ToPoint3D(), forward.Normalize(), left.Normalize(), up.Normalize());
        }
    }
}
