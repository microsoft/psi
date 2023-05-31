// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Numerics;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Calibration;
    using Windows.Perception.Spatial;
    using Windows.Storage;
    using Quaternion = System.Numerics.Quaternion;
    using VectorDouble = MathNet.Numerics.LinearAlgebra.Vector<double>;

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
            return worldPose.HasValue ? worldPose.Value.RebaseToMathNetCoordinateSystem() : null;
        }

        /// <summary>
        /// Converts a <see cref="CoordinateSystem"/> in \psi basis to a <see cref="SpatialCoordinateSystem"/> in HoloLens basis.
        /// </summary>
        /// <param name="coordinateSystem">The <see cref="CoordinateSystem"/> in \psi basis.</param>
        /// <returns>The <see cref="SpatialCoordinateSystem"/>.</returns>
        public static SpatialCoordinateSystem TryConvertPsiCoordinateSystemToSpatialCoordinateSystem(this CoordinateSystem coordinateSystem)
        {
            var holoLensMatrix = coordinateSystem.RebaseToHoloLensSystemMatrix();
            var translation = holoLensMatrix.Translation;
            holoLensMatrix.Translation = Vector3.Zero;
            var rotation = Quaternion.CreateFromRotationMatrix(holoLensMatrix);
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(MixedReality.WorldSpatialCoordinateSystem, translation, rotation);
            return spatialAnchor?.CoordinateSystem;
        }

        /// <summary>
        /// Computes camera intrinsics from a map of calibration points.
        /// </summary>
        /// <param name="calibrationPointsMap">The map of calibration points to compute camera intrinsics from.</param>
        /// <returns>The computed <see cref="CameraIntrinsics"/>.</returns>
        internal static CameraIntrinsics ComputeCameraIntrinsics(this CalibrationPointsMap calibrationPointsMap)
        {
            var width = calibrationPointsMap.Width;
            var height = calibrationPointsMap.Height;

            // Convert unit plane points in the calibration map to a lookup table mapping image points to 3D points in camera space.
            List<Point3D> cameraPoints = new ();
            List<Point2D> imagePoints = new ();
            int ci = 0;
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    var x = calibrationPointsMap.CameraUnitPlanePoints[ci++];
                    var y = calibrationPointsMap.CameraUnitPlanePoints[ci++];

                    if (!double.IsNaN(x) && !double.IsNaN(y))
                    {
                        var norm = Math.Sqrt((x * x) + (y * y) + 1.0);
                        imagePoints.Add(new Point2D(i + 0.5, j + 0.5));
                        cameraPoints.Add(new Point3D(x / norm, y / norm, 1.0 / norm));
                    }
                }
            }

            // Initialize a starting camera matrix
            var initialCameraMatrix = Matrix<double>.Build.Dense(3, 3);
            var initialDistortion = VectorDouble.Build.Dense(2);
            initialCameraMatrix[0, 0] = 250; // fx
            initialCameraMatrix[1, 1] = 250; // fy
            initialCameraMatrix[0, 2] = width / 2.0; // cx
            initialCameraMatrix[1, 2] = height / 2.0; // cy
            initialCameraMatrix[2, 2] = 1;
            CalibrationExtensions.CalibrateCameraIntrinsics(
                cameraPoints,
                imagePoints,
                initialCameraMatrix,
                initialDistortion,
                out var computedCameraMatrix,
                out var computedDistortionCoefficients,
                false);

            return new CameraIntrinsics(width, height, computedCameraMatrix, computedDistortionCoefficients);
        }

        /// <summary>
        /// Serialize camera intrinsics to a file on the device.
        /// </summary>
        /// <param name="cameraIntrinsics">The camera intrinsics to serialize.</param>
        /// <param name="writeFile">The device file to serialize to.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        internal static async Task<bool> SerializeAsync(this CameraIntrinsics cameraIntrinsics, StorageFile writeFile)
        {
            SerializedCameraIntrinsics serializedCameraIntrinsics = new ()
            {
                ImageWidth = cameraIntrinsics.ImageWidth,
                ImageHeight = cameraIntrinsics.ImageHeight,
                Transform = cameraIntrinsics.Transform.AsColumnMajorArray(),
                RadialDistortion = cameraIntrinsics.RadialDistortion.ToArray(),
                TangentialDistortion = cameraIntrinsics.TangentialDistortion.ToArray(),
                ClosedFormDistorts = cameraIntrinsics.ClosedFormDistorts,
            };

            using var stream = await writeFile.OpenStreamForWriteAsync();
            var serializer = new XmlSerializer(typeof(SerializedCameraIntrinsics));
            serializer.Serialize(stream, serializedCameraIntrinsics);

            return true;
        }

        /// <summary>
        /// Serialize calibration points map to a file on the device.
        /// </summary>
        /// <param name="calibrationPointsMap">The calibration points map to serialize.</param>
        /// <param name="writeFile">The device file to serialize to.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        internal static async Task<bool> SerializeAsync(this CalibrationPointsMap calibrationPointsMap, StorageFile writeFile)
        {
            using var stream = await writeFile.OpenStreamForWriteAsync();
            using var writer = new BinaryWriter(stream);
            writer.Write(calibrationPointsMap.Width);
            writer.Write(calibrationPointsMap.Height);
            writer.Write(calibrationPointsMap.CameraUnitPlanePoints.Length);
            foreach (var value in calibrationPointsMap.CameraUnitPlanePoints)
            {
                writer.Write(value);
            }

            return true;
        }

        /// <summary>
        /// Reads camera intrinsics from a file on the device.
        /// </summary>
        /// <param name="file">The file with serialized camera intrinsics.</param>
        /// <returns>A <see cref="Task{CameraIntrinsics}"/> result with the deserialized camera intrinsics.</returns>
        internal static async Task<CameraIntrinsics> DeserializeCameraIntrinsicsAsync(this StorageFile file)
        {
            var serializer = new XmlSerializer(typeof(SerializedCameraIntrinsics));
            using var stream = await file.OpenStreamForReadAsync();
            var data = serializer.Deserialize(stream) as SerializedCameraIntrinsics;

            // Parse out and return
            var transform = Matrix<double>.Build.DenseOfColumnMajor(3, 3, data.Transform);
            VectorDouble radialDistortion = null;
            if (data.RadialDistortion is not null)
            {
                radialDistortion = VectorDouble.Build.DenseOfArray(data.RadialDistortion);
            }

            VectorDouble tangentialDistortion = null;
            if (data.TangentialDistortion is not null)
            {
                tangentialDistortion = VectorDouble.Build.DenseOfArray(data.TangentialDistortion);
            }

            return new (
                data.ImageWidth,
                data.ImageHeight,
                transform,
                radialDistortion,
                tangentialDistortion,
                data.ClosedFormDistorts);
        }

        /// <summary>
        /// Reads calibration points map from a file on the device.
        /// </summary>
        /// <param name="file">The file with serialized calibration points map.</param>
        /// <returns>A <see cref="Task{CalibrationPointsMap}"/> result with the deserialized calibration points map.</returns>
        internal static async Task<CalibrationPointsMap> DeserializeCalibrationPointsMapAsync(this StorageFile file)
        {
            using var stream = await file.OpenStreamForReadAsync();
            using var reader = new BinaryReader(stream);
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var len = reader.ReadInt32();
            double[] cameraUnitPlanePoints = new double[len];
            for (var i = 0; i < len; i++)
            {
                cameraUnitPlanePoints[i] = reader.ReadDouble();
            }

            return new ()
            {
                Width = width,
                Height = height,
                CameraUnitPlanePoints = cameraUnitPlanePoints,
            };
        }
    }
}
