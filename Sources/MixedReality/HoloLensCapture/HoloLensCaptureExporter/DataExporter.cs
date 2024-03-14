// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureExporter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;
    using OpenXRHand = Microsoft.Psi.MixedReality.OpenXR.Hand;
    using StereoKitHand = Microsoft.Psi.MixedReality.StereoKit.Hand;
    using WinRTEyes = Microsoft.Psi.MixedReality.WinRT.Eyes;

    /// <summary>
    /// Implements the data exporter.
    /// </summary>
    internal class DataExporter
    {
        /// <summary>
        /// The video stream name.
        /// </summary>
        private const string VideoImageCameraViewStreamName = "VideoImageCameraView";

        /// <summary>
        /// The encoded video stream name.
        /// </summary>
        private const string VideoEncodedImageCameraViewStreamName = "VideoEncodedImageCameraView";

        /// <summary>
        /// The audio stream name.
        /// </summary>
        private const string AudioStreamName = "Audio";

        /// <summary>
        /// Exports data based on the specified export command.
        /// </summary>
        /// <param name="exportCommand">The export command.</param>
        /// <returns>An error code or 0 if success.</returns>
        public static int Run(Verbs.ExportCommand exportCommand)
        {
            // Register platform-specific resources
            Microsoft.Psi.Imaging.Resources.RegisterPlatformResources();
            Microsoft.Psi.Media.Resources.RegisterPlatformResources();
            Microsoft.Psi.Audio.Resources.RegisterPlatformResources();

            // Create a pipeline
            using var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.Throttle);

            // Open the psi store for reading
            var store = PsiStore.Open(p, exportCommand.StoreName, exportCommand.StorePath);

            // Get references to the various streams. If a stream is not present in the store,
            // the reference will be null.
            var accelerometer = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Accelerometer");
            var gyroscope = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Gyroscope");
            var magnetometer = store.OpenStreamOrDefault<(Vector3D, DateTime)[]>("Magnetometer");
            var head = store.OpenStreamOrDefault<CoordinateSystem>("Head");
            var audio = store.OpenStreamOrDefault<AudioBuffer>(AudioStreamName);
            var videoEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>(VideoEncodedImageCameraViewStreamName);
            var videoImageCameraView = store.OpenStreamOrDefault<ImageCameraView>(VideoImageCameraViewStreamName);
            var previewEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("PreviewEncodedImageCameraView");
            var previewImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("PreviewImageCameraView");
            var depthImageCameraView = store.OpenStreamOrDefault<DepthImageCameraView>("DepthImageCameraView");
            var depthCalibrationMap = store.OpenStreamOrDefault<CalibrationPointsMap>("DepthCalibrationMap");
            var infraredEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("DepthInfraredEncodedImageCameraView");
            var infraredImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("DepthInfraredImageCameraView");
            var ahatInfraredEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>("AhatDepthInfraredEncodedImageCameraView");
            var ahatInfraredImageCameraView = store.OpenStreamOrDefault<ImageCameraView>("AhatDepthInfraredImageCameraView");
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
            static void VerifyMutualExclusivity(dynamic a, dynamic b, string name)
            {
                if (a != null && b != null)
                {
                    throw new Exception($"Found both encoded and unencoded {name} streams (expected one or the other).");
                }
            }

            VerifyMutualExclusivity(videoEncodedImageCameraView, videoImageCameraView, "video");
            VerifyMutualExclusivity(previewEncodedImageCameraView, previewImageCameraView, "preview");
            VerifyMutualExclusivity(infraredEncodedImageCameraView, infraredImageCameraView, "infrared");
            VerifyMutualExclusivity(ahatInfraredEncodedImageCameraView, ahatInfraredImageCameraView, "ahat-infrared");
            VerifyMutualExclusivity(leftFrontEncodedImageCameraView, leftFrontImageCameraView, "left-front");
            VerifyMutualExclusivity(rightFrontEncodedImageCameraView, rightFrontImageCameraView, "right-front");
            VerifyMutualExclusivity(leftLeftEncodedImageCameraView, leftLeftImageCameraView, "left-left");
            VerifyMutualExclusivity(rightRightEncodedImageCameraView, rightRightImageCameraView, "right-right");

            // Construct a list of stream writers to export data with (these will be closed once
            // the export pipeline is completed)
            var streamWritersToClose = new List<StreamWriter>();

            var imageCameraView = HoloLensCaptureInterop.Operators.Export("Video", videoImageCameraView, videoEncodedImageCameraView, null, isNV12: true, exportCommand.OutputPath, streamWritersToClose);
            HoloLensCaptureInterop.Operators.Export("Preview", previewImageCameraView, previewEncodedImageCameraView, null, isNV12: true, exportCommand.OutputPath, streamWritersToClose);
            HoloLensCaptureInterop.Operators.Export("Infrared", infraredImageCameraView, infraredEncodedImageCameraView, null, isNV12: false, exportCommand.OutputPath, streamWritersToClose);
            HoloLensCaptureInterop.Operators.Export("AhatInfrared", ahatInfraredImageCameraView, ahatInfraredEncodedImageCameraView, null, isNV12: false, exportCommand.OutputPath, streamWritersToClose);
            HoloLensCaptureInterop.Operators.Export("LeftFront", leftFrontImageCameraView, leftFrontEncodedImageCameraView, leftFrontGzipImageCameraView, isNV12: false, exportCommand.OutputPath, streamWritersToClose);
            HoloLensCaptureInterop.Operators.Export("RightFront", rightFrontImageCameraView, rightFrontEncodedImageCameraView, rightFrontGzipImageCameraView, isNV12: false, exportCommand.OutputPath, streamWritersToClose);
            HoloLensCaptureInterop.Operators.Export("LeftLeft", leftLeftImageCameraView, leftLeftEncodedImageCameraView, leftLeftGzipImageCameraView, isNV12: false, exportCommand.OutputPath, streamWritersToClose);
            HoloLensCaptureInterop.Operators.Export("RightRight", rightRightImageCameraView, rightRightEncodedImageCameraView, rightRightGzipImageCameraView, isNV12: false, exportCommand.OutputPath, streamWritersToClose);

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
            accelerometer?.SelectManyImuSamples().Export("IMU", "Accelerometer", exportCommand.OutputPath, streamWritersToClose);
            gyroscope?.SelectManyImuSamples().Export("IMU", "Gyroscope", exportCommand.OutputPath, streamWritersToClose);
            magnetometer?.SelectManyImuSamples().Export("IMU", "Magnetometer", exportCommand.OutputPath, streamWritersToClose);

            // Export head, eyes and hands streams
            head?.Export("Head", exportCommand.OutputPath, streamWritersToClose);

            if (store.Contains("Eyes"))
            {
                var simplifiedEyesTypeName = SimplifyTypeName(store.GetMetadata("Eyes").TypeName);
                if (simplifiedEyesTypeName == SimplifyTypeName(typeof(Ray3D).FullName))
                {
                    var eyes = store.OpenStreamOrDefault<Ray3D>("Eyes");
                    eyes?.Export("Eyes", exportCommand.OutputPath, streamWritersToClose);
                }
                else if (simplifiedEyesTypeName == SimplifyTypeName(typeof(WinRTEyes).FullName) ||
                    simplifiedEyesTypeName == "Microsoft.Psi.MixedReality.EyesRT")
                {
                    var eyes = store.OpenStreamOrDefault<WinRTEyes>("Eyes");
                    eyes?.Export("Eyes", exportCommand.OutputPath, streamWritersToClose);
                }
            }

            if (store.Contains("Hands"))
            {
                var simplifiedHandsTypeName = SimplifyTypeName(store.GetMetadata("Hands").TypeName);
                if (simplifiedHandsTypeName == SimplifyTypeName(typeof((StereoKitHand, StereoKitHand)).FullName) ||
                    simplifiedHandsTypeName == "System.ValueTuple`2[[Microsoft.Psi.MixedReality.Hand][Microsoft.Psi.MixedReality.Hand]]")
                {
                    var hands = store.OpenStream<(StereoKitHand Left, StereoKitHand Right)>("Hands");
                    hands.Select(x => x.Left).Export("Hands", "Left", exportCommand.OutputPath, streamWritersToClose);
                    hands.Select(x => x.Right).Export("Hands", "Right", exportCommand.OutputPath, streamWritersToClose);
                }
                else if (simplifiedHandsTypeName == SimplifyTypeName(typeof((OpenXRHand, OpenXRHand)).FullName) ||
                    simplifiedHandsTypeName == "System.ValueTuple`2[[Microsoft.Psi.MixedReality.HandXR][Microsoft.Psi.MixedReality.HandXR]]")
                {
                    var hands = store.OpenStream<(OpenXRHand Left, OpenXRHand Right)>("Hands");
                    hands.Select(x => x.Left).Export("Hands", "Left", exportCommand.OutputPath, streamWritersToClose);
                    hands.Select(x => x.Right).Export("Hands", "Right", exportCommand.OutputPath, streamWritersToClose);
                }
            }

            // Export audio
            audio?.Export("Audio", exportCommand.OutputPath, streamWritersToClose);

            // Export scene understanding
            sceneUnderstanding?.Export("SceneUnderstanding", exportCommand.OutputPath, streamWritersToClose);

            // Export the video and audio to an Mpeg
            HoloLensCaptureInterop.Operators.ExportToMpeg(
                imageCameraView,
                VideoImageCameraViewStreamName,
                VideoEncodedImageCameraViewStreamName,
                audio,
                AudioStreamName,
                exportCommand.StoreName,
                exportCommand.StorePath,
                Path.Combine(exportCommand.OutputPath, "Video"),
                streamWritersToClose);

            Console.WriteLine($"Exporting {exportCommand.StoreName} to {exportCommand.OutputPath}");
            var startTime = DateTime.Now;
            p.RunAsync(ReplayDescriptor.ReplayAll, progress: new Progress<double>(p => Console.Write($"Progress: {p:P} Time elapsed: {DateTime.Now - startTime}\r")));
            p.WaitAll();

            foreach (var sw in streamWritersToClose)
            {
                sw.Close();
            }

            Console.WriteLine();
            Console.WriteLine($"Done in {DateTime.Now - startTime}.");

            return 0;
        }

        /// <summary>
        /// Simplify the full type name into just the basic underlying type names,
        /// stripping away details like assembly, version, culture, token, etc.
        /// For example, for the type (Vector3D, DateTime)[]
        /// Input: "System.ValueTuple`2
        ///     [[MathNet.Spatial.Euclidean.Vector3D, MathNet.Spatial, Version=0.6.0.0, Culture=neutral, PublicKeyToken=000000000000],
        ///     [System.DateTime, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=000000000000]]
        ///     [], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=000000000000"
        /// Output: "System.ValueTuple`2[[MathNet.Spatial.Euclidean.Vector3D],[System.DateTime]][]".
        /// </summary>
        private static string SimplifyTypeName(string typeName)
        {
            static string SubstringToComma(string s)
            {
                var commaIndex = s.IndexOf(',');
                if (commaIndex >= 0)
                {
                    return s.Substring(0, commaIndex);
                }
                else
                {
                    return s;
                }
            }

            // Split first on open bracket, then on closed bracket
            var allSplits = new List<string[]>();
            foreach (var openSplit in typeName.Split('['))
            {
                allSplits.Add(openSplit.Split(']'));
            }

            // Re-assemble into a simplified string (without assembly, version, culture, token, etc).
            var assembledString = string.Empty;
            for (int i = 0; i < allSplits.Count; i++)
            {
                // Add back an open bracket (except the first time)
                if (i != 0)
                {
                    assembledString += "[";
                }

                for (int j = 0; j < allSplits[i].Length; j++)
                {
                    // Remove everything after the comma (assembly, version, culture, token, etc).
                    assembledString += SubstringToComma(allSplits[i][j]);

                    // Add back a closed bracket (except the last time)
                    if (j != allSplits[i].Length - 1)
                    {
                        assembledString += "]";
                    }
                }
            }

            return assembledString;
        }
    }
}
