// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKit;
    using static Microsoft.Psi.Spatial.Euclidean.Operators;
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
            return systemMatrix.RebaseToMathNetCoordinateSystem();
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
            return new StereoKit.Matrix(coordinateSystem.RebaseToHoloLensSystemMatrix());
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
        /// Gets the current StereoKit frame time from OpenXR as a \psi pipeline <see cref="DateTime"/>.
        /// </summary>
        /// <param name="pipeline">The pipeline to get the current time for.</param>
        /// <returns>The current frame time.</returns>
        public static DateTime GetCurrentTimeFromOpenXr(this Pipeline pipeline)
        {
            return pipeline.ConvertTimeFromOpenXr(Backend.OpenXR.Time);
        }

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
        /// Interpolates between two <see cref="Hand"/> poses by interpolating the <see cref="CoordinateSystem"/>
        /// poses of each joint. Spherical linear interpolation (<see cref="System.Numerics.Quaternion.Slerp"/>)
        /// is used for rotation, and linear interpolation is used for translation.
        /// </summary>
        /// <param name="hand1">The first <see cref="Hand"/> pose.</param>
        /// <param name="hand2">The second <see cref="Hand"/> pose.</param>
        /// <param name="amount">The amount to interpolate between the two hand poses. A value of 0 will
        /// effectively return the first hand pose, a value of 1 will effectively return the second
        /// hand pose, and a value between 0 and 1 will return an interpolation between those two values.
        /// A value outside the 0-1 range will generate an extrapolated result.</param>
        /// <returns>The interpolated <see cref="Hand"/> pose.</returns>
        public static Hand InterpolateHands(Hand hand1, Hand hand2, double amount)
        {
            int numJoints = (int)HandJointIndex.MaxIndex;

            // Initialize a default pose if either input hand is null
            hand1 ??= new Hand(false, false, false, new CoordinateSystem[numJoints]);
            hand2 ??= new Hand(false, false, false, new CoordinateSystem[numJoints]);

            // Interpolate each joint pose separately
            var interpolatedJoints = new CoordinateSystem[numJoints];
            for (int i = 0; i < numJoints; i++)
            {
                interpolatedJoints[i] = InterpolateCoordinateSystems(hand1.Joints[i], hand2.Joints[i], amount);
            }

            // If interpolating *any* non-zero amount of the pose of hand2 into the pose of hand1,
            // then use values from hand2 for tracked, pinched, and gripped. Otherwise select from hand1.
            return new Hand(
                amount > 0 ? hand2.IsTracked : hand1.IsTracked,
                amount > 0 ? hand2.IsPinched : hand1.IsPinched,
                amount > 0 ? hand2.IsGripped : hand1.IsGripped,
                interpolatedJoints);
        }

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
