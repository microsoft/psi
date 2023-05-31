// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Spatial.Euclidean;
    using static Microsoft.Psi.Spatial.Euclidean.Operators;

    /// <summary>
    /// Implements operators.
    /// </summary>
    public static partial class Operators
    {
        private static readonly CoordinateSystem HoloLensBasis = new (default, UnitVector3D.ZAxis.Negate(), UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis);
        private static readonly CoordinateSystem HoloLensBasisInverted = HoloLensBasis.Invert();

        /// <summary>
        /// Convert stream of frames of IMU samples to flattened stream of samples within.
        /// </summary>
        /// <param name="source">Stream of IMU frames.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>Stream of IMU samples.</returns>
        public static IProducer<Vector3D> SelectManyImuSamples(
            this IProducer<(Vector3D Sample, DateTime OriginatingTime)[]> source,
            DeliveryPolicy<(Vector3D Sample, DateTime OriginatingTime)[]> deliveryPolicy = null,
            string name = nameof(SelectManyImuSamples))
            => source.Process<(Vector3D Sample, DateTime OriginatingTime)[], Vector3D>(
                (samples, envelope, emitter) =>
                {
                    foreach (var sample in samples)
                    {
                        emitter.Post(sample.Sample, sample.OriginatingTime);
                    }
                },
                deliveryPolicy,
                name);

        /// <summary>
        /// Converts a time from OpenXR into the equivalent \psi pipeline <see cref="DateTime"/>.
        /// </summary>
        /// <param name="pipeline">The pipeline to convert the given time for.</param>
        /// <param name="openXrTime">The OpenXR time to convert.</param>
        /// <returns>The OpenXR time as a <see cref="DateTime"/>.</returns>
        public static DateTime ConvertTimeFromOpenXr(this Pipeline pipeline, long openXrTime)
        {
            long sampleTicks = TimeHelper.ConvertXrTimeToHnsTicks(openXrTime);
            return pipeline.GetCurrentTimeFromElapsedTicks(sampleTicks);
        }

        /// <summary>
        /// Convert a <see cref="Vector3"/> to a <see cref="Point3D"/>, changing the basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="vector3">The <see cref="Vector3"/> to be converted.</param>
        /// <returns><see cref="Point3D"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Point3D ToPoint3D(this Vector3 vector3)
            => new (-vector3.Z, -vector3.X, vector3.Y);

        /// <summary>
        /// Convert a <see cref="Vector3"/> to a <see cref="Vector3D"/>, changing the basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="vector3">The <see cref="Vector3"/> to be converted.</param>
        /// <returns><see cref="Vector3D"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Vector3D ToVector3D(this Vector3 vector3)
            => new (-vector3.Z, -vector3.X, vector3.Y);

        /// <summary>
        /// Converts and rebases a MathNet <see cref="CoordinateSystem"/> to a HoloLens <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="coordinateSystem">The MathNet basis <see cref="CoordinateSystem"/>.</param>
        /// <returns>The HoloLens basis <see cref="Matrix4x4"/>.</returns>
        /// <remarks>The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.</remarks>
        internal static Matrix4x4 RebaseToHoloLensSystemMatrix(this CoordinateSystem coordinateSystem)
        {
            var rebased = new CoordinateSystem(HoloLensBasis * coordinateSystem * HoloLensBasisInverted);
            return rebased.ToSystemNumericsMatrix();
        }

        /// <summary>
        /// Converts and rebases a HoloLens <see cref="Matrix4x4"/> to a MathNet <see cref="CoordinateSystem"/>.
        /// </summary>
        /// <param name="holoLensMatrix">The HoloLens basis <see cref="Matrix4x4"/>.</param>
        /// <returns>The MathNet basis <see cref="CoordinateSystem"/>.</returns>
        /// <remarks>The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.</remarks>
        internal static CoordinateSystem RebaseToMathNetCoordinateSystem(this Matrix4x4 holoLensMatrix)
        {
            var cs = holoLensMatrix.ToCoordinateSystem();
            return new CoordinateSystem(HoloLensBasisInverted * cs * HoloLensBasis);
        }
    }
}
