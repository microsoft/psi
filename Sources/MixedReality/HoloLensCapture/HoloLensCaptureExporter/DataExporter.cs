// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureExporter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;

    /// <summary>
    /// Implements the data exporter.
    /// </summary>
    internal class DataExporter
    {
        /// <summary>
        /// Exports data based on the specified export command.
        /// </summary>
        /// <param name="exportCommand">The export command.</param>
        /// <returns>An error code or 0 if success.</returns>
        public static int Run(Verbs.ExportCommand exportCommand)
        {
            // Create a pipeline
            using var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.SynchronousOrThrottle);

            // Open the psi store for reading
            var store = PsiStore.Open(p, exportCommand.StoreName, exportCommand.StorePath);

            // Get references to the various streams. If a stream is not present in the store,
            // the reference will be null.
            var accelerometer = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Accelerometer");
            var gyroscope = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Gyroscope");
            var magnetometer = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Magnetometer");
            var head = store.OpenStreamOrDefault<CoordinateSystem>("Head");
            var eyes = store.OpenStreamOrDefault<Ray3D>("Eyes");
            var hands = store.OpenStreamOrDefault<(Hand Left, Hand Right)>("Hands");
            var audio = store.OpenStreamOrDefault<AudioBuffer>("Audio");
            var videoEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("VideoEncodedImageCameraView");
            var videoImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("VideoImageCameraView");
            var previewEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("PreviewEncodedImageCameraView");
            var previewImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("PreviewImageCameraView");
            var depthImageCameraView = store.OpenStreamOrDefault<DepthImageCameraView>("DepthImageCameraView");
            var depthCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("DepthCalibrationMap");
            var infraredEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("InfraredEncodedImageCameraView");
            var infraredImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("InfraredImageCameraView");
            var ahatDepthImageCameraView = store.OpenStreamOrDefault<DepthImageCameraView>("AhatDepthImageCameraView");
            var ahatDepthCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("AhatDepthCalibrationMap");
            var leftFrontEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftFrontEncodedImageCameraView");
            var leftFrontGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftFrontGzipImageCameraView");
            var leftFrontImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("LeftFrontImageCameraView");
            var leftFrontCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("LeftFrontCalibrationMap");
            var rightFrontEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightFrontEncodedImageCameraView");
            var rightFrontGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightFrontGzipImageCameraView");
            var rightFrontImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("RightFrontImageCameraView");
            var rightFrontCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("RightFrontCalibrationMap");
            var leftLeftEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftLeftEncodedImageCameraView");
            var leftLeftGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("LeftLeftGzipImageCameraView");
            var leftLeftImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("LeftLeftImageCameraView");
            var leftLeftCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("LeftLeftCalibrationMap");
            var rightRightEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightRightEncodedImageCameraView");
            var rightRightGzipImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("RightRightGzipImageCameraView");
            var rightRightImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("RightRightImageCameraView");
            var rightRightCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("RightRightCalibrationMap");
            var sceneUnderstanding = store.OpenStreamOrDefault<SceneObjectCollection>("SceneUnderstanding");

            // Verify expected stream combinations
            void VerifyMutualExclusivity(dynamic a, dynamic b, string name)
            {
                if (a != null && b != null)
                {
                    throw new Exception($"Found both encoded and unencoded {name} streams (expected one or the other).");
                }
            }

            VerifyMutualExclusivity(videoEncodedImageCameraView, videoImageCameraView, "video");
            VerifyMutualExclusivity(previewEncodedImageCameraView, previewImageCameraView, "preview");
            VerifyMutualExclusivity(infraredEncodedImageCameraView, infraredImageCameraView, "infrared");
            VerifyMutualExclusivity(leftFrontEncodedImageCameraView, leftFrontImageCameraView, "left-front");
            VerifyMutualExclusivity(rightFrontEncodedImageCameraView, rightFrontImageCameraView, "right-front");
            VerifyMutualExclusivity(leftLeftEncodedImageCameraView, leftLeftImageCameraView, "left-left");
            VerifyMutualExclusivity(rightRightEncodedImageCameraView, rightRightImageCameraView, "right-right");

            // Construct a list of stream writers to export data with (these will be closed once
            // the export pipeline is completed)
            var streamWritersToClose = new List<StreamWriter>();

            // Export various encoded image camera views
            var pngEncoder = new ImageToPngStreamEncoder();
            var gzipDecoder = new ImageFromStreamDecoder();

            void Export(
                string name,
                IProducer<ImageCameraView> imageCameraView,
                IProducer<EncodedImageCameraView> encodedImageCameraView,
                IProducer<EncodedImageCameraView> gzipImageCameraView = null)
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
                    imageCameraView?.Encode(pngEncoder).Export(name, exportCommand.OutputPath, streamWritersToClose);
                }

                if (encodedImageCameraView != null)
                {
                    // export encoded camera view as is
                    VerifyMutualExclusivity(imageCameraView, gzipImageCameraView);
                    encodedImageCameraView.Export(name, exportCommand.OutputPath, streamWritersToClose);
                }

                if (gzipImageCameraView != null)
                {
                    // export GZIP'd camera view as lossless PNG
                    VerifyMutualExclusivity(imageCameraView, encodedImageCameraView);
                    gzipImageCameraView?.Decode(gzipDecoder)?.Encode(pngEncoder).Export(name, exportCommand.OutputPath, streamWritersToClose);
                }
            }

            Export("Video", videoImageCameraView, videoEncodedImageCameraView);
            Export("Preview", previewImageCameraView, previewEncodedImageCameraView);
            Export("Infrared", infraredImageCameraView, infraredEncodedImageCameraView);
            Export("LeftFront", leftFrontImageCameraView, leftFrontEncodedImageCameraView, leftFrontGzipImageCameraView);
            Export("RightFront", rightFrontImageCameraView, rightFrontEncodedImageCameraView, rightFrontGzipImageCameraView);
            Export("LeftLeft", leftLeftImageCameraView, leftLeftEncodedImageCameraView, leftLeftGzipImageCameraView);
            Export("RightRight", rightRightImageCameraView, rightRightEncodedImageCameraView, rightRightGzipImageCameraView);

            // Export various depth image camera views
            depthImageCameraView?.Export("Depth", exportCommand.OutputPath, streamWritersToClose);
            ahatDepthImageCameraView?.Export("AhatDepth", exportCommand.OutputPath, streamWritersToClose);

            // Export various camera calibration maps
            depthCalibrationMap?.Export("Depth", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            ahatDepthCalibrationMap?.Export("AhatDepth", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            leftFrontCalibrationMap?.Export("LeftFront", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            rightFrontCalibrationMap?.Export("RightFront", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            leftLeftCalibrationMap?.Export("LeftLeft", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);
            rightRightCalibrationMap?.Export("RightRight", "CalibrationMap", exportCommand.OutputPath, streamWritersToClose);

            // Export IMU streams
            accelerometer?.SelectManyImuSamples(DeliveryPolicy.SynchronousOrThrottle).Export("IMU", "Accelerometer", exportCommand.OutputPath, streamWritersToClose);
            gyroscope?.SelectManyImuSamples(DeliveryPolicy.SynchronousOrThrottle).Export("IMU", "Gyroscope", exportCommand.OutputPath, streamWritersToClose);
            magnetometer?.SelectManyImuSamples(DeliveryPolicy.SynchronousOrThrottle).Export("IMU", "Magnetometer", exportCommand.OutputPath, streamWritersToClose);

            // Export head, eyes and hands streams
            head?.Export("Head", exportCommand.OutputPath, streamWritersToClose);
            eyes?.Export("Eyes", exportCommand.OutputPath, streamWritersToClose);
            hands?.Select(x => x.Left).Export("Hands", "Left", exportCommand.OutputPath, streamWritersToClose);
            hands?.Select(x => x.Right).Export("Hands", "Right", exportCommand.OutputPath, streamWritersToClose);

            // Export audio
            audio?.Export("Audio", exportCommand.OutputPath, streamWritersToClose);

            // Export scene understanding
            sceneUnderstanding?.Export("SceneUnderstanding", exportCommand.OutputPath, streamWritersToClose);

            p.PipelineCompleted += (_, _) => Console.WriteLine("DONE.");
            p.RunAsync(ReplayDescriptor.ReplayAllRealTime, progress: new Progress<double>(p => Console.Write($"Progress: {p:P}\r")));
            p.WaitAll();

            foreach (var sw in streamWritersToClose)
            {
                sw.Close();
            }

            Console.WriteLine("Done.");
            return 0;
        }

        /// <summary>
        /// Ensures that a specified path exists.
        /// </summary>
        /// <param name="path">The path to ensure the existence of.</param>
        /// <returns>The path.</returns>
        internal static string EnsurePathExists(string path)
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
