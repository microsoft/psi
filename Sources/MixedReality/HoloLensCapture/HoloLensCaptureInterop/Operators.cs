// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureInterop
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;
    using OpenXRHand = Microsoft.Psi.MixedReality.OpenXR.Hand;
    using OpenXRHandsSensor = Microsoft.Psi.MixedReality.OpenXR.HandsSensor;
    using StereoKitHand = Microsoft.Psi.MixedReality.StereoKit.Hand;
    using StereoKitHandsSensor = Microsoft.Psi.MixedReality.StereoKit.HandsSensor;
    using WinRTEyes = Microsoft.Psi.MixedReality.WinRT.Eyes;

    /// <summary>
    /// Stream operators and extension methods for exporting data.
    /// </summary>
    public static class Operators
    {
        private static readonly string NaNString = double.NaN.ToText();
        private static readonly IImageToStreamEncoder PngEncoder = PlatformResources.Get<IImageToStreamEncoder>("ImageToPngStreamEncoder");
        private static readonly IImageFromStreamDecoder Decoder = PlatformResources.GetDefault<IImageFromStreamDecoder>();

        /// <summary>
        /// Opens the specified stream for reading and (or returns null if nonexistent).
        /// </summary>
        /// <typeparam name="T">The expected type of the stream to open.</typeparam>
        /// <param name="store">Store containing stream.</param>
        /// <param name="name">The name of the stream to open.</param>
        /// <returns>Stream instance that can be used to consume the messages (or null if nonexistent).</returns>
        public static IProducer<T> OpenStreamOrDefault<T>(this PsiImporter store, string name)
            => store.Contains(name) ? store.OpenStream<T>(name) : null;

        /// <summary>
        /// Convert <see cref="double"/> to string.
        /// </summary>
        /// <param name="d"><see cref="double"/> to be converted.</param>
        /// <returns>Text representation.</returns>
        public static string ToText(this double d)
        {
            var text = d.ToString("G17", CultureInfo.InvariantCulture); // https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings#RFormatString
            if (!double.IsNaN(d) && double.Parse(text) != d)
            {
                throw new Exception("Text representation of double did not survive round-trip parsing.");
            }

            return text;
        }

        /// <summary>
        /// Convert <see cref="int"/> to string.
        /// </summary>
        /// <param name="i"><see cref="int"/> to be converted.</param>
        /// <returns>Text representation.</returns>
        public static string ToText(this int i)
        {
            return i.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert <see cref="DateTime"/> to string.
        /// </summary>
        /// <param name="dt"><see cref="DateTime"/> to be converted.</param>
        /// <returns>Text representation.</returns>
        public static string ToText(this DateTime dt)
        {
            return dt.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert <see cref="bool"/> to string.
        /// </summary>
        /// <param name="b"><see cref="bool"/> to be converted.</param>
        /// <returns>Text representation.</returns>
        public static string ToText(this bool b)
        {
            return b ? "1" : "0";
        }

        /// <summary>
        /// Convert <see cref="CoordinateSystem"/> to tab-delimited text representation.
        /// </summary>
        /// <param name="c"><see cref="CoordinateSystem"/> to be converted.</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this CoordinateSystem c)
            => c is not null
               ? $"{c[0, 0].ToText()}\t{c[0, 1].ToText()}\t{c[0, 2].ToText()}\t{c[0, 3].ToText()}\t" +
                 $"{c[1, 0].ToText()}\t{c[1, 1].ToText()}\t{c[1, 2].ToText()}\t{c[1, 3].ToText()}\t" +
                 $"{c[2, 0].ToText()}\t{c[2, 1].ToText()}\t{c[2, 2].ToText()}\t{c[2, 3].ToText()}\t" +
                 $"{c[3, 0].ToText()}\t{c[3, 1].ToText()}\t{c[3, 2].ToText()}\t{c[3, 3].ToText()}"
               : $"{NaNString}\t{NaNString}\t{NaNString}\t{NaNString}\t" +
                 $"{NaNString}\t{NaNString}\t{NaNString}\t{NaNString}\t" +
                 $"{NaNString}\t{NaNString}\t{NaNString}\t{NaNString}\t" +
                 $"{NaNString}\t{NaNString}\t{NaNString}\t{NaNString}";

        /// <summary>
        /// Converts a camera intrinsics to a tab-delimited text representation.
        /// </summary>
        /// <param name="cameraIntrinsics">The camera intrinsics.</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this ICameraIntrinsics cameraIntrinsics)
            => $"{cameraIntrinsics.Transform.ToText()}\t" +
               $"{cameraIntrinsics.RadialDistortion.ToText()}\t" +
               $"{cameraIntrinsics.TangentialDistortion.ToText()}\t" +
               $"{cameraIntrinsics.FocalLength.ToText()}\t" +
               $"{cameraIntrinsics.FocalLengthXY.ToText()}\t" +
               $"{cameraIntrinsics.PrincipalPoint.ToText()}\t" +
               $"{cameraIntrinsics.ClosedFormDistorts.ToText()}\t" +
               $"{cameraIntrinsics.ImageWidth.ToText()}\t" +
               $"{cameraIntrinsics.ImageHeight.ToText()}";

        /// <summary>
        /// Converts a matrix to a tab-delimited text representation.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this Matrix<double> matrix)
        {
            var result = new StringBuilder();
            for (int i = 0; i < matrix.RowCount; i++)
            {
                for (int j = 0; j < matrix.ColumnCount; j++)
                {
                    result.Append($"{matrix[i, j].ToText()}\t");
                }
            }

            return result.ToString().TrimEnd('\t');
        }

        /// <summary>
        /// Converts a vector to a tab-delimited text representation.
        /// </summary>
        /// <param name="vector">The vector .</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this Vector<double> vector)
        {
            var result = new StringBuilder();
            for (int i = 0; i < vector.Count; i++)
            {
                result.Append($"{vector[i].ToText()}\t");
            }

            return result.ToString().TrimEnd('\t');
        }

        /// <summary>
        /// Converts a 2D point to a tab-delimited text representation.
        /// </summary>
        /// <param name="point2D">The 2D point.</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this Point2D point2D) =>
            $"{point2D.X.ToText()}\t{point2D.Y.ToText()}";

        /// <summary>
        /// Converts a 3D point to a tab-delimited text representation.
        /// </summary>
        /// <param name="point3D">The 3D point.</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this Point3D point3D) =>
            $"{point3D.X.ToText()}\t{point3D.Y.ToText()}\t{point3D.Z.ToText()}";

        /// <summary>
        /// Converts a 3D vector to a tab-delimited text representation.
        /// </summary>
        /// <param name="vector3D">The 3D vector.</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this Vector3D vector3D) =>
            $"{vector3D.X.ToText()}\t{vector3D.Y.ToText()}\t{vector3D.Z.ToText()}";

        /// <summary>
        /// Converts a 3D ray to a tab-delimited text representation.
        /// </summary>
        /// <param name="ray3D">The 3D ray.</param>
        /// <returns>Tab-delimited text representation.</returns>
        public static string ToText(this Ray3D ray3D) =>
            $"{ray3D.ThroughPoint.ToText()}\t{ray3D.Direction.ToVector3D().ToText()}";

        /// <summary>
        /// Exports a stream of encoded image camera views.
        /// </summary>
        /// <param name="source">The source stream of encoded image camera views.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<EncodedImageCameraView> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var timingFilePath = EnsurePathExists(Path.Combine(outputPath, name, $"Timing.txt"));
            var timingFile = File.CreateText(timingFilePath);
            streamWritersToClose.Add(timingFile);

            var poseFilePath = EnsurePathExists(Path.Combine(outputPath, name, $"Pose.txt"));
            var poseFile = File.CreateText(poseFilePath);
            streamWritersToClose.Add(poseFile);

            var intrinsicsFilePath = EnsurePathExists(Path.Combine(outputPath, name, $"Intrinsics.txt"));
            var intrinsicsFile = File.CreateText(intrinsicsFilePath);
            streamWritersToClose.Add(intrinsicsFile);

            var imageCounter = 0;
            source.Do(
                    (eicv, envelope) =>
                    {
                        var buffer = eicv.ViewedObject.Resource.GetBuffer();
                        var isPng = buffer.Length >= 8 &&
                                    buffer[0] == 0x89 && // look for PNG header
                                    buffer[1] == 0x50 && // see https://en.wikipedia.org/wiki/Portable_Network_Graphics#File_header
                                    buffer[2] == 0x4e && // P
                                    buffer[3] == 0x47 && // N
                                    buffer[4] == 0x0d && // G
                                    buffer[5] == 0x0a &&
                                    buffer[6] == 0x1a &&
                                    buffer[7] == 0x0a;
                        var extension = isPng ? "png" : "jpg";
                        var videoImagesPath = EnsurePathExists(Path.Combine(outputPath, name, $"{imageCounter:000000}.{extension}"));
                        using var videoImageFile = File.Create(videoImagesPath);
                        videoImageFile.Write(buffer, 0, eicv.ViewedObject.Resource.Size);
                        timingFile.WriteLine($"{imageCounter}\t{envelope.OriginatingTime.ToText()}");
                        poseFile.WriteLine($"{envelope.OriginatingTime.ToText()}\t{eicv.CameraPose.ToText()}");
                        intrinsicsFile.WriteLine($"{envelope.OriginatingTime.ToText()}\t{eicv.CameraIntrinsics.ToText()}");
                        imageCounter++;
                    });
        }

        /// <summary>
        /// Exports a stream of depth image camera views.
        /// </summary>
        /// <param name="source">The source stream of depth image camera views.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<DepthImageCameraView> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var timingFilePath = EnsurePathExists(Path.Combine(outputPath, name, $"Timing.txt"));
            var timingFile = File.CreateText(timingFilePath);
            streamWritersToClose.Add(timingFile);

            var poseFilePath = EnsurePathExists(Path.Combine(outputPath, name, $"Pose.txt"));
            var poseFile = File.CreateText(poseFilePath);
            streamWritersToClose.Add(poseFile);

            var intrinsicsFilePath = EnsurePathExists(Path.Combine(outputPath, name, $"Intrinsics.txt"));
            var intrinsicsFile = File.CreateText(intrinsicsFilePath);
            streamWritersToClose.Add(intrinsicsFile);

            var depthImageCounter = 0;
            source
                .Encode(PlatformResources.GetDefault<IDepthImageToStreamEncoder>())
                .Do(
                    (edicv, envelope) =>
                    {
                        var buffer = edicv.ViewedObject.Resource.GetBuffer();
                        var depthImagesPath = EnsurePathExists(Path.Combine(outputPath, name, $"{depthImageCounter:000000}.png"));
                        using var depthImageFile = File.Create(depthImagesPath);
                        depthImageFile.Write(buffer, 0, buffer.Length);
                        timingFile.WriteLine($"{depthImageCounter}\t{envelope.OriginatingTime.ToText()}");
                        poseFile.WriteLine($"{envelope.OriginatingTime.ToText()}\t{edicv.CameraPose.ToText()}");
                        intrinsicsFile.WriteLine($"{envelope.OriginatingTime.ToText()}\t{edicv.CameraIntrinsics.ToText()}");
                        depthImageCounter++;
                    });
        }

        /// <summary>
        /// Exports a stream of IMU readings.
        /// </summary>
        /// <param name="source">The source stream of IMU readings.</param>
        /// <param name="directory">The directory in which to persist.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<Vector3D> source, string directory, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, directory, $"{name}.txt"));
            var file = File.CreateText(filePath);
            streamWritersToClose.Add(file);
            source
                .Do(
                    (vector, envelope) =>
                    {
                        file.WriteLine($"{envelope.OriginatingTime.ToText()}\t{vector.ToText()}");
                    });
        }

        /// <summary>
        /// Exports a stream of poses.
        /// </summary>
        /// <param name="source">The source stream of poses.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<CoordinateSystem> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, name, $"{name}.txt"));
            var file = File.CreateText(filePath);
            streamWritersToClose.Add(file);
            source
                .Do(
                    (coordinateSystem, envelope) =>
                    {
                        file.WriteLine($"{envelope.OriginatingTime.ToText()}\t{coordinateSystem.ToText()}");
                    });
        }

        /// <summary>
        /// Exports a stream of strings.
        /// </summary>
        /// <param name="source">The source stream of string.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<string> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, $"{name}.txt"));
            var file = File.CreateText(filePath);
            file.AutoFlush = true;
            streamWritersToClose.Add(file);
            source.Do(
                    (s, envelope) =>
                    {
                        file.WriteLine($"{s}");
                    });
            file.Flush();
        }

        /// <summary>
        /// Exports a stream of 3D rays.
        /// </summary>
        /// <param name="source">The source stream of 3D rays.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<Ray3D> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, name, $"{name}.txt"));
            var file = File.CreateText(filePath);
            streamWritersToClose.Add(file);
            source
                .Do(
                    (ray3D, envelope) =>
                    {
                        file.WriteLine($"{envelope.OriginatingTime.ToText()}\t{ray3D.ToText()}");
                    });
        }

        /// <summary>
        /// Exports a stream of EyeRT.
        /// </summary>
        /// <param name="source">The source stream of EyeRT.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<WinRTEyes> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, name, $"{name}.txt"));
            var file = File.CreateText(filePath);
            streamWritersToClose.Add(file);
            source
                .Do(
                    (eyes, envelope) =>
                    {
                        file.Write($"{envelope.OriginatingTime.ToText()}\t");

                        if (eyes?.GazeRay is null)
                        {
                            // write 6 NaNs to represent the null GazeRay (Point3D, Vector3D)
                            for (int i = 0; i < 6; i++)
                            {
                                file.Write($"{NaNString}\t");
                            }
                        }
                        else
                        {
                            file.Write($"{eyes.GazeRay.Value.ToText()}\t");
                        }

                        if (eyes is null)
                        {
                            // write false for CalibrationValid
                            file.WriteLine(false.ToText());
                        }
                        else
                        {
                            file.WriteLine($"{eyes.CalibrationValid.ToText()}");
                        }
                    });
        }

        /// <summary>
        /// Exports a stream of hand infomation (from <see cref="StereoKitHandsSensor"/>).
        /// </summary>
        /// <param name="source">The source stream of hand information.</param>
        /// <param name="directory">The directory in which to persist.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<StereoKitHand> source, string directory, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, directory, $"{name}.txt"));
            var file = File.CreateText(filePath);
            streamWritersToClose.Add(file);
            source
                .Do(
                    (hand, envelope) =>
                    {
                        // ensures that we export null Hand instances with NaNs
                        hand ??= StereoKitHand.Empty;

                        var result = new StringBuilder();
                        result.Append($"{envelope.OriginatingTime.ToText()}\t");
                        result.Append($"{hand.IsGripped.ToText()}\t");
                        result.Append($"{hand.IsPinched.ToText()}\t");
                        result.Append($"{hand.IsTracked.ToText()}\t");

                        // hand.Joints is never null, but may contain null values
                        foreach (var joint in hand.Joints)
                        {
                            result.Append($"{joint.ToText()}\t");
                        }

                        file.WriteLine(result.ToString().TrimEnd('\t'));
                    });
        }

        /// <summary>
        /// Exports a stream of hand information (from <see cref="OpenXRHandsSensor"/>).
        /// </summary>
        /// <param name="source">The source stream of hand information.</param>
        /// <param name="directory">The directory in which to persist.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<OpenXRHand> source, string directory, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, directory, $"{name}.txt"));
            var file = File.CreateText(filePath);
            streamWritersToClose.Add(file);
            source
                .Do(
                    (hand, envelope) =>
                    {
                        // ensures that we export null HandXR instances with NaNs
                        hand ??= OpenXRHand.Empty;

                        var result = new StringBuilder();
                        result.Append($"{envelope.OriginatingTime.ToText()}\t");
                        result.Append($"{hand.IsActive.ToText()}\t");

                        // hand.Joints is never null, but may contain null values
                        foreach (var joint in hand.Joints)
                        {
                            result.Append($"{joint.ToText()}\t");
                        }

                        foreach (var jointValid in hand.JointsValid)
                        {
                            result.Append($"{jointValid.ToText()}\t");
                        }

                        foreach (var jointTracked in hand.JointsTracked)
                        {
                            result.Append($"{jointTracked.ToText()}\t");
                        }

                        file.WriteLine(result.ToString().TrimEnd('\t'));
                    });
        }

        /// <summary>
        /// Exports a stream of audio.
        /// </summary>
        /// <param name="source">The source stream of audio.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<AudioBuffer> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            // export to .wav file
            source.PipeTo(
                new WaveFileWriter(
                    source.Out.Pipeline,
                    EnsurePathExists(Path.Combine(outputPath, name, $"{name}.wav"))));

            // export individual raw audio buffers to `Audio000123.bin` files along with timing information
            var buffersPath = Path.Combine(outputPath, name, "Buffers");
            var timingFilePath = EnsurePathExists(Path.Combine(buffersPath, $"Timing.txt"));
            var timingFile = File.CreateText(timingFilePath);
            streamWritersToClose.Add(timingFile);
            var bufferCounter = 0;
            source
                .Do(
                    (buffer, envelope) =>
                    {
                        if (buffer.HasValidData)
                        {
                            var data = buffer.Data;
                            var file = File.Create(EnsurePathExists(Path.Combine(buffersPath, $"Audio{bufferCounter:000000}.bin")));
                            file.Write(data, 0, data.Length);
                            file.Close();
                            file.Dispose();
                            timingFile.WriteLine($"{bufferCounter++}\t{envelope.OriginatingTime.ToText()}");
                        }
                    });
        }

        /// <summary>
        /// Exports a stream of calibration maps.
        /// </summary>
        /// <param name="source">The source stream of calibration maps.</param>
        /// <param name="directory">The directory in which to persist.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<CalibrationPointsMap> source, string directory, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            var filePath = EnsurePathExists(Path.Combine(outputPath, directory, $"{name}.txt"));
            var file = File.CreateText(filePath);
            streamWritersToClose.Add(file);
            source
                .Do(
                    (map, envelope) =>
                    {
                        var result = new StringBuilder();
                        result.Append($"{envelope.OriginatingTime.ToText()}\t");
                        result.Append($"{map.Width.ToText()}\t");
                        result.Append($"{map.Height.ToText()}\t");
                        foreach (var point in map.CameraUnitPlanePoints)
                        {
                            result.Append($"{point.ToText()}\t");
                        }

                        file.WriteLine(result.ToString().TrimEnd('\t'));
                    });
        }

        /// <summary>
        /// Exports a stream of scene objects.
        /// </summary>
        /// <param name="source">The source stream of scene objects.</param>
        /// <param name="name">The name for the source stream.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void Export(this IProducer<SceneObjectCollection> source, string name, string outputPath, List<StreamWriter> streamWritersToClose)
        {
            void ExportScene(IProducer<SceneObjectCollection.SceneObject> sceneObject, string sceneName)
            {
                void BuildRectangle(Rectangle3D? rect, StringBuilder sb)
                {
                    void BuildPoint(Point3D point, StringBuilder sb)
                    {
                        sb.Append($"{point.ToText()}\t");
                    }

                    if (rect.HasValue)
                    {
                        var r = rect.Value;
                        BuildPoint(r.TopLeft, sb);
                        BuildPoint(r.TopRight, sb);
                        BuildPoint(r.BottomLeft, sb);
                        BuildPoint(r.BottomRight, sb);
                    }
                    else
                    {
                        for (var i = 0; i < 8; i++)
                        {
                            sb.Append($"{NaNString}\t");
                        }
                    }
                }

                var path = Path.Combine(outputPath, name, sceneName);

                void ExportMeshes(List<Mesh3D> meshes, string directory, string name)
                {
                    for (var i = 0; i < meshes.Count; i++)
                    {
                        // .obj file format: https://en.wikipedia.org/wiki/Wavefront_.obj_file
                        var mesh = meshes[i];
                        var meshFile = File.CreateText(EnsurePathExists(Path.Combine(path, directory, name, $"Mesh{i}.obj")));
                        meshFile.Write($"# {directory} {name}");
                        foreach (var v in mesh.Vertices)
                        {
                            meshFile.Write($"\nv {v.X.ToText()} {v.Y.ToText()} {v.Z.ToText()}");
                        }

                        var indices = mesh.TriangleIndices;
                        for (var j = 0; j < indices.Length; j += 3)
                        {
                            meshFile.WriteLine($"\nf {indices[j] + 1} {indices[j + 1] + 1} {indices[j + 2] + 1}");
                        }

                        meshFile.Close();
                    }
                }

                var rectanglesFile = File.CreateText(EnsurePathExists(Path.Combine(path, $"Rectangles.txt")));
                var meshesFile = File.CreateText(EnsurePathExists(Path.Combine(path, $"Meshes.txt")));
                streamWritersToClose.Add(rectanglesFile);
                streamWritersToClose.Add(meshesFile);
                sceneObject
                    .Do(
                        (s, envelope) =>
                        {
                            var originatingTime = $"{envelope.OriginatingTime.ToText()}";
                            var result = new StringBuilder();
                            result.Append(originatingTime).Append('\t');
                            for (var i = 0; i < s.Rectangles.Count; i++)
                            {
                                BuildRectangle(s.Rectangles[i], result);
                                BuildRectangle(s.PlacementRectangles.Count > 0 ? s.PlacementRectangles[i] : null, result);
                            }

                            rectanglesFile.WriteLine(result.ToString().TrimEnd('\t'));
                            meshesFile.WriteLine($"{originatingTime}\t{s.Meshes.Count}\t{s.ColliderMeshes.Count}");
                            ExportMeshes(s.Meshes, nameof(SceneObjectCollection.SceneObject.Meshes), originatingTime);
                            ExportMeshes(s.ColliderMeshes, nameof(SceneObjectCollection.SceneObject.ColliderMeshes), originatingTime);
                        });
            }

            ExportScene(source.Select(s => s.Background), nameof(SceneObjectCollection.Background));
            ExportScene(source.Select(s => s.Ceiling), nameof(SceneObjectCollection.Ceiling));
            ExportScene(source.Select(s => s.Floor), nameof(SceneObjectCollection.Floor));
            ExportScene(source.Select(s => s.Inferred), nameof(SceneObjectCollection.Inferred));
            ExportScene(source.Select(s => s.Platform), nameof(SceneObjectCollection.Platform));
            ExportScene(source.Select(s => s.Unknown), nameof(SceneObjectCollection.Unknown));
            ExportScene(source.Select(s => s.Wall), nameof(SceneObjectCollection.Wall));
            ExportScene(source.Select(s => s.World), nameof(SceneObjectCollection.World));
        }

        /// <summary>
        /// Export image camera views.
        /// </summary>
        /// <param name="name">The name of the view.</param>
        /// <param name="imageCameraView">The image camera view stream.</param>
        /// <param name="encodedImageCameraView">The encoded image camera view stream.</param>
        /// <param name="gzipImageCameraView">The gzip image camera view stream.</param>
        /// <param name="isNV12">Whether the encoding is NV12.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        /// <returns>The image camera view.</returns>
        public static IProducer<ImageCameraView> Export(
            string name,
            IProducer<ImageCameraView> imageCameraView,
            IProducer<EncodedImageCameraView> encodedImageCameraView,
            IProducer<EncodedImageCameraView> gzipImageCameraView,
            bool isNV12,
            string outputPath,
            List<StreamWriter> streamWritersToClose)
        {
            void VerifyMutualExclusivity(dynamic s0, dynamic s1)
            {
                if (s0 != null || s1 != null)
                {
                    throw new Exception($"Expected single stream for each camera (found multiple for {name}).");
                }
            }

            if (imageCameraView != null)
            {
                // export raw camera view as lossless PNG
                VerifyMutualExclusivity(encodedImageCameraView, gzipImageCameraView);
                imageCameraView.Encode(PngEncoder).Export(name, outputPath, streamWritersToClose);
                return imageCameraView;
            }

            if (encodedImageCameraView != null)
            {
                VerifyMutualExclusivity(imageCameraView, gzipImageCameraView);
                var decoded = encodedImageCameraView.Decode(Decoder);
                if (isNV12)
                {
                    // export NV12-encoded camera view as lossless PNG
                    decoded.Encode(PngEncoder).Export(name, outputPath, streamWritersToClose);
                }
                else
                {
                    // export encoded camera view as is
                    encodedImageCameraView.Export(name, outputPath, streamWritersToClose);
                }

                return decoded;
            }

            if (gzipImageCameraView != null)
            {
                // export GZIP'd camera view as lossless PNG
                VerifyMutualExclusivity(imageCameraView, encodedImageCameraView);
                var decoded = gzipImageCameraView.Decode(Decoder);
                decoded.Encode(PngEncoder).Export(name, outputPath, streamWritersToClose);
                return decoded;
            }

            return null;
        }

        /// <summary>
        /// Exports the audio and video to an mpeg file.
        /// </summary>
        /// <param name="imageCameraView">The image camera view stream.</param>
        /// <param name="imageCameraViewStreamName">The image camera view stream name.</param>
        /// <param name="encodedImageCameraViewStreamName">The encoded image camera view stream name.</param>
        /// <param name="audio">The audio stream.</param>
        /// <param name="audioStreamName">The audio stream name.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="storePath">The store path.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="streamWritersToClose">The collection of stream writers to be closed.</param>
        public static void ExportToMpeg(
            IProducer<ImageCameraView> imageCameraView,
            string imageCameraViewStreamName,
            string encodedImageCameraViewStreamName,
            IProducer<AudioBuffer> audio,
            string audioStreamName,
            string storeName,
            string storePath,
            string outputPath,
            List<StreamWriter> streamWritersToClose)
        {
            // Export MPEG video
            (var videoMeta, var width, var height, var audioFormat) = GetAudioAndVideoInfo(storeName, storePath, imageCameraViewStreamName, encodedImageCameraViewStreamName, audioStreamName);

            if (videoMeta is not null)
            {
                // Configure and initialize the mpeg writer
                var frameRateNumerator = (uint)(videoMeta.MessageCount - 1);
                var frameRateDenominator = (uint)((videoMeta.LastMessageOriginatingTime - videoMeta.FirstMessageOriginatingTime).TotalSeconds + 0.5);
                var frameRate = frameRateNumerator / frameRateDenominator;
                var mpegFile = EnsurePathExists(Path.Combine(outputPath, $"Video.mpeg"));
                var audioOutputFormat = WaveFormat.Create16BitPcm((int)(audioFormat?.SamplesPerSec ?? 0), audioFormat?.Channels ?? 0);
                var mpegWriter = PlatformResources.GetDefault<Func<Pipeline, string, uint, uint, PixelFormat, uint, uint, uint, bool, uint, uint, uint, IMpeg4Writer>>().Invoke(
                    imageCameraView.Out.Pipeline,
                    mpegFile,
                    (uint)width,
                    (uint)height,
                    PixelFormat.BGRA_32bpp,
                    frameRateNumerator,
                    frameRateDenominator,
                    10000000,
                    audioFormat != null,
                    audioOutputFormat.BitsPerSample,
                    audioOutputFormat.SamplesPerSec,
                    audioOutputFormat.Channels);

                // We will need to resample the video stream for the mpeg
                var mpegVideoInterval = TimeSpan.FromSeconds(1.0 / frameRate);
                IProducer<bool> mpegVideoTicks;
                IProducer<bool> mpegTicks;

                // Write "start" and "end" times of the mpeg to file
                var mpegTimingFile = File.CreateText(EnsurePathExists(Path.Combine(outputPath, "Video", "VideoMpegTiming.txt")));
                streamWritersToClose.Add(mpegTimingFile);

                // Audio
                if (audioFormat is not null)
                {
                    var audioResampler = PlatformResources.GetDefault<Func<Pipeline, WaveFormat, IAudioResampler>>().Invoke(audio.Out.Pipeline, audioOutputFormat);
                    var mpegAudio = audio.PipeTo(audioResampler, DeliveryPolicy.Unlimited);
                    mpegAudio.PipeTo(mpegWriter.AudioIn);

                    // Compute frame ticks for the resampled video
                    mpegVideoTicks = mpegAudio
                        .Select((m, e) => e.OriginatingTime - m.Duration)
                        .Zip(imageCameraView.TimeOf())
                        .Process<DateTime[], bool>((m, e, emitter) =>
                        {
                            if (emitter.LastEnvelope.OriginatingTime == default)
                            {
                                // The mpeg "start time" will be the minimum time of the first video message and *start* of the first (resampled) audio buffer.
                                emitter.Post(true, m.Min());
                            }

                            while (emitter.LastEnvelope.OriginatingTime <= e.OriginatingTime)
                            {
                                emitter.Post(true, emitter.LastEnvelope.OriginatingTime + mpegVideoInterval);
                            }
                        });

                    // Zip with the resampled audio times
                    mpegTicks = mpegAudio.Select(_ => true).Zip(mpegVideoTicks).Select(a => a.First());
                }
                else
                {
                    // Compute frame ticks for the resampled video
                    mpegVideoTicks = imageCameraView.Process<ImageCameraView, bool>((_, e, emitter) =>
                    {
                        if (emitter.LastEnvelope.OriginatingTime == default)
                        {
                            emitter.Post(true, e.OriginatingTime);
                        }

                        while (emitter.LastEnvelope.OriginatingTime <= e.OriginatingTime)
                        {
                            emitter.Post(true, emitter.LastEnvelope.OriginatingTime + mpegVideoInterval);
                        }
                    });

                    // No audio, so the mpeg start/end times are just the time of the first/last video message.
                    mpegTicks = mpegVideoTicks;
                }

                // Video
                mpegVideoTicks
                    .Join(imageCameraView, Reproducible.Nearest<ImageCameraView>()).Select(tuple => tuple.Item2.ViewedObject)
                    .PipeTo(mpegWriter);

                // Write the mpeg start and end times time
                mpegTicks.First().Do((_, e) => mpegTimingFile.WriteLine($"{e.OriginatingTime.ToText()}"));
                mpegTicks.Last().Do((_, e) => mpegTimingFile.WriteLine($"{e.OriginatingTime.ToText()}"));
            }
        }

        private static (IStreamMetadata VideoMetadata, int Width, int Height, WaveFormat AudioFormat) GetAudioAndVideoInfo(
            string storeName,
            string storePath,
            string imageCameraViewStreamName,
            string encodedImageCameraViewStreamName,
            string audioStreamName)
        {
            // determine properties for the mpeg writer by peeking at the first video and audio messages
            using var p = Pipeline.Create();
            var store = PsiStore.Open(p, storeName, storePath);

            // Get the image width and height by looking at the first message of the video stream.
            var width = 0;
            var height = 0;
            var videoWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            void SetWidthAndHeight(IImage image)
            {
                if (image.PixelFormat != PixelFormat.BGRA_32bpp)
                {
                    throw new ArgumentException($"Expected video stream of {PixelFormat.BGRA_32bpp} (found {image.PixelFormat}).");
                }

                width = image.Width;
                height = image.Height;
                videoWaitHandle.Set();
            }

            bool TryGetMetadata(string stream, out IStreamMetadata meta)
            {
                if (store.Contains(stream))
                {
                    meta = store.GetMetadata(stream);
                    if (meta.MessageCount > 0)
                    {
                        return true;
                    }
                }

                meta = null;
                return false;
            }

            if (!string.IsNullOrEmpty(imageCameraViewStreamName) && TryGetMetadata(imageCameraViewStreamName, out var videoMetadata))
            {
                store.OpenStream<ImageCameraView>(imageCameraViewStreamName).First().Do(v => SetWidthAndHeight(v.ViewedObject.Resource));
            }
            else if (!string.IsNullOrEmpty(encodedImageCameraViewStreamName) && TryGetMetadata(encodedImageCameraViewStreamName, out videoMetadata))
            {
                store.OpenStream<EncodedImageCameraView>(encodedImageCameraViewStreamName).First().Do(v => SetWidthAndHeight(v.ViewedObject.Resource));
            }
            else
            {
                videoMetadata = default;
                videoWaitHandle.Set();
            }

            // Get the audio format by examining the first audio message (if one exists).
            WaveFormat audioFormat = null;
            var audioWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            if (!string.IsNullOrEmpty(audioStreamName) && TryGetMetadata(audioStreamName, out var audioMeta))
            {
                store.OpenStream<AudioBuffer>(audioStreamName).First().Do((a, env) =>
                {
                    audioFormat = a.Format;
                    audioWaitHandle.Set();
                });
            }
            else
            {
                audioFormat = default;
                audioWaitHandle.Set();
            }

            // Run the pipeline, just until we've read the first video and audio message
            p.RunAsync();
            WaitHandle.WaitAll(new WaitHandle[2] { videoWaitHandle, audioWaitHandle });

            return (videoMetadata, width, height, audioFormat);
        }

        /// <summary>
        /// Ensures that a specified path exists.
        /// </summary>
        /// <param name="path">The path to ensure the existence of.</param>
        /// <returns>The path.</returns>
        private static string EnsurePathExists(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return path;
        }
    }
}
