// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKit;
    using StereoKitColor = StereoKit.Color;
    using StereoKitColor32 = StereoKit.Color32;
    using SystemDrawingColor = System.Drawing.Color;

    /// <summary>
    /// Implements operators.
    /// </summary>
    public static partial class Operators
    {
        private static readonly CoordinateSystem HoloLensBasis = new (default, UnitVector3D.ZAxis.Negate(), UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis);
        private static readonly CoordinateSystem HoloLensBasisInverted = HoloLensBasis.Invert();

        /// <summary>
        /// Converts a <see cref="StereoKit.Matrix"/> to a <see cref="CoordinateSystem"/>,
        /// changing basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="stereoKitMatrix">The <see cref="StereoKit.Matrix"/> to be converted.</param>
        /// <returns>The <see cref="CoordinateSystem"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static CoordinateSystem ToCoordinateSystem(this StereoKit.Matrix stereoKitMatrix)
        {
            Matrix4x4 systemMatrix = stereoKitMatrix;
            return new CoordinateSystem(systemMatrix.ToMathNetMatrix());
        }

        /// <summary>
        /// Converts a StereoKit <see cref="Pose"/> to a <see cref="CoordinateSystem"/>,
        /// changing basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="pose">The StereoKit <see cref="Pose"/> to be converted.</param>
        /// <returns>The <see cref="CoordinateSystem"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static CoordinateSystem ToCoordinateSystem(this Pose pose)
        {
            return pose.ToMatrix().ToCoordinateSystem();
        }

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> to a <see cref="StereoKit.Matrix"/>,
        /// changing basis from MathNet to HoloLens.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> to be converted.</param>
        /// <returns>The <see cref="StereoKit.Matrix"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static StereoKit.Matrix ToStereoKitMatrix(this CoordinateSystem coordinateSystem)
        {
            return new StereoKit.Matrix(coordinateSystem.ToHoloLensSystemMatrix());
        }

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> pose to a StereoKit <see cref="Pose"/>,
        /// changing basis from MathNet to HoloLens.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> pose to be converted.</param>
        /// <returns>The <see cref="Pose"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Pose ToStereoKitPose(this CoordinateSystem coordinateSystem)
        {
            return coordinateSystem.ToStereoKitMatrix().Pose;
        }

        /// <summary>
        /// Convert <see cref="Point3D"/> to <see cref="Vec3"/>, changing the basis from MathNet to HoloLens.
        /// </summary>
        /// <param name="point3d"><see cref="Point3D"/> to be converted.</param>
        /// <returns><see cref="Vec3"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Vec3 ToVec3(this Point3D point3d)
        {
            // Change of basis happening in place here.
            return new Vec3(-(float)point3d.Y, (float)point3d.Z, -(float)point3d.X);
        }

        /// <summary>
        /// Convert <see cref="Vec3"/> to <see cref="Point3D"/>, changing the basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="vec3"><see cref="Vec3"/> to be converted.</param>
        /// <returns><see cref="Point3D"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Point3D ToPoint3D(this Vec3 vec3)
        {
            // Change of basis happening in place here.
            return new Point3D(-vec3.z, -vec3.x, vec3.y);
        }

        /// <summary>
        /// Convert <see cref="Vector3"/> to <see cref="Point3D"/>, changing the basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="vector3"><see cref="Vector3"/> to be converted.</param>
        /// <returns><see cref="Point3D"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Point3D ToPoint3D(this Vector3 vector3)
        {
            Vec3 v = vector3;
            return v.ToPoint3D();
        }

        /// <summary>
        /// Converts a specified <see cref="System.Drawing.Color"/> to a <see cref="StereoKit.Color"/>.
        /// </summary>
        /// <param name="color">The <see cref="System.Drawing.Color"/>.</param>
        /// <returns>The corresponding <see cref="StereoKit.Color"/>.</returns>
        public static StereoKitColor ToStereoKitColor(this SystemDrawingColor color)
            => new ((float)color.R / 255, (float)color.G / 255, (float)color.B / 255, (float)color.A / 255);

        /// <summary>
        /// Converts a specified <see cref="System.Drawing.Color"/> to a <see cref="StereoKit.Color"/>.
        /// </summary>
        /// <param name="color">The <see cref="System.Drawing.Color"/>.</param>
        /// <returns>The corresponding <see cref="StereoKit.Color32"/>.</returns>
        public static StereoKitColor32 ToStereoKitColor32(this SystemDrawingColor color)
            => new (color.R, color.G, color.B, color.A);

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
        /// Gets the pipeline current time from OpenXR.
        /// </summary>
        /// <param name="pipeline">The pipeline to get the current time for.</param>
        /// <returns>The current OpenXR time.</returns>
        public static DateTime GetCurrentTimeFromOpenXr(this Pipeline pipeline)
        {
            long currentSampleTicks = TimeHelper.ConvertXrTimeToHnsTicks(Backend.OpenXR.Time);
            return pipeline.GetCurrentTimeFromElapsedTicks(currentSampleTicks);
        }

        /// <summary>
        /// Converts a MathNet <see cref="DenseMatrix"/> to a HoloLens <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="mathNetMatrix">The MathNet dense matrix.</param>
        /// <returns>The HoloLens System.Numerics matrix.</returns>
        /// <remarks>The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.</remarks>
        internal static Matrix4x4 ToHoloLensSystemMatrix(this DenseMatrix mathNetMatrix)
        {
            var holoLensMatrix = HoloLensBasis * mathNetMatrix * HoloLensBasisInverted;
            return new Matrix4x4(
                (float)holoLensMatrix.Values[0],
                (float)holoLensMatrix.Values[1],
                (float)holoLensMatrix.Values[2],
                (float)holoLensMatrix.Values[3],
                (float)holoLensMatrix.Values[4],
                (float)holoLensMatrix.Values[5],
                (float)holoLensMatrix.Values[6],
                (float)holoLensMatrix.Values[7],
                (float)holoLensMatrix.Values[8],
                (float)holoLensMatrix.Values[9],
                (float)holoLensMatrix.Values[10],
                (float)holoLensMatrix.Values[11],
                (float)holoLensMatrix.Values[12],
                (float)holoLensMatrix.Values[13],
                (float)holoLensMatrix.Values[14],
                (float)holoLensMatrix.Values[15]);
        }

        /// <summary>
        /// Converts a HoloLens <see cref="Matrix4x4"/> to a MathNet <see cref="DenseMatrix"/>.
        /// </summary>
        /// <param name="holoLensMatrix">The System.Numerics matrix.</param>
        /// <returns>The MathNet dense matrix.</returns>
        /// <remarks>The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.</remarks>
        internal static DenseMatrix ToMathNetMatrix(this Matrix4x4 holoLensMatrix)
        {
            // Values are stored column-major.
            var values = new double[]
            {
                holoLensMatrix.M11,
                holoLensMatrix.M12,
                holoLensMatrix.M13,
                holoLensMatrix.M14,
                holoLensMatrix.M21,
                holoLensMatrix.M22,
                holoLensMatrix.M23,
                holoLensMatrix.M24,
                holoLensMatrix.M31,
                holoLensMatrix.M32,
                holoLensMatrix.M33,
                holoLensMatrix.M34,
                holoLensMatrix.M41,
                holoLensMatrix.M42,
                holoLensMatrix.M43,
                holoLensMatrix.M44,
            };

            var mathNetMatrix = new DenseMatrix(4, 4, values);
            return HoloLensBasisInverted * mathNetMatrix * HoloLensBasis;
        }
    }
}
