// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Numerics;
    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Spatial.Euclidean;
    using StereoKit;
    using StereoKitColor = StereoKit.Color;
    using SystemDrawingColor = System.Drawing.Color;

    /// <summary>
    /// Implements operators.
    /// </summary>
    public static partial class Operators
    {
        private static readonly CoordinateSystem HoloLensBasis = new (default, UnitVector3D.ZAxis.Negate(), UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis);
        private static readonly CoordinateSystem HoloLensBasisInverted = HoloLensBasis.Invert();

        /// <summary>
        /// Compute a change of basis for the given matrix. From HoloLens basis to \psi basis.
        /// </summary>
        /// <param name="holoLensMatrix">The given matrix in HoloLens basis.</param>
        /// <returns>The converted matrix with \psi basis.</returns>
        /// /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static DenseMatrix ChangeBasisHoloLensToPsi(this DenseMatrix holoLensMatrix)
        {
            return HoloLensBasisInverted * holoLensMatrix * HoloLensBasis;
        }

        /// <summary>
        /// Compute a change of basis for the given matrix. From \psi basis to HoloLens basis.
        /// </summary>
        /// <param name="psiMatrix">The given matrix in \psi basis.</param>
        /// <returns>The converted matrix with HoloLens basis.</returns>
        /// /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// </remarks>
        public static DenseMatrix ChangeBasisPsiToHoloLens(this DenseMatrix psiMatrix)
        {
            return HoloLensBasis * psiMatrix * HoloLensBasisInverted;
        }

        /// <summary>
        /// Converts a <see cref="StereoKit.Matrix"/> pose to a \psi <see cref="CoordinateSystem"/>,
        /// changing basis from HoloLens to \psi and transforming from StereoKit coordinates to world coordinates.
        /// </summary>
        /// <param name="stereoKitMatrix">The <see cref="StereoKit.Matrix"/> to be converted.</param>
        /// <returns>The <see cref="CoordinateSystem"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// "StereoKit coordinates" means "in relation to the pose of the headset at startup".
        /// "World coordinates" means "in relation to the world spatial anchor".
        /// </remarks>
        public static CoordinateSystem ToPsiCoordinateSystem(this StereoKit.Matrix stereoKitMatrix)
        {
            Matrix4x4 systemMatrix = stereoKitMatrix;
            var mathNetMatrix = systemMatrix.ToMathNetMatrix().ChangeBasisHoloLensToPsi();
            var coordinateSystem = new CoordinateSystem(mathNetMatrix);
            return coordinateSystem.TransformBy(StereoKitTransforms.StereoKitStartingPose);
        }

        /// <summary>
        /// Converts a StereoKit <see cref="Pose"/> to a \psi <see cref="CoordinateSystem"/>,
        /// changing basis from HoloLens to \psi and transforming from StereoKit coordinates to world coordinates.
        /// </summary>
        /// <param name="pose">The StereoKit <see cref="Pose"/> to be converted.</param>
        /// <returns>The <see cref="CoordinateSystem"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// "StereoKit coordinates" means "in relation to the pose of the headset at startup".
        /// "World coordinates" means "in relation to the world spatial anchor".
        /// </remarks>
        public static CoordinateSystem ToPsiCoordinateSystem(this Pose pose)
        {
            return pose.ToMatrix().ToPsiCoordinateSystem();
        }

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> pose to a <see cref="StereoKit.Matrix"/> pose,
        /// changing basis from \psi to HoloLens and transforming from world coordinates to StereoKit coordinates.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> pose to be converted.</param>
        /// <returns>The <see cref="StereoKit.Matrix"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// "StereoKit coordinates" means "in relation to the pose of the headset at startup".
        /// "World coordinates" means "in relation to the world spatial anchor".
        /// </remarks>
        public static StereoKit.Matrix ToStereoKitMatrix(this CoordinateSystem coordinateSystem)
        {
            var mathNetMatrix = coordinateSystem.TransformBy(StereoKitTransforms.StereoKitStartingPoseInverse).ChangeBasisPsiToHoloLens();
            return new StereoKit.Matrix(mathNetMatrix.ToSystemNumericsMatrix());
        }

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> pose to a StereoKit <see cref="Pose"/>,
        /// changing basis from \psi to HoloLens and transforming from world coordinates to StereoKit coordinates.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> pose to be converted.</param>
        /// <returns>The <see cref="Pose"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// "StereoKit coordinates" means "in relation to the pose of the headset at startup".
        /// "World coordinates" means "in relation to the world spatial anchor".
        /// </remarks>
        public static Pose ToStereoKitPose(this CoordinateSystem coordinateSystem)
        {
            return coordinateSystem.ToStereoKitMatrix().Pose;
        }

        /// <summary>
        /// Converts a <see cref="Matrix4x4"/> to a <see cref="DenseMatrix"/>.
        /// </summary>
        /// <param name="systemNumericsMatrix">The System.Numerics matrix.</param>
        /// <returns>The MathNet dense matrix.</returns>
        public static DenseMatrix ToMathNetMatrix(this Matrix4x4 systemNumericsMatrix)
        {
            // Values are stored column-major.
            var values = new double[]
            {
                systemNumericsMatrix.M11,
                systemNumericsMatrix.M12,
                systemNumericsMatrix.M13,
                systemNumericsMatrix.M14,
                systemNumericsMatrix.M21,
                systemNumericsMatrix.M22,
                systemNumericsMatrix.M23,
                systemNumericsMatrix.M24,
                systemNumericsMatrix.M31,
                systemNumericsMatrix.M32,
                systemNumericsMatrix.M33,
                systemNumericsMatrix.M34,
                systemNumericsMatrix.M41,
                systemNumericsMatrix.M42,
                systemNumericsMatrix.M43,
                systemNumericsMatrix.M44,
            };

            return new DenseMatrix(4, 4, values);
        }

        /// <summary>
        /// Converts a <see cref="DenseMatrix"/> to a <see cref="Matrix4x4"/>.
        /// </summary>
        /// <param name="mathNetMatrix">The MathNet dense matrix.</param>
        /// <returns>The System.Numerics matrix.</returns>
        public static Matrix4x4 ToSystemNumericsMatrix(this DenseMatrix mathNetMatrix)
        {
            var values = mathNetMatrix.Values;
            return new Matrix4x4(
                (float)values[0],
                (float)values[1],
                (float)values[2],
                (float)values[3],
                (float)values[4],
                (float)values[5],
                (float)values[6],
                (float)values[7],
                (float)values[8],
                (float)values[9],
                (float)values[10],
                (float)values[11],
                (float)values[12],
                (float)values[13],
                (float)values[14],
                (float)values[15]);
        }

        /// <summary>
        /// Convert <see cref="Point3D"/> to <see cref="Vec3"/>, changing the basis from \psi to HoloLens.
        /// </summary>
        /// <param name="point3d"><see cref="Point3D"/> to be converted.</param>
        /// <param name="transformWorldToStereoKit">If true, transform from world coordinates to StereoKit coordinates.</param>
        /// <returns><see cref="Vec3"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// "StereoKit coordinates" means "in relation to the pose of the headset at startup".
        /// "World coordinates" means "in relation to the world spatial anchor".
        /// </remarks>
        public static Vec3 ToVec3(this Point3D point3d, bool transformWorldToStereoKit = true)
        {
            if (transformWorldToStereoKit)
            {
                point3d = StereoKitTransforms.StereoKitStartingPoseInverse.Transform(point3d);
            }

            // Change of basis happening here:
            return new Vec3(-(float)point3d.Y, (float)point3d.Z, -(float)point3d.X);
        }

        /// <summary>
        /// Convert <see cref="Vec3"/> to <see cref="Point3D"/>, changing the basis from HoloLens to \psi.
        /// </summary>
        /// <param name="vec3"><see cref="Vec3"/> to be converted.</param>
        /// <param name="transformStereoKitToWorld">If true, transform from StereoKit coordinates to world coordinates.</param>
        /// <returns><see cref="Point3D"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// "StereoKit coordinates" means "in relation to the pose of the headset at startup".
        /// "World coordinates" means "in relation to the world spatial anchor".
        /// </remarks>
        public static Point3D ToPoint3D(this Vec3 vec3, bool transformStereoKitToWorld = true)
        {
            var point3D = new Point3D(-vec3.z, -vec3.x, vec3.y);

            if (transformStereoKitToWorld)
            {
                return StereoKitTransforms.StereoKitStartingPose.Transform(point3D);
            }
            else
            {
                return point3D;
            }
        }

        /// <summary>
        /// Convert <see cref="Vector3"/> to <see cref="Point3D"/>, changing the basis from HoloLens to \psi.
        /// </summary>
        /// <param name="vector3"><see cref="Vector3"/> to be converted.</param>
        /// <param name="transformStereoKitToWorld">If true, transform from StereoKit coordinates to world coordinates.</param>
        /// <returns><see cref="Point3D"/>.</returns>
        /// <remarks>
        /// The HoloLens basis assumes that Forward=-Z, Left=-X, and Up=Y.
        /// The \psi basis assumes that Forward=X, Left=Y, and Up=Z.
        /// "StereoKit coordinates" means "in relation to the pose of the headset at startup".
        /// "World coordinates" means "in relation to the world spatial anchor".
        /// </remarks>
        public static Point3D ToPoint3D(this Vector3 vector3, bool transformStereoKitToWorld = true)
        {
            Vec3 v = vector3;
            return v.ToPoint3D(transformStereoKitToWorld);
        }

        /// <summary>
        /// Converts a specified <see cref="System.Drawing.Color"/> to a <see cref="StereoKit.Color"/>.
        /// </summary>
        /// <param name="color">The <see cref="System.Drawing.Color"/>.</param>
        /// <returns>The corresponding <see cref="StereoKit.Color"/>.</returns>
        public static StereoKitColor ToStereoKitColor(this SystemDrawingColor color)
            => new ((float)color.R / 255, (float)color.G / 255, (float)color.B / 255, (float)color.A / 255);

        /// <summary>
        /// Convert stream of frames of IMU samples to flattened stream of samples within.
        /// </summary>
        /// <param name="source">Stream of IMU frames.</param>
        /// <returns>Stream of IMU samples.</returns>
        public static IProducer<Vector3D> SelectManyImuSamples(this IProducer<(Vector3D Sample, DateTime OriginatingTime)[]> source)
        {
            return source.Process<(Vector3D Sample, DateTime OriginatingTime)[], Vector3D>((samples, envelope, emitter) =>
            {
                foreach (var sample in samples)
                {
                    emitter.Post(sample.Sample, sample.OriginatingTime);
                }
            });
        }
    }
}
