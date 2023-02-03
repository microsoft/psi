// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureInterop
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using MathNet.Spatial.Units;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;
    using static Microsoft.Psi.Diagnostics.PipelineDiagnostics;
    using Image = Microsoft.Psi.Imaging.Image;
    using OpenXRHand = Microsoft.Psi.MixedReality.OpenXR.Hand;
    using StereoKitHand = Microsoft.Psi.MixedReality.StereoKit.Hand;
    using WinRTEyes = Microsoft.Psi.MixedReality.WinRT.Eyes;

    /// <summary>
    /// Provides serializers and deserializers for the various mixed reality streams.
    /// </summary>
    public static class Serializers
    {
        private static readonly WaveFormat AssumedWaveFormat = WaveFormat.CreateIeeeFloat(48000, 1);

        /// <summary>
        /// Format for <see cref="SceneObjectCollection"/>.
        /// </summary>
        /// <returns><see cref="Format{SceneObjectCollection}"/> serializer/deserializer.</returns>
        public static Format<SceneObjectCollection> SceneObjectCollectionFormat()
            => new (WriteSceneObjectCollection, ReadSceneObjectCollection);

        /// <summary>
        /// Write <see cref="SceneObjectCollection"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="sceneObjectCollection"><see cref="SceneObjectCollection"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteSceneObjectCollection(SceneObjectCollection sceneObjectCollection, BinaryWriter writer)
        {
            void WriteSceneObject(SceneObjectCollection.SceneObject obj)
            {
                void WriteMeshes(List<Mesh3D> meshes)
                {
                    WriteCollection(meshes, writer, m =>
                    {
                        WriteCollection(m.Vertices, writer, WritePoint3D);
                        WriteCollection(m.TriangleIndices, writer, i => writer.Write(i));
                    });
                }

                if (obj != null)
                {
                    WriteMeshes(obj.Meshes);
                    WriteMeshes(obj.ColliderMeshes);
                    WriteCollection(obj.Rectangles, writer, WriteRectangle3D);
                    WriteCollection(obj.PlacementRectangles, writer, rect => WriteNullable(rect, writer, r => WriteRectangle3D(r, writer)));
                }
                else
                {
                    // treat null as empty collections
                    writer.Write(0); // empty meshes
                    writer.Write(0); // empty collider meshes
                    writer.Write(0); // empty rectangles
                    writer.Write(0); // empty placement rectangles
                }
            }

            WriteBool(sceneObjectCollection != null, writer);
            if (sceneObjectCollection == null)
            {
                return;
            }

            WriteSceneObject(sceneObjectCollection.Background);
            WriteSceneObject(sceneObjectCollection.Ceiling);
            WriteSceneObject(sceneObjectCollection.Floor);
            WriteSceneObject(sceneObjectCollection.Inferred);
            WriteSceneObject(sceneObjectCollection.Platform);
            WriteSceneObject(sceneObjectCollection.Unknown);
            WriteSceneObject(sceneObjectCollection.Wall);
            WriteSceneObject(sceneObjectCollection.World);
        }

        /// <summary>
        /// Read <see cref="SceneObjectCollection"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="SceneObjectCollection"/>.</returns>
        public static SceneObjectCollection ReadSceneObjectCollection(BinaryReader reader)
        {
            SceneObjectCollection.SceneObject ReadSceneObject()
            {
                List<Mesh3D> ReadMeshes()
                {
                    return ReadCollection(reader, () =>
                    {
                        var vertices = ReadCollection(reader, ReadPoint3D).ToArray();
                        var indices = ReadCollection(reader, reader.ReadUInt32).ToArray();
                        return new Mesh3D(vertices, indices);
                    }).ToList();
                }

                var meshes = ReadMeshes();
                var colliderMeshes = ReadMeshes();
                var rectangles = ReadCollection(reader, ReadRectangle3D).ToList();
                var placementRectangles = ReadCollection(reader, () => ReadNullable(reader, () => ReadRectangle3D(reader))).ToList();

                return new SceneObjectCollection.SceneObject(meshes, colliderMeshes, rectangles, placementRectangles);
            }

            if (!ReadBool(reader))
            {
                return null;
            }

            var background = ReadSceneObject();
            var ceiling = ReadSceneObject();
            var floor = ReadSceneObject();
            var inferred = ReadSceneObject();
            var platform = ReadSceneObject();
            var unknown = ReadSceneObject();
            var wall = ReadSceneObject();
            var world = ReadSceneObject();
            var scene = new SceneObjectCollection(
                background,
                ceiling,
                floor,
                inferred,
                platform,
                unknown,
                wall,
                world);
            return scene;
        }

        /// <summary>
        /// Format for <see cref="CoordinateSystem"/>.
        /// </summary>
        /// <returns><see cref="Format{CoordinateSystem}"/> serializer/deserializer.</returns>
        public static Format<CoordinateSystem> CoordinateSystemFormat()
            => new (WriteCoordinateSystem, ReadCoordinateSystem);

        /// <summary>
        /// Write <see cref="CoordinateSystem"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="coordinateSystem"><see cref="CoordinateSystem"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteCoordinateSystem(CoordinateSystem coordinateSystem, BinaryWriter writer)
        {
            WriteBool(coordinateSystem != null, writer);
            if (coordinateSystem == null)
            {
                return;
            }

            var m = coordinateSystem.AsColumnMajorArray();
            for (var i = 0; i < 16; i++)
            {
                if ((i + 1) % 4 != 0 /* not bottom row? */)
                {
                    writer.Write(m[i]);
                }
            }
        }

        /// <summary>
        /// Read <see cref="CoordinateSystem"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="CoordinateSystem"/>.</returns>
        public static CoordinateSystem ReadCoordinateSystem(BinaryReader reader)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var m = new double[12];
            for (var i = 0; i < 12; i++)
            {
                m[i] = reader.ReadDouble();
            }

            return new CoordinateSystem(
                Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { m[0], m[3], m[6], m[9] },
                    { m[1], m[4], m[7], m[10] },
                    { m[2], m[5], m[8], m[11] },
                    { 0, 0, 0, 1 },
                }));
        }

        /// <summary>
        /// Write <see cref="CoordinateSystemVelocity3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="coordinateSystemVelocity3D"><see cref="CoordinateSystemVelocity3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteCoordinateSystemVelocity3D(CoordinateSystemVelocity3D coordinateSystemVelocity3D, BinaryWriter writer)
        {
            WriteAngularVelocity3D(coordinateSystemVelocity3D.Angular, writer);
            WriteLinearVelocity3D(coordinateSystemVelocity3D.Linear, writer);
        }

        /// <summary>
        /// Read <see cref="CoordinateSystemVelocity3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="CoordinateSystemVelocity3D"/>.</returns>
        public static CoordinateSystemVelocity3D ReadCoordinateSystemVelocity3D(BinaryReader reader)
        {
            var angularVelocity3D = ReadAngularVelocity3D(reader);
            var linearVelocity3D = ReadLinearVelocity3D(reader);
            return new CoordinateSystemVelocity3D(angularVelocity3D, linearVelocity3D);
        }

        /// <summary>
        /// Write <see cref="AngularVelocity3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="angularVelocity3D"><see cref="AngularVelocity3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteAngularVelocity3D(AngularVelocity3D angularVelocity3D, BinaryWriter writer)
        {
            // Write the origin rotation
            WriteBool(angularVelocity3D.OriginRotation != null, writer);
            if (angularVelocity3D.OriginRotation != null)
            {
                var or = angularVelocity3D.OriginRotation.AsColumnMajorArray();
                for (var i = 0; i < 9; i++)
                {
                    writer.Write(or[i]);
                }
            }

            WriteUnitVector3D(angularVelocity3D.AxisDirection, writer);
            WriteAngle(angularVelocity3D.Magnitude, writer);
        }

        /// <summary>
        /// Read <see cref="AngularVelocity3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="AngularVelocity3D"/>.</returns>
        public static AngularVelocity3D ReadAngularVelocity3D(BinaryReader reader)
        {
            Matrix<double> originRotation = default;
            var hasOriginRotation = ReadBool(reader);
            if (hasOriginRotation)
            {
                var or = new double[9];
                for (var i = 0; i < 9; i++)
                {
                    or[i] = reader.ReadDouble();
                }

                originRotation =
                    Matrix<double>.Build.DenseOfArray(new double[,]
                    {
                        { or[0], or[3], or[6] },
                        { or[1], or[4], or[7] },
                        { or[2], or[5], or[8] },
                    });
            }

            var axisDirection = ReadUnitVector3D(reader);
            var magnitude = ReadAngle(reader);
            return hasOriginRotation ? new AngularVelocity3D(originRotation, axisDirection, magnitude) : default;
        }

        /// <summary>
        /// Write <see cref="LinearVelocity3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="linearVelocity3D"><see cref="LinearVelocity3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteLinearVelocity3D(LinearVelocity3D linearVelocity3D, BinaryWriter writer)
        {
            // Write the origin rotation
            WritePoint3D(linearVelocity3D.Origin, writer);
            WriteUnitVector3D(linearVelocity3D.Direction, writer);
            writer.Write(linearVelocity3D.Magnitude);
        }

        /// <summary>
        /// Read <see cref="LinearVelocity3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="LinearVelocity3D"/>.</returns>
        public static LinearVelocity3D ReadLinearVelocity3D(BinaryReader reader)
        {
            var origin = ReadPoint3D(reader);
            var direction = ReadUnitVector3D(reader);
            var magnitude = reader.ReadDouble();
            return new LinearVelocity3D(origin, direction, magnitude);
        }

        /// <summary>
        /// Format for <see cref="Ray3D"/>.
        /// </summary>
        /// <returns><see cref="Format{Ray3D}"/> serializer/deserializer.</returns>
        public static Format<Ray3D> Ray3DFormat()
            => new (WriteRay3D, ReadRay3D);

        /// <summary>
        /// Write <see cref="Ray3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="ray3d"><see cref="Ray3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteRay3D(Ray3D ray3d, BinaryWriter writer)
        {
            WritePoint3D(ray3d.ThroughPoint, writer);
            WriteVector3D(ray3d.Direction.ToVector3D(), writer);
        }

        /// <summary>
        /// Read <see cref="Ray3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Ray3D"/>.</returns>
        public static Ray3D ReadRay3D(BinaryReader reader)
        {
            var throughPoint = ReadPoint3D(reader);
            var direction = ReadVector3D(reader);
            return new Ray3D(throughPoint, direction);
        }

        /// <summary>
        /// Format for <see cref="CalibrationPointsMap"/>.
        /// </summary>
        /// <returns><see cref="Format{CalibrationPointsMap}"/> serializer/deserializer.</returns>
        public static Format<CalibrationPointsMap> CalibrationPointsMapFormat()
            => new (WriteCalibrationPointsMap, ReadCalibrationPointsMap);

        /// <summary>
        /// Write <see cref="CalibrationPointsMap"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="calibrationPointsMap"><see cref="CalibrationPointsMap"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteCalibrationPointsMap(CalibrationPointsMap calibrationPointsMap, BinaryWriter writer)
        {
            writer.Write(calibrationPointsMap.Width);
            writer.Write(calibrationPointsMap.Height);
            WriteCollection(calibrationPointsMap.CameraUnitPlanePoints, writer, f => writer.Write(f));
        }

        /// <summary>
        /// Read <see cref="CalibrationPointsMap"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="CoordinateSystem"/>.</returns>
        public static CalibrationPointsMap ReadCalibrationPointsMap(BinaryReader reader)
            => new ()
            {
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                CameraUnitPlanePoints = ReadCollection(reader, () => reader.ReadDouble()).ToArray(),
            };

        /// <summary>
        /// Format for <see cref="StereoKitHand"/>.
        /// </summary>
        /// <returns><see cref="Format{Hand}"/> serializer/deserializer.</returns>
        public static Format<StereoKitHand> StereoKitHandFormat()
            => new (WriteStereoKitHand, ReadStereoKitHand);

        /// <summary>
        /// Write <see cref="StereoKit.Hand"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="hand"><see cref="StereoKit.Hand"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteStereoKitHand(StereoKitHand hand, BinaryWriter writer)
        {
            WriteBool(hand != null, writer);
            if (hand == null)
            {
                return;
            }

            WriteBool(hand.IsTracked, writer);
            WriteBool(hand.IsPinched, writer);
            WriteBool(hand.IsGripped, writer);
            WriteCollection(hand.Joints, writer, WriteCoordinateSystem);
        }

        /// <summary>
        /// Read <see cref="StereoKitHand"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="StereoKitHand"/>.</returns>
        public static StereoKitHand ReadStereoKitHand(BinaryReader reader)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var isTracked = ReadBool(reader);
            var isPinched = ReadBool(reader);
            var isGripped = ReadBool(reader);
            var joints = ReadCollection(reader, ReadCoordinateSystem).ToArray();
            return new StereoKitHand(isTracked, isPinched, isGripped, joints);
        }

        /// <summary>
        /// Format for <see cref="ValueTuple{Hand, Hand}"/>.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of <see cref="ValueTuple{Hand, Hand}"/> serializer/deserializer.</returns>
        public static Format<(StereoKitHand Left, StereoKitHand Right)> StereoKitHandsFormat()
            => new (WriteStereoKitHands, ReadStereoKitHands);

        /// <summary>
        /// Write <see cref="ValueTuple{StereoKitHand, StereoKitHand}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="hands"><see cref="ValueTuple{StereoKitHand, StereoKitHand}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteStereoKitHands((StereoKitHand Left, StereoKitHand Right) hands, BinaryWriter writer)
        {
            WriteStereoKitHand(hands.Left, writer);
            WriteStereoKitHand(hands.Right, writer);
        }

        /// <summary>
        /// Read <see cref="ValueTuple{StereoKitHand, StereoKitHand}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="ValueTuple{StereoKitHand, StereoKitHand}"/>.</returns>
        public static (StereoKitHand Left, StereoKitHand Right) ReadStereoKitHands(BinaryReader reader)
        {
            return (ReadStereoKitHand(reader), ReadStereoKitHand(reader));
        }

        /// <summary>
        /// Format for <see cref="OpenXRHand"/>.
        /// </summary>
        /// <returns><see cref="Format{OpenXRHand}"/> serializer/deserializer.</returns>
        public static Format<OpenXRHand> OpenXRHandFormat()
            => new (WriteOpenXRHand, ReadOpenXRHand);

        /// <summary>
        /// Write <see cref="OpenXRHand"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="hand"><see cref="OpenXRHand"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteOpenXRHand(OpenXRHand hand, BinaryWriter writer)
        {
            WriteBool(hand != null, writer);
            if (hand == null)
            {
                return;
            }

            WriteBool(hand.IsActive, writer);
            WriteCollection(hand.Joints, writer, WriteCoordinateSystem);
            WriteCollection(hand.JointsValid, writer, WriteBool);
            WriteCollection(hand.JointsTracked, writer, WriteBool);
        }

        /// <summary>
        /// Read <see cref="OpenXRHand"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="OpenXRHand"/>.</returns>
        public static OpenXRHand ReadOpenXRHand(BinaryReader reader)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var isActive = ReadBool(reader);
            var joints = ReadCollection(reader, ReadCoordinateSystem).ToArray();
            var jointsValid = ReadCollection(reader, ReadBool).ToArray();
            var jointsTracked = ReadCollection(reader, ReadBool).ToArray();
            return new OpenXRHand(isActive, joints, jointsValid, jointsTracked);
        }

        /// <summary>
        /// Format for <see cref="WinRTEyes"/>.
        /// </summary>
        /// <returns><see cref="Format{WinRTEyes}"/> serializer/deserializer.</returns>
        public static Format<WinRTEyes> WinRTEyesFormat()
            => new (WriteWinRTEyes, ReadWinRTEyes);

        /// <summary>
        /// Write <see cref="WinRTEyes"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="eyes"><see cref="WinRTEyes"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteWinRTEyes(WinRTEyes eyes, BinaryWriter writer)
        {
            WriteBool(eyes != null, writer);
            if (eyes == null)
            {
                return;
            }

            WriteBool(eyes.CalibrationValid, writer);
            WriteNullable(eyes.GazeRay, writer, WriteRay3D);
        }

        /// <summary>
        /// Read <see cref="WinRTEyes"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="WinRTEyes"/>.</returns>
        public static WinRTEyes ReadWinRTEyes(BinaryReader reader)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var calibrationValid = ReadBool(reader);
            var gazeRay = ReadNullable(reader, ReadRay3D);
            return new WinRTEyes(gazeRay, calibrationValid);
        }

        /// <summary>
        /// Format for <see cref="ValueTuple{HandXR, HandXR}"/>.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of <see cref="ValueTuple{HandXR, HandXR}"/> serializer/deserializer.</returns>
        public static Format<(OpenXRHand Left, OpenXRHand Right)> OpenXRHandsFormat()
            => new (WriteOpenXRHands, ReadOpenXRHands);

        /// <summary>
        /// Write <see cref="ValueTuple{HandXR, HandXR}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="hands"><see cref="ValueTuple{HandXR, HandXR}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteOpenXRHands((OpenXRHand Left, OpenXRHand Right) hands, BinaryWriter writer)
        {
            WriteOpenXRHand(hands.Left, writer);
            WriteOpenXRHand(hands.Right, writer);
        }

        /// <summary>
        /// Read <see cref="ValueTuple{HandXR, HandXR}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="ValueTuple{HandXR, HandXR}"/>.</returns>
        public static (OpenXRHand Left, OpenXRHand Right) ReadOpenXRHands(BinaryReader reader)
        {
            return (ReadOpenXRHand(reader), ReadOpenXRHand(reader));
        }

        /// <summary>
        /// Format for <see cref="AudioBuffer"/>.
        /// </summary>
        /// <returns><see cref="Format{AudioBuffer}"/> serializer/deserializer.</returns>
        public static Format<AudioBuffer> AudioBufferFormat()
            => new (WriteAudioBuffer, ReadAudioBuffer);

        /// <summary>
        /// Write <see cref="AudioBuffer"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="audioBuffer"><see cref="AudioBuffer"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteAudioBuffer(AudioBuffer audioBuffer, BinaryWriter writer)
        {
            if (audioBuffer.Format.Channels != AssumedWaveFormat.Channels ||
                audioBuffer.Format.FormatTag != AssumedWaveFormat.FormatTag ||
                audioBuffer.Format.BitsPerSample != AssumedWaveFormat.BitsPerSample ||
                audioBuffer.Format.SamplesPerSec != AssumedWaveFormat.SamplesPerSec)
            {
                throw new ArgumentException("Unexpected audio format.");
            }

            writer.Write(audioBuffer.Length);
            writer.Write(audioBuffer.Data);
        }

        /// <summary>
        /// Read <see cref="AudioBuffer"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="AudioBuffer"/>.</returns>
        public static AudioBuffer ReadAudioBuffer(BinaryReader reader)
            => new (reader.ReadBytes(reader.ReadInt32()), AssumedWaveFormat);

        /// <summary>
        /// Format for <see cref="CameraIntrinsics"/>.
        /// </summary>
        /// <returns><see cref="Format{ICameraIntrinsics}"/> serializer/deserializer.</returns>
        public static Format<CameraIntrinsics> CameraIntrinsicsFormat()
            => new (WriteCameraIntrinsics, ReadCameraIntrinsics);

        /// <summary>
        /// Write <see cref="CameraIntrinsics"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="cameraIntrinsics"><see cref="CameraIntrinsics"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteCameraIntrinsics(ICameraIntrinsics cameraIntrinsics, BinaryWriter writer)
        {
            static void WriteVector(Vector<double> vector, int size, BinaryWriter writer)
            {
                if (vector.Count != size)
                {
                    throw new ArgumentException($"Expected vector of size {size} (actual: {vector.Count}.");
                }

                foreach (var v in vector)
                {
                    writer.Write(v);
                }
            }

            WriteBool(cameraIntrinsics != null, writer);
            if (cameraIntrinsics == null)
            {
                return;
            }

            writer.Write(cameraIntrinsics.ImageWidth);
            writer.Write(cameraIntrinsics.ImageHeight);
            WriteTransformMatrix(cameraIntrinsics.Transform, writer);
            WriteVector(cameraIntrinsics.RadialDistortion, 6, writer);
            WriteVector(cameraIntrinsics.TangentialDistortion, 2, writer);
            WriteBool(cameraIntrinsics.ClosedFormDistorts, writer);
        }

        /// <summary>
        /// Read <see cref="CameraIntrinsics"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="CameraIntrinsics"/>.</returns>
        public static CameraIntrinsics ReadCameraIntrinsics(BinaryReader reader)
        {
            static Vector<double> ReadVector(int size, BinaryReader reader)
            {
                var v = new double[size];
                for (var i = 0; i < size; i++)
                {
                    v[i] = reader.ReadDouble();
                }

                return Vector<double>.Build.DenseOfArray(v);
            }

            if (!ReadBool(reader))
            {
                return null;
            }

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var transform = ReadTransformMatrix(reader);
            var radialDistortion = ReadVector(6, reader);
            var tangentialDistortion = ReadVector(2, reader);
            var closedFormDistorts = ReadBool(reader);
            var intrinsics = new CameraIntrinsics(
                width,
                height,
                transform,
                radialDistortion,
                tangentialDistortion,
                closedFormDistorts);
            return intrinsics;
        }

        /// <summary>
        /// Format for encoded image camera views.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of encoded image camera view serializer/deserializer.</returns>
        public static Format<EncodedImageCameraView> EncodedImageCameraViewFormat()
            => new (WriteEncodedImageCameraView, (reader, payload, _, _) => ReadEncodedImageCameraView(reader, payload));

        /// <summary>
        /// Write a encoded image camera view to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="encodedImageCameraView">The encoded camera image view.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteEncodedImageCameraView(EncodedImageCameraView encodedImageCameraView, BinaryWriter writer)
        {
            WriteEncodedImage(encodedImageCameraView.ViewedObject, writer);
            WriteCameraIntrinsics(encodedImageCameraView.CameraIntrinsics, writer);
            WriteCoordinateSystem(encodedImageCameraView.CameraPose, writer);
        }

        /// <summary>
        /// Read encoded image camera view from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="payload">The payload of bytes.</param>
        /// <returns>The encoded image camera view.</returns>
        public static EncodedImageCameraView ReadEncodedImageCameraView(BinaryReader reader, byte[] payload)
        {
            using var sharedEncodedImage = ReadEncodedImage(reader, payload);
            var cameraIntrinsics = ReadCameraIntrinsics(reader);
            var cameraPose = ReadCoordinateSystem(reader);
            return new EncodedImageCameraView(sharedEncodedImage, cameraIntrinsics, cameraPose);
        }

        /// <summary>
        /// Format for <see cref="Shared{EncodedImage}"/>.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of <see cref="Shared{EncodedImage}"/> serializer/deserializer.</returns>
        public static Format<Shared<EncodedImage>> SharedEncodedImageFormat()
            => new (WriteEncodedImage, (reader, payload, _, _) => ReadEncodedImage(reader, payload));

        /// <summary>
        /// Write <see cref="Shared{EncodedImage}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="sharedEncodedImage"><see cref="Shared{EncodedImage}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteEncodedImage(Shared<EncodedImage> sharedEncodedImage, BinaryWriter writer)
        {
            WriteBool(sharedEncodedImage != null, writer);
            if (sharedEncodedImage == null)
            {
                return;
            }

            var image = sharedEncodedImage.Resource;
            var data = image.GetBuffer();
            writer.Write(image.Width);
            writer.Write(image.Height);
            writer.Write((int)image.PixelFormat);
            writer.Write(image.Size);
            writer.Write(data, 0, image.Size);
        }

        /// <summary>
        /// Read <see cref="Shared{EncodedImage}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="payload">The payload of bytes.</param>
        /// <returns><see cref="Shared{EncodedImage}"/>.</returns>
        public static Shared<EncodedImage> ReadEncodedImage(BinaryReader reader, byte[] payload)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var pixelFormat = (PixelFormat)reader.ReadInt32();
            var size = reader.ReadInt32();
            var image = EncodedImagePool.GetOrCreate(width, height, pixelFormat);
            int position = (int)reader.BaseStream.Position;
            image.Resource.CopyFrom(payload, position, size);
            reader.BaseStream.Position = position + size;
            return image;
        }

        /// <summary>
        /// Format for depth image camera views.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of depth image camera view serializer/deserializer.</returns>
        public static Format<DepthImageCameraView> DepthImageCameraViewFormat()
            => new (WriteDepthImageCameraView, (reader, payload, _, _) => ReadDepthImageCameraView(reader, payload));

        /// <summary>
        /// Write a shared depth image camera view to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="depthImageCameraView">The depth image camera view.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteDepthImageCameraView(DepthImageCameraView depthImageCameraView, BinaryWriter writer)
        {
            WriteDepthImage(depthImageCameraView.ViewedObject, writer);
            WriteCameraIntrinsics(depthImageCameraView.CameraIntrinsics, writer);
            WriteCoordinateSystem(depthImageCameraView.CameraPose, writer);
        }

        /// <summary>
        /// Read shared depth image camera view from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="payload">The payload of bytes.</param>
        /// <returns>The shared depth image camera view.</returns>
        public static DepthImageCameraView ReadDepthImageCameraView(BinaryReader reader, byte[] payload)
        {
            using var sharedDepthImage = ReadDepthImage(reader, payload);
            var cameraIntrinsics = ReadCameraIntrinsics(reader);
            var cameraPose = ReadCoordinateSystem(reader);
            return new DepthImageCameraView(sharedDepthImage, cameraIntrinsics, cameraPose);
        }

        /// <summary>
        /// Format for <see cref="Shared{DepthImage}"/>.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of <see cref="Shared{DepthImage}"/> serializer/deserializer.</returns>
        public static Format<Shared<DepthImage>> SharedDepthImageFormat()
            => new (WriteDepthImage, (reader, payload, _, _) => ReadDepthImage(reader, payload));

        /// <summary>
        /// Write <see cref="Shared{DepthImage}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="sharedDepthImage"><see cref="Shared{DepthImage}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteDepthImage(Shared<DepthImage> sharedDepthImage, BinaryWriter writer)
        {
            WriteBool(sharedDepthImage != null, writer);
            if (sharedDepthImage == null)
            {
                return;
            }

            var image = sharedDepthImage.Resource;
            writer.Write(image.Width);
            writer.Write(image.Height);
            writer.Write(image.Size);
            writer.Write((byte)image.DepthValueSemantics);
            writer.Write(image.DepthValueToMetersScaleFactor);
            writer.Write(image.ReadBytes(image.Size));
        }

        /// <summary>
        /// Read <see cref="Shared{DepthImage}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="payload">The payload of bytes.</param>
        /// <returns><see cref="Shared{DepthImage}"/>.</returns>
        public static Shared<DepthImage> ReadDepthImage(BinaryReader reader, byte[] payload)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var size = reader.ReadInt32();
            var depthValueSemantics = (DepthValueSemantics)reader.ReadByte();
            var depthValueToMetersScaleFactor = reader.ReadDouble();
            var image = DepthImagePool.GetOrCreate(width, height, depthValueSemantics, depthValueToMetersScaleFactor);
            int position = (int)reader.BaseStream.Position;
            image.Resource.CopyFrom(payload, position, size);
            reader.BaseStream.Position = position + size;
            return image;
        }

        /// <summary>
        /// Format for image camera views.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of image camera view serializer/deserializer.</returns>
        public static Format<ImageCameraView> ImageCameraViewFormat()
            => new (WriteImageCameraView, (reader, payload, _, _) => ReadImageCameraView(reader, payload));

        /// <summary>
        /// Write an image camera view to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="imageCameraView">The image camera view.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteImageCameraView(ImageCameraView imageCameraView, BinaryWriter writer)
        {
            WriteSharedImage(imageCameraView.ViewedObject, writer);
            WriteCameraIntrinsics(imageCameraView.CameraIntrinsics, writer);
            WriteCoordinateSystem(imageCameraView.CameraPose, writer);
        }

        /// <summary>
        /// Read an image camera view from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="payload">The payload of bytes.</param>
        /// <returns>The shared camera image view.</returns>
        public static ImageCameraView ReadImageCameraView(BinaryReader reader, byte[] payload)
        {
            var sharedImage = ReadSharedImage(reader, payload);
            var cameraIntrinsics = ReadCameraIntrinsics(reader);
            var cameraPose = ReadCoordinateSystem(reader);
            return new ImageCameraView(sharedImage, cameraIntrinsics, cameraPose);
        }

        /// <summary>
        /// Format for <see cref="Shared{Image}"/>.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of <see cref="Shared{Image}"/> serializer/deserializer.</returns>
        public static Format<Shared<Image>> SharedImageFormat()
            => new (WriteSharedImage, (reader, payload, _, _) => ReadSharedImage(reader, payload));

        /// <summary>
        /// Write <see cref="Shared{Image}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="sharedImage"><see cref="Shared{Image}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteSharedImage(Shared<Image> sharedImage, BinaryWriter writer)
        {
            WriteBool(sharedImage != null, writer);
            if (sharedImage == null)
            {
                return;
            }

            var image = sharedImage.Resource;
            writer.Write(image.Width);
            writer.Write(image.Height);
            writer.Write((int)image.PixelFormat);
            writer.Write(image.Size);
            writer.Write(image.ReadBytes(image.Size));
        }

        /// <summary>
        /// Read <see cref="Shared{Image}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="payload">The payload of bytes.</param>
        /// <returns><see cref="Shared{Image}"/>.</returns>
        public static Shared<Image> ReadSharedImage(BinaryReader reader, byte[] payload)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var pixelFormat = (PixelFormat)reader.ReadInt32();
            var size = reader.ReadInt32();
            var image = ImagePool.GetOrCreate(width, height, pixelFormat);
            int position = (int)reader.BaseStream.Position;
            image.Resource.CopyFrom(payload, position, size);
            reader.BaseStream.Position = position + size;
            return image;
        }

        /// <summary>
        /// Format for <see cref="int"/>.
        /// </summary>
        /// <returns><see cref="Format{Int32}"/> serializer/deserializer.</returns>
        public static Format<int> Int32Format()
        {
            return new (
                (value, writer) => writer.Write(value),
                (reader) => reader.ReadInt32());
        }

        /// <summary>
        /// Format for <see cref="PipelineDiagnostics"/>.
        /// </summary>
        /// <returns><see cref="Format{PipelineDiagnostics}"/> serializer/deserializer.</returns>
        public static Format<PipelineDiagnostics> PipelineDiagnosticsFormat()
        {
            /* Pipeline diagnostics structure is a graph with many cycles. The strategy to serialize/deserialize
             * this is to use mutually recursive write/read functions and hashsets/dictionaries of known entities.
             * We write out whole entities (emitters/receivers/elements/...) the first time they're seen while
             * walking the graph, but then to write out only the ID when seen again in the future. Then, while
             * deserializing, we remember entities that have already been deserialized and upon seeing an ID for
             * which we have a cached entity, we stop and return the cached instance. One catch is that instanced
             * may be in the process of being deserialized when referenced to them are seen. In this case, a
             * partially complete instance is used that is later completed by the time deserialization has finished.
             */
            return new (
                (pipelineDiagnostics, writer) =>
                {
                    WriteBool(pipelineDiagnostics != null, writer);
                    if (pipelineDiagnostics == null)
                    {
                        return;
                    }

                    var knownEmitterIds = new HashSet<int>();
                    var knownReceiverIds = new HashSet<int>();
                    var knownPipelineElementIds = new HashSet<int>();
                    var knownPipelineIds = new HashSet<int>();

                    void WriteEmitterDiagnostics(EmitterDiagnostics emitterDiagnostics)
                    {
                        WriteBool(emitterDiagnostics != null, writer);
                        if (emitterDiagnostics == null)
                        {
                            return;
                        }

                        writer.Write(emitterDiagnostics.Id);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownEmitterIds.Contains(emitterDiagnostics.Id))
                        {
                            knownEmitterIds.Add(emitterDiagnostics.Id);
                            WriteString(emitterDiagnostics.Name, writer);
                            WriteString(emitterDiagnostics.Type, writer);
                            WritePipelineElementDiagnostics(emitterDiagnostics.PipelineElement);
                            WriteCollection(emitterDiagnostics.Targets, writer, WriteReceiverDiagnostics);
                        }
                    }

                    void WriteReceiverDiagnostics(ReceiverDiagnostics receiverDiagnostics)
                    {
                        WriteBool(receiverDiagnostics != null, writer);
                        if (receiverDiagnostics == null)
                        {
                            return;
                        }

                        writer.Write(receiverDiagnostics.Id);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownReceiverIds.Contains(receiverDiagnostics.Id))
                        {
                            knownReceiverIds.Add(receiverDiagnostics.Id);
                            WriteString(receiverDiagnostics.ReceiverName, writer);
                            WriteString(receiverDiagnostics.DeliveryPolicyName, writer);
                            WriteString(receiverDiagnostics.TypeName, writer);
                            WriteBool(receiverDiagnostics.ReceiverIsThrottled, writer);
                            writer.Write(receiverDiagnostics.LastDeliveryQueueSize);
                            writer.Write(receiverDiagnostics.AvgDeliveryQueueSize);
                            writer.Write(receiverDiagnostics.TotalMessageEmittedCount);
                            writer.Write(receiverDiagnostics.WindowMessageEmittedCount);
                            writer.Write(receiverDiagnostics.TotalMessageProcessedCount);
                            writer.Write(receiverDiagnostics.WindowMessageProcessedCount);
                            writer.Write(receiverDiagnostics.TotalMessageDroppedCount);
                            writer.Write(receiverDiagnostics.WindowMessageDroppedCount);
                            writer.Write(receiverDiagnostics.LastMessageCreatedLatency);
                            writer.Write(receiverDiagnostics.AvgMessageCreatedLatency);
                            writer.Write(receiverDiagnostics.LastMessageEmittedLatency);
                            writer.Write(receiverDiagnostics.AvgMessageEmittedLatency);
                            writer.Write(receiverDiagnostics.LastMessageReceivedLatency);
                            writer.Write(receiverDiagnostics.AvgMessageReceivedLatency);
                            writer.Write(receiverDiagnostics.LastMessageProcessTime);
                            writer.Write(receiverDiagnostics.AvgMessageProcessTime);
                            writer.Write(receiverDiagnostics.LastMessageSize);
                            writer.Write(receiverDiagnostics.AvgMessageSize);
                            WritePipelineElementDiagnostics(receiverDiagnostics.PipelineElement);
                            WriteEmitterDiagnostics(receiverDiagnostics.Source);
                        }
                    }

                    void WritePipelineElementDiagnostics(PipelineElementDiagnostics pipelineElementDiagnostics)
                    {
                        WriteBool(pipelineElementDiagnostics != null, writer);
                        if (pipelineElementDiagnostics == null)
                        {
                            return;
                        }

                        writer.Write(pipelineElementDiagnostics.Id);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownPipelineElementIds.Contains(pipelineElementDiagnostics.Id))
                        {
                            knownPipelineElementIds.Add(pipelineElementDiagnostics.Id);
                            WriteString(pipelineElementDiagnostics.Name, writer);
                            WriteString(pipelineElementDiagnostics.TypeName, writer);
                            writer.Write((int)pipelineElementDiagnostics.Kind);
                            WriteBool(pipelineElementDiagnostics.IsRunning, writer);
                            WriteBool(pipelineElementDiagnostics.Finalized, writer);
                            WriteString(pipelineElementDiagnostics.DiagnosticState, writer);
                            writer.Write(pipelineElementDiagnostics.PipelineId);
                            WriteCollection(pipelineElementDiagnostics.Emitters, writer, WriteEmitterDiagnostics);
                            WriteCollection(pipelineElementDiagnostics.Receivers, writer, WriteReceiverDiagnostics);
                            WritePipelineDiagnostics(pipelineElementDiagnostics.RepresentsSubpipeline);
                            WritePipelineElementDiagnostics(pipelineElementDiagnostics.ConnectorBridgeToPipelineElement);
                        }
                    }

                    void WritePipelineDiagnostics(PipelineDiagnostics diagnostics)
                    {
                        WriteBool(diagnostics != null, writer);
                        if (diagnostics == null)
                        {
                            return;
                        }

                        writer.Write(diagnostics.Id);

                        // serialize only if an instance with this ID has not already been serialized
                        if (!knownPipelineIds.Contains(diagnostics.Id))
                        {
                            knownPipelineIds.Add(diagnostics.Id);
                            WriteString(diagnostics.Name, writer);
                            WriteBool(diagnostics.IsPipelineRunning, writer);
                            WritePipelineDiagnostics(diagnostics.ParentPipelineDiagnostics);
                            WriteCollection(diagnostics.SubpipelineDiagnostics, writer, WritePipelineDiagnostics);
                            WriteCollection(diagnostics.PipelineElements, writer, WritePipelineElementDiagnostics);
                        }
                    }

                    WritePipelineDiagnostics(pipelineDiagnostics);
                },
                (reader) =>
                {
                    if (!ReadBool(reader))
                    {
                        return null;
                    }

                    var knownEmitters = new Dictionary<int, EmitterDiagnostics>();
                    var knownReceivers = new Dictionary<int, ReceiverDiagnostics>();
                    var knownPipelineElements = new Dictionary<int, PipelineElementDiagnostics>();
                    var knownPipelines = new Dictionary<int, PipelineDiagnostics>();

                    EmitterDiagnostics ReadEmitterDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = reader.ReadInt32();
                        if (knownEmitters.TryGetValue(id, out var emitter))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return emitter;
                        }

                        // construct and add a partially complete instance before recursing
                        emitter = new EmitterDiagnostics(
                            id,
                            ReadString(reader), // name
                            ReadString(reader), // type
                            null, // pipelineElement
                            null); // targets
                        knownEmitters.Add(id, emitter);

                        // recurse and complete assignment of instance fields
                        emitter.PipelineElement = ReadPipelineElementDiagnostics();
                        emitter.Targets = ReadCollection(reader, ReadReceiverDiagnostics).ToArray();

                        return emitter;
                    }

                    ReceiverDiagnostics ReadReceiverDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = reader.ReadInt32();
                        if (knownReceivers.TryGetValue(id, out var receiver))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return receiver;
                        }

                        // construct and add a partially complete instance before recursing
                        receiver = new ReceiverDiagnostics(
                            id,
                            ReadString(reader), // receiverName
                            ReadString(reader), // deliverPolicyName
                            ReadString(reader), // typeName
                            ReadBool(reader), // receiverIsThrottled
                            reader.ReadDouble(), // lastDeliveryQueueSize
                            reader.ReadDouble(), // avgDeliveryQueueSize
                            reader.ReadInt32(), // totalMessageEmittedCount
                            reader.ReadInt32(), // windowMessageEmittedCount
                            reader.ReadInt32(), // totalMessageProcessedCount
                            reader.ReadInt32(), // windowMessageProcessedCount
                            reader.ReadInt32(), // totalMessageDroppedCount
                            reader.ReadInt32(), // windowMessageDroppedCount
                            reader.ReadDouble(), // lastMessageCreatedLatency
                            reader.ReadDouble(), // avgMessageCreatedLatency
                            reader.ReadDouble(), // lastMessageEmittedLatency
                            reader.ReadDouble(), // avgMessageEmittedLatency
                            reader.ReadDouble(), // lastMessageReceivedLatency
                            reader.ReadDouble(), // avgMessageReceivedLatency
                            reader.ReadDouble(), // lastMessageProcessTime
                            reader.ReadDouble(), // avgMessageProcessTime
                            reader.ReadDouble(), // lastMessageSize
                            reader.ReadDouble(), // avgMessageSize
                            null, // pipelineElement
                            null); // source
                        knownReceivers.Add(id, receiver);

                        // recurse and complete assignment of instance fields
                        receiver.PipelineElement = ReadPipelineElementDiagnostics();
                        receiver.Source = ReadEmitterDiagnostics();

                        return receiver;
                    }

                    PipelineElementDiagnostics ReadPipelineElementDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = reader.ReadInt32();
                        if (knownPipelineElements.TryGetValue(id, out var pipelineElement))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return pipelineElement;
                        }

                        // construct and add a partially complete instance before recursing
                        pipelineElement = new PipelineElementDiagnostics(
                            id,
                            ReadString(reader), // name
                            ReadString(reader), // typeName
                            (PipelineElementKind)reader.ReadInt32(), // kind
                            ReadBool(reader), // isRunning
                            ReadBool(reader), // finalized
                            ReadString(reader), // diagnosticState
                            reader.ReadInt32(), // pipelineId
                            null, // emitters
                            null, // receivers
                            null, // representsSubpipeline
                            null); // connectorBridgeToPipelineElement
                        knownPipelineElements.Add(id, pipelineElement);

                        // recurse and complete assignment of instance fields
                        pipelineElement.Emitters = ReadCollection(reader, ReadEmitterDiagnostics).ToArray();
                        pipelineElement.Receivers = ReadCollection(reader, ReadReceiverDiagnostics).ToArray();
                        pipelineElement.RepresentsSubpipeline = ReadPipelineDiagnostics();
                        pipelineElement.ConnectorBridgeToPipelineElement = ReadPipelineElementDiagnostics();

                        return pipelineElement;
                    }

                    PipelineDiagnostics ReadPipelineDiagnostics()
                    {
                        if (!ReadBool(reader))
                        {
                            return null;
                        }

                        var id = reader.ReadInt32();
                        if (knownPipelines.TryGetValue(id, out var pipeline))
                        {
                            // if this ID is already known, merely return the known instance (potentially partially complete)
                            return pipeline;
                        }

                        // construct and add a partially complete instance before recursing
                        pipeline = new PipelineDiagnostics(
                            id,
                            ReadString(reader), // name
                            ReadBool(reader), // isPipelineRunning
                            null, // parentPipelineDiagnostics
                            null, // subpipelineDiagnostics
                            null); // pipelineElements
                        knownPipelines.Add(id, pipeline);

                        // recurse and complete assignment of instance fields
                        pipeline.ParentPipelineDiagnostics = ReadPipelineDiagnostics();
                        pipeline.SubpipelineDiagnostics = ReadCollection(reader, ReadPipelineDiagnostics).ToArray();
                        pipeline.PipelineElements = ReadCollection(reader, ReadPipelineElementDiagnostics).ToArray();

                        return pipeline;
                    }

                    return ReadPipelineDiagnostics();
                });
        }

        /// <summary>
        /// Format for <see cref="DateTime"/>.
        /// </summary>
        /// <returns><see cref="Format{DateTime}"/> serializer/deserializer.</returns>
        public static Format<DateTime> DateTimeFormat()
            => new (WriteDateTime, ReadDateTime);

        /// <summary>
        /// Write <see cref="DateTime"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="dateTime"><see cref="DateTime"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteDateTime(DateTime dateTime, BinaryWriter writer)
            => writer.Write(dateTime.ToFileTimeUtc());

        /// <summary>
        /// Read <see cref="DateTime"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="DateTime"/>.</returns>
        public static DateTime ReadDateTime(BinaryReader reader)
            => DateTime.FromFileTimeUtc(reader.ReadInt64());

        /// <summary>
        /// Format for heartbeat of <see cref="ValueTuple{Single, Single}"/>.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of <see cref="ValueTuple{Single, Single}"/> serializer/deserializer.</returns>
        public static Format<(float VideoFps, float DepthFps)> HeartbeatFormat()
            => new (WriteHeartbeat, ReadHeartbeat);

        /// <summary>
        /// Write heartbeat of <see cref="ValueTuple{Single, Single}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="heartbeat"><see cref="ValueTuple{Single, Single}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteHeartbeat((float VideoFps, float DepthFps) heartbeat, BinaryWriter writer)
        {
            writer.Write(heartbeat.VideoFps);
            writer.Write(heartbeat.DepthFps);
        }

        /// <summary>
        /// Read heartbeat of <see cref="ValueTuple{Single, Single}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="ValueTuple{Single, Single}"/>.</returns>
        public static (float VideoFps, float DepthFps) ReadHeartbeat(BinaryReader reader)
        {
            var videoFps = reader.ReadSingle();
            var depthFps = reader.ReadSingle();
            return (videoFps, depthFps);
        }

        /// <summary>
        /// Format for <see cref="Color"/>.
        /// </summary>
        /// <returns><see cref="Format{Color}"/> serializer/deserializer.</returns>
        public static Format<Color> ColorFormat()
            => new (WriteColor, ReadColor);

        /// <summary>
        /// Write <see cref="Color"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="color"><see cref="Color"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteColor(Color color, BinaryWriter writer)
            => writer.Write(color.ToArgb());

        /// <summary>
        /// Read <see cref="Color"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Color"/>.</returns>
        public static Color ReadColor(BinaryReader reader)
            => Color.FromArgb(reader.ReadInt32());

        /// <summary>
        /// Format for IMU data of <see cref="T:ValueTuple{Vector3D, DateTime}[]"/>.
        /// </summary>
        /// <returns><see cref="Format{T}"/> of <see cref="T:ValueTuple{Vector3D, DateTime}[]"/> serializer/deserializer.</returns>
        public static Format<(Vector3D Sample, DateTime OriginatingTIme)[]> ImuFormat()
            => new (WriteImu, ReadImu);

        /// <summary>
        /// Write IMU data of <see cref="T:ValueTuple{Vector3D, DateTime}[]"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="imu"><see cref="T:ValueTuple{Vector3D, DateTime}[]"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteImu((Vector3D Value, DateTime OriginatingTime)[] imu, BinaryWriter writer)
        {
            WriteCollection(imu, writer, sample =>
            {
                WriteVector3D(sample.Value, writer);
                WriteDateTime(sample.OriginatingTime, writer);
            });
        }

        /// <summary>
        /// Read IMU data of <see cref="T:ValueTuple{Vector3D, DateTime}[]"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="T:ValueTuple{Vector3D, DateTime}[]"/>.</returns>
        public static (Vector3D Value, DateTime OriginatingTime)[] ReadImu(BinaryReader reader)
        {
            return ReadCollection(reader, () =>
            {
                var vector3D = ReadVector3D(reader);
                var originatingTime = ReadDateTime(reader);
                return (vector3D, originatingTime);
            }).ToArray();
        }

        /// <summary>
        /// Write <see cref="Vector3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="vector3D"><see cref="Vector3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteVector3D(Vector3D vector3D, BinaryWriter writer)
        {
            writer.Write(vector3D.X);
            writer.Write(vector3D.Y);
            writer.Write(vector3D.Z);
        }

        /// <summary>
        /// Read <see cref="Vector3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Vector3D"/>.</returns>
        public static Vector3D ReadVector3D(BinaryReader reader)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            return new Vector3D(x, y, z);
        }

        /// <summary>
        /// Write <see cref="UnitVector3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="unitVector3D"><see cref="UnitVector3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteUnitVector3D(UnitVector3D unitVector3D, BinaryWriter writer)
        {
            writer.Write(unitVector3D.X);
            writer.Write(unitVector3D.Y);
            writer.Write(unitVector3D.Z);
        }

        /// <summary>
        /// Read <see cref="UnitVector3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="UnitVector3D"/>.</returns>
        public static UnitVector3D ReadUnitVector3D(BinaryReader reader)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            if (x == 0 && y == 0 && z == 0)
            {
                return default;
            }
            else
            {
                return UnitVector3D.Create(x, y, z);
            }
        }

        /// <summary>
        /// Write <see cref="Angle"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="angle"><see cref="Angle"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteAngle(Angle angle, BinaryWriter writer)
        {
            writer.Write(angle.Radians);
        }

        /// <summary>
        /// Read <see cref="Angle"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Angle"/>.</returns>
        public static Angle ReadAngle(BinaryReader reader)
        {
            return Angle.FromRadians(reader.ReadDouble());
        }

        /// <summary>
        /// Format for <see cref="bool"/>.
        /// </summary>
        /// <returns><see cref="Format{Bool}"/> serializer/deserializer.</returns>
        public static Format<bool> BoolFormat()
            => new (WriteBool, ReadBool);

        /// <summary>
        /// Write <see cref="bool"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="boolean"><see cref="bool"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteBool(bool boolean, BinaryWriter writer)
            => writer.Write((byte)(boolean ? 0xff : 0));

        /// <summary>
        /// Read <see cref="bool"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="bool"/>.</returns>
        public static bool ReadBool(BinaryReader reader)
            => reader.ReadByte() == 0xff;

        /// <summary>
        /// Format for <see cref="Guid"/>.
        /// </summary>
        /// <returns><see cref="Format{Guid}"/> serializer/deserializer.</returns>
        public static Format<Guid> GuidFormat()
            => new (WriteGuid, ReadGuid);

        /// <summary>
        /// Write <see cref="Guid"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="guid"><see cref="Guid"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteGuid(Guid guid, BinaryWriter writer)
            => WriteString(guid.ToString(), writer);

        /// <summary>
        /// Read <see cref="Guid"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Guid"/>.</returns>
        public static Guid ReadGuid(BinaryReader reader)
            => Guid.Parse(ReadString(reader));

        /// <summary>
        /// Write <see cref="Matrix{Double}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="matrix"><see cref="Matrix{Double}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteTransformMatrix(Matrix<double> matrix, BinaryWriter writer)
        {
            WriteBool(matrix != null, writer);
            if (matrix == null)
            {
                return;
            }

            var m = matrix.AsColumnMajorArray();
            for (var i = 0; i < 9; i++)
            {
                if ((i + 1) % 3 != 0 /* not bottom row? */)
                {
                    writer.Write(m[i]);
                }
            }
        }

        /// <summary>
        /// Read <see cref="Matrix{Double}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Matrix{Double}"/>.</returns>
        public static Matrix<double> ReadTransformMatrix(BinaryReader reader)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            var m = new double[6];
            for (var i = 0; i < 6; i++)
            {
                m[i] = reader.ReadDouble();
            }

            var matrix = Matrix<double>.Build.Dense(3, 3, 0);
            matrix[0, 0] = m[0];
            matrix[1, 0] = m[1];
            matrix[0, 1] = m[2];
            matrix[1, 1] = m[3];
            matrix[0, 2] = m[4];
            matrix[1, 2] = m[5];
            matrix[2, 2] = 1;

            return matrix;
        }

        /// <summary>
        /// Format for <see cref="Box3D"/>.
        /// </summary>
        /// <returns><see cref="Format{Box3D}"/> serializer/deserializer.</returns>
        public static Format<Box3D> Box3DFormat()
            => new (WriteBox3D, ReadBox3D);

        /// <summary>
        /// Write <see cref="Box3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="box3D"><see cref="Box3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteBox3D(Box3D box3D, BinaryWriter writer)
        {
            WriteBounds3D(box3D.Bounds, writer);
            WriteCoordinateSystem(box3D.Pose, writer);
        }

        /// <summary>
        /// Read <see cref="Box3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Box3D"/>.</returns>
        public static Box3D ReadBox3D(BinaryReader reader)
            => new (ReadBounds3D(reader), ReadCoordinateSystem(reader));

        /// <summary>
        /// Format for <see cref="Bounds3D"/>.
        /// </summary>
        /// <returns><see cref="Format{Bounds3D}"/> serializer/deserializer.</returns>
        public static Format<Bounds3D> Bounds3DFormat()
            => new (WriteBounds3D, ReadBounds3D);

        /// <summary>
        /// Write <see cref="Bounds3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="bounds3D"><see cref="Bounds3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteBounds3D(Bounds3D bounds3D, BinaryWriter writer)
        {
            writer.Write(bounds3D.Min.X);
            writer.Write(bounds3D.Min.Y);
            writer.Write(bounds3D.Min.Z);
            writer.Write(bounds3D.Max.X);
            writer.Write(bounds3D.Max.Y);
            writer.Write(bounds3D.Max.Z);
        }

        /// <summary>
        /// Read <see cref="Bounds3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Bounds3D"/>.</returns>
        public static Bounds3D ReadBounds3D(BinaryReader reader)
        {
            var minX = reader.ReadDouble();
            var minY = reader.ReadDouble();
            var minZ = reader.ReadDouble();
            var maxX = reader.ReadDouble();
            var maxY = reader.ReadDouble();
            var maxZ = reader.ReadDouble();
            return new Bounds3D(minX, maxX, minY, maxY, minZ, maxZ);
        }

        /// <summary>
        /// Write <see cref="Point3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="point3D"><see cref="Point3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WritePoint3D(Point3D point3D, BinaryWriter writer)
        {
            writer.Write(point3D.X);
            writer.Write(point3D.Y);
            writer.Write(point3D.Z);
        }

        /// <summary>
        /// Read <see cref="Point3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Point3D"/>.</returns>
        public static Point3D ReadPoint3D(BinaryReader reader)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            var z = reader.ReadDouble();
            return new Point3D(x, y, z);
        }

        /// <summary>
        /// Write <see cref="Point2D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="point2D"><see cref="Point2D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WritePoint2D(Point2D point2D, BinaryWriter writer)
        {
            writer.Write(point2D.X);
            writer.Write(point2D.Y);
        }

        /// <summary>
        /// Read <see cref="Point2D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Point2D"/>.</returns>
        public static Point2D ReadPoint2D(BinaryReader reader)
        {
            var x = reader.ReadDouble();
            var y = reader.ReadDouble();
            return new Point2D(x, y);
        }

        /// <summary>
        /// Write <see cref="Rectangle3D"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="rectangle3D"><see cref="Rectangle3D"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteRectangle3D(Rectangle3D rectangle3D, BinaryWriter writer)
        {
            // note: persisting 3 points. Remaining properties inferred (TopRight, Width, Height, IsDegenerate)
            WritePoint3D(rectangle3D.TopLeft, writer);
            WritePoint3D(rectangle3D.BottomLeft, writer);
            WritePoint3D(rectangle3D.BottomRight, writer);
        }

        /// <summary>
        /// Read <see cref="Rectangle3D"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="Rectangle3D"/>.</returns>
        public static Rectangle3D ReadRectangle3D(BinaryReader reader)
        {
            var topLeft = ReadPoint3D(reader);
            var bottomLeft = ReadPoint3D(reader);
            var bottomRight = ReadPoint3D(reader);
            return new Rectangle3D(
                bottomLeft, // assumed origin
                (bottomRight - bottomLeft).Normalize(), // width axis
                (topLeft - bottomLeft).Normalize(), // height axis
                0, // left
                0, // bottom
                bottomLeft.DistanceTo(bottomRight), // width
                bottomLeft.DistanceTo(topLeft)); // height
        }

        /// <summary>
        /// Format for <see cref="string"/>.
        /// </summary>
        /// <returns><see cref="Format{String}"/> serializer/deserializer.</returns>
        public static Format<string> StringFormat()
            => new (WriteString, ReadString);

        /// <summary>
        /// Write optional string (may be null) to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="str">String value to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteString(string str, BinaryWriter writer)
        {
            WriteBool(str != null, writer);
            if (str == null)
            {
                return;
            }

            writer.Write(str);
        }

        /// <summary>
        /// Read optional string (may be null) from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns>Optional value (may be null).</returns>
        public static string ReadString(BinaryReader reader)
        {
            if (!ReadBool(reader))
            {
                return null;
            }

            return reader.ReadString();
        }

        /// <summary>
        /// Write <see cref="Nullable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of nullable value.</typeparam>
        /// <param name="value">Value to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T}"/> used to write each element.</param>
        public static void WriteNullable<T>(T? value, BinaryWriter writer, Action<T> writeAction)
            where T : struct
        {
            WriteBool(value.HasValue, writer);
            if (value.HasValue)
            {
                writeAction(value.Value);
            }
        }

        /// <summary>
        /// Write <see cref="Nullable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T, BinaryWriter}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of nullable value.</typeparam>
        /// <param name="value">Value to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T, BinaryWriter}"/> used to write each element.</param>
        public static void WriteNullable<T>(T? value, BinaryWriter writer, Action<T, BinaryWriter> writeAction)
            where T : struct
            => WriteNullable(value, writer, v => writeAction(v, writer));

        /// <summary>
        /// Read <see cref="Nullable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static T? ReadNullable<T>(BinaryReader reader, Func<T> readFunc)
            where T : struct
        {
            return ReadBool(reader) ? readFunc() : null;
        }

        /// <summary>
        /// Read <see cref="Nullable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{BinaryReader, T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{BinaryReader, T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static T? ReadNullable<T>(BinaryReader reader, Func<BinaryReader, T> readFunc)
            where T : struct
            => ReadNullable(reader, () => readFunc(reader));

        /// <summary>
        /// Write <see cref="IEnumerable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="collection"><see cref="IEnumerable{T}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T}"/> used to write each element.</param>
        public static void WriteCollection<T>(IEnumerable<T> collection, BinaryWriter writer, Action<T> writeAction)
        {
            if (collection == null)
            {
                WriteBool(false, writer);
            }
            else
            {
                WriteBool(true, writer);
                var len = collection.Count();
                writer.Write(len);
                foreach (var value in collection)
                {
                    writeAction(value);
                }
            }
        }

        /// <summary>
        /// Write <see cref="IEnumerable{T}"/> to <see cref="BinaryWriter"/> using an <see cref="Action{T, BinaryWriter}"/> to write each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="collection"><see cref="IEnumerable{T}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeAction"><see cref="Action{T, BinaryWriter}"/> used to write each element.</param>
        public static void WriteCollection<T>(IEnumerable<T> collection, BinaryWriter writer, Action<T, BinaryWriter> writeAction)
            => WriteCollection(collection, writer, c => writeAction(c, writer));

        /// <summary>
        /// Read <see cref="IEnumerable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static IEnumerable<T> ReadCollection<T>(BinaryReader reader, Func<T> readFunc)
        {
            if (!ReadBool(reader))
            {
                return null;
            }
            else
            {
                var len = reader.ReadInt32();
                var collection = new T[len];
                for (var i = 0; i < len; i++)
                {
                    collection[i] = readFunc();
                }

                return collection;
            }
        }

        /// <summary>
        /// Read <see cref="IEnumerable{T}"/> from <see cref="BinaryReader"/> using a <see cref="Func{BinaryReader, T}"/> to read each element.
        /// </summary>
        /// <typeparam name="T">Type of collection elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readFunc"><see cref="Func{BinaryReader, T}"/> used to read each element.</param>
        /// <returns><see cref="IEnumerable{T}"/> of elements.</returns>
        public static IEnumerable<T> ReadCollection<T>(BinaryReader reader, Func<BinaryReader, T> readFunc)
            => ReadCollection(reader, () => readFunc(reader));

        /// <summary>
        /// Write <see cref="Dictionary{TKey, TValue}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="dictionary"><see cref="Dictionary{TKey, TValue}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeKeyAction"><see cref="Action{TKey}"/> used to write each key element.</param>
        /// <param name="writeValueAction"><see cref="Action{TValue}"/> used to write each value element.</param>
        public static void WriteDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> dictionary,
            BinaryWriter writer,
            Action<TKey> writeKeyAction,
            Action<TValue> writeValueAction)
        {
            if (dictionary == null)
            {
                WriteBool(false, writer);
            }
            else
            {
                WriteBool(true, writer);
                writer.Write(dictionary.Count);
                foreach (var kvp in dictionary)
                {
                    writeKeyAction(kvp.Key);
                    writeValueAction(kvp.Value);
                }
            }
        }

        /// <summary>
        /// Write <see cref="Dictionary{TKey, TValue}"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="dictionary"><see cref="Dictionary{TKey, TValue}"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        /// <param name="writeKeyAction"><see cref="Action{TKey, BinaryWriter}"/> used to write each key element.</param>
        /// <param name="writeValueAction"><see cref="Action{TValue, BinaryWriter}"/> used to write each value element.</param>
        public static void WriteDictionary<TKey, TValue>(
            Dictionary<TKey, TValue> dictionary,
            BinaryWriter writer,
            Action<TKey, BinaryWriter> writeKeyAction,
            Action<TValue, BinaryWriter> writeValueAction)
            => WriteDictionary(dictionary, writer, d => writeKeyAction(d, writer), d => writeValueAction(d, writer));

        /// <summary>
        /// Read <see cref="Dictionary{TKey, TValue}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readKeyFunc"><see cref="Func{TKey}"/> used to read each key element.</param>
        /// <param name="readValueFunc"><see cref="Func{TValue}"/> used to read each value element.</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BinaryReader reader,
            Func<TKey> readKeyFunc,
            Func<TValue> readValueFunc)
        {
            if (!ReadBool(reader))
            {
                return null;
            }
            else
            {
                var result = new Dictionary<TKey, TValue>();
                var count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    result.Add(readKeyFunc(), readValueFunc());
                }

                return result;
            }
        }

        /// <summary>
        /// Read <see cref="Dictionary{TKey, TValue}"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="TKey">Type of dictionary key elements.</typeparam>
        /// <typeparam name="TValue">Type of dictionary value elements.</typeparam>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <param name="readKeyFunc"><see cref="Func{BinaryReader, TKey}"/> used to read each key element.</param>
        /// <param name="readValueFunc"><see cref="Func{Reader, TValue}"/> used to read each value element.</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/>.</returns>
        public static Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BinaryReader reader,
            Func<BinaryReader, TKey> readKeyFunc,
            Func<BinaryReader, TValue> readValueFunc)
            => ReadDictionary(reader, () => readKeyFunc(reader), () => readValueFunc(reader));
    }
}
