// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using System.Numerics;
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using static Microsoft.Psi.Spatial.Euclidean.Operators;
    using SystemDrawingColor = System.Drawing.Color;

    /// <summary>
    /// Implements operators.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Converts a StereoKit <see cref="Matrix"/> to a <see cref="CoordinateSystem"/>,
        /// changing basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="stereoKitMatrix">The <see cref="Matrix"/> to be converted.</param>
        /// <returns>The <see cref="CoordinateSystem"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static CoordinateSystem ToCoordinateSystem(this Matrix stereoKitMatrix)
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
            => pose.ToMatrix().ToCoordinateSystem();

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> to a StereoKit <see cref="Matrix"/>,
        /// changing basis from MathNet to HoloLens.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> to be converted.</param>
        /// <returns>The <see cref="Matrix"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Matrix ToStereoKitMatrix(this CoordinateSystem coordinateSystem)
            => new (coordinateSystem.RebaseToHoloLensSystemMatrix());

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> pose to a StereoKit <see cref="Pose"/>,
        /// changing basis from MathNet to HoloLens.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> pose to be converted.</param>
        /// <returns>The StereoKit <see cref="Pose"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Pose ToStereoKitPose(this CoordinateSystem coordinateSystem)
            => coordinateSystem.ToStereoKitMatrix().Pose;

        /// <summary>
        /// Convert <see cref="Point3D"/> to StereoKit <see cref="Vec3"/>, changing the basis from MathNet to HoloLens.
        /// </summary>
        /// <param name="point3d"><see cref="Point3D"/> to be converted.</param>
        /// <returns>The StereoKit <see cref="Vec3"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Vec3 ToVec3(this Point3D point3d)
            => new (-(float)point3d.Y, (float)point3d.Z, -(float)point3d.X);

        /// <summary>
        /// Convert StereoKit <see cref="Vec3"/> to <see cref="Point3D"/>, changing the basis from HoloLens to MathNet.
        /// </summary>
        /// <param name="vec3">The StereoKit <see cref="Vec3"/> to be converted.</param>
        /// <returns><see cref="Point3D"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The MathNet basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static Point3D ToPoint3D(this Vec3 vec3)
            => new (-vec3.z, -vec3.x, vec3.y);

        /// <summary>
        /// Converts a specified <see cref="System.Drawing.Color"/> to a StereoKit <see cref="Color"/>.
        /// </summary>
        /// <param name="color">The <see cref="System.Drawing.Color"/>.</param>
        /// <returns>The corresponding StereoKit <see cref="Color"/>.</returns>
        public static Color ToStereoKitColor(this SystemDrawingColor color)
            => new ((float)color.R / 255, (float)color.G / 255, (float)color.B / 255, (float)color.A / 255);

        /// <summary>
        /// Converts a specified <see cref="System.Drawing.Color"/> to a StereoKit <see cref="Color32"/>.
        /// </summary>
        /// <param name="color">The <see cref="System.Drawing.Color"/>.</param>
        /// <returns>The corresponding <see cref="Color32"/>.</returns>
        public static Color32 ToStereoKitColor32(this SystemDrawingColor color)
            => new (color.R, color.G, color.B, color.A);

        /// <summary>
        /// Gets the current StereoKit frame time from OpenXR as a \psi pipeline <see cref="DateTime"/>.
        /// </summary>
        /// <param name="pipeline">The pipeline to get the current time for.</param>
        /// <returns>The current frame time.</returns>
        public static DateTime GetCurrentTimeFromOpenXr(this Pipeline pipeline)
            => pipeline.ConvertTimeFromOpenXr(Backend.OpenXR.Time);

        /// <summary>
        /// Interpolates between two <see cref="Hand"/> poses by interpolating the <see cref="CoordinateSystem"/>
        /// poses of each joint. Spherical linear interpolation (<see cref="System.Numerics.Quaternion.Slerp"/>)
        /// is used for rotation, and linear interpolation is used for translation.
        /// </summary>
        /// <param name="hand1">The first <see cref="Hand"/> pose.</param>
        /// <param name="hand2">The second <see cref="Hand"/> pose.</param>
        /// <param name="amount">The amount to interpolate between the two hand poses.
        /// A value between 0 and 1 will return an interpolation between the two values.
        /// A value outside the 0-1 range will generate an extrapolated result.</param>
        /// <returns>The interpolated <see cref="Hand"/> pose.</returns>
        /// <remarks>Returns null if either input hand is null.</remarks>
        public static Hand InterpolateHands(Hand hand1, Hand hand2, double amount)
        {
            if (hand1 is null || hand2 is null)
            {
                return null;
            }

            // Interpolate each joint pose separately
            int numJoints = (int)HandJointIndex.MaxIndex;
            var interpolatedJoints = new CoordinateSystem[numJoints];
            for (int i = 0; i < numJoints; i++)
            {
                interpolatedJoints[i] = InterpolateCoordinateSystems(hand1.Joints[i], hand2.Joints[i], amount);
            }

            // Use values from hand2 for boolean members if the amount we are
            // interpolating is greater than 0.5. Otherwise select from hand1.
            return new Hand(
                amount > 0.5 ? hand2.IsTracked : hand1.IsTracked,
                amount > 0.5 ? hand2.IsPinched : hand1.IsPinched,
                amount > 0.5 ? hand2.IsGripped : hand1.IsGripped,
                interpolatedJoints);
        }
    }
}
