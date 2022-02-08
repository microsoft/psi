// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Windows.Perception.Spatial;
    using Quaternion = System.Numerics.Quaternion;

    /// <summary>
    /// Implements operators.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Converts a <see cref="SpatialCoordinateSystem"/> in HoloLens basis to a <see cref="CoordinateSystem"/> in \psi basis.
        /// </summary>
        /// <param name="spatialCoordinateSystem">The <see cref="SpatialCoordinateSystem"/>.</param>
        /// <returns>The <see cref="CoordinateSystem"/>.</returns>
        public static CoordinateSystem TryConvertSpatialCoordinateSystemToPsiCoordinateSystem(this SpatialCoordinateSystem spatialCoordinateSystem)
        {
            var worldPose = spatialCoordinateSystem.TryGetTransformTo(MixedReality.WorldSpatialCoordinateSystem);
            return worldPose.HasValue ? new CoordinateSystem(worldPose.Value.ToMathNetMatrix().ChangeBasisHoloLensToPsi()) : null;
        }

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> in \psi basis to a <see cref="SpatialCoordinateSystem"/> in HoloLens basis.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> in \psi basis.</param>
        /// <returns>The <see cref="SpatialCoordinateSystem"/>.</returns>
        public static SpatialCoordinateSystem TryConvertPsiCoordinateSystemToSpatialCoordinateSystem(this CoordinateSystem coordinateSystem)
        {
            var holoLensMatrix = coordinateSystem.ChangeBasisPsiToHoloLens().ToSystemNumericsMatrix();
            var translation = holoLensMatrix.Translation;
            holoLensMatrix.Translation = Vector3.Zero;
            var rotation = Quaternion.CreateFromRotationMatrix(holoLensMatrix);
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(MixedReality.WorldSpatialCoordinateSystem, translation, rotation);
            return spatialAnchor?.CoordinateSystem;
        }
    }
}
