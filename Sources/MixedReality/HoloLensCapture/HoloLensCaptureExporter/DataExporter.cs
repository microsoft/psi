// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureExporter
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
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
            const string videoStreamName = "VideoImageCameraView";
            const string videoEncodedStreamName = "VideoEncodedImageCameraView";
            const string audioStreamName = "Audio";

            var pngEncoder = new ImageToPngStreamEncoder();
            var decoder = new ImageFromStreamDecoder();

            // Create a pipeline
            using var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.Unlimited);

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
            var audio = store.OpenStreamOrDefault<AudioBuffer>(audioStreamName);
            var videoEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>(videoEncodedStreamName);
            var videoImageCameraView = store.OpenStreamOrDefault<ImageCameraView>(videoStreamName);
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
            IProducer<ImageCameraView> Export(
                string name,
                IProducer<ImageCameraView> imageCameraView,
                IProducer<EncodedImageCameraView> encodedImageCameraView,
                IProducer<EncodedImageCameraView> gzipImageCameraView = null,
                bool isNV12 = false)
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
                    imageCameraView.Encode(pngEncoder).Export(name, exportCommand.OutputPath, streamWritersToClose);
                    return imageCameraView;
                }

                if (encodedImageCameraView != null)
                {
                    VerifyMutualExclusivity(imageCameraView, gzipImageCameraView);
                    var decoded = encodedImageCameraView.Decode(decoder);
                    if (isNV12)
                    {
                        // export NV12-encoded camera view as lossless PNG
                        decoded.Encode(pngEncoder).Export(name, exportCommand.OutputPath, streamWritersToClose);
                    }
                    else
                    {
                        // export encoded camera view as is
                        encodedImageCameraView.Export(name, exportCommand.OutputPath, streamWritersToClose);
                    }

                    return decoded;
                }

                if (gzipImageCameraView != null)
                {
                    // export GZIP'd camera view as lossless PNG
                    VerifyMutualExclusivity(imageCameraView, encodedImageCameraView);
                    var decoded = gzipImageCameraView.Decode(decoder);
                    decoded.Encode(pngEncoder).Export(name, exportCommand.OutputPath, streamWritersToClose);
                    return decoded;
                }

                return null;
            }

            var decodedVideo = Export("Video", videoImageCameraView, videoEncodedImageCameraView, isNV12: true);
            Export("Preview", previewImageCameraView, previewEncodedImageCameraView, isNV12: true);
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

            // Export video as MPEG
            if (decodedVideo != null)
            {
                // determine video properties by examining the store up front
                (int Width, int Height, long FrameCount, TimeSpan TimeSpan, WaveFormat audioFormat) GetAudioAndVideoInfo()
                {
                    using var p = Pipeline.Create(deliveryPolicy: DeliveryPolicy.SynchronousOrThrottle);
                    var store = PsiStore.Open(p, exportCommand.StoreName, exportCommand.StorePath);
                    IProducer<ImageCameraView> video = null;
                    IStreamMetadata meta = null;
                    if (store.Contains(videoStreamName))
                    {
                        video = store.OpenStream<ImageCameraView>(videoStreamName);
                        meta = store.GetMetadata(videoStreamName);
                    }
                    else if (store.Contains(videoEncodedStreamName))
                    {
                        video = store.OpenStream<EncodedImageCameraView>(videoEncodedStreamName).Decode(decoder);
                        meta = store.GetMetadata(videoEncodedStreamName);
                    }
                    else
                    {
                        return (0, 0, 0, TimeSpan.Zero, null);
                    }

                    // frame count and time extents can be determined directly from metadata
                    var frameCount = meta.MessageCount;
                    var frameTimeSpan = meta.LastMessageCreationTime - meta.FirstMessageOriginatingTime;

                    // width and height must be determined by reading an actual frame
                    var width = 0;
                    var height = 0;
                    WaveFormat audioFormat = null;
                    var wait = new EventWaitHandle(false, EventResetMode.ManualReset);

                    var audio = store.OpenStreamOrDefault<AudioBuffer>(audioStreamName);
                    audio?.Do(a =>
                    {
                        audioFormat = a.Format;
                        if (width != 0 && height != 0)
                        {
                            wait.Set();
                        }
                    });

                    video.Select(x => x.ViewedObject).Do(v =>
                    {
                        var i = v.Resource;
                        if (i.PixelFormat != PixelFormat.BGRA_32bpp)
                        {
                            throw new ArgumentException($"Expected video stream of {PixelFormat.BGRA_32bpp} (found {i.PixelFormat}).");
                        }

                        width = i.Width;
                        height = i.Height;
                        if (audio == null || audioFormat != null)
                        {
                            wait.Set();
                        }
                    });

                    p.RunAsync();
                    wait.WaitOne(); // wait to see the first frame

                    return (width, height, frameCount, frameTimeSpan, audioFormat);
                }

                (var width, var height, var frameCount, var frameTimeSpan, var audioFormat) = GetAudioAndVideoInfo();

                if (frameCount > 0)
                {
                    var frameRateNumerator = (uint)(frameCount - 1);
                    var frameRateDenominator = (uint)(frameTimeSpan.TotalSeconds + 0.5);
                    var frameRate = frameRateNumerator / frameRateDenominator;
                    var mpegFile = EnsurePathExists(Path.Combine(exportCommand.OutputPath, "Video", $"Video.mpeg"));
                    var audioOutputFormat = WaveFormat.Create16BitPcm((int)(audioFormat?.SamplesPerSec ?? 0), audioFormat?.Channels ?? 0);
                    var mpegWriter = new Mpeg4Writer(p, mpegFile, new Mpeg4WriterConfiguration()
                    {
                        ImageWidth = (uint)width,
                        ImageHeight = (uint)height,
                        FrameRateNumerator = (uint)(frameCount - 1),
                        FrameRateDenominator = (uint)(frameTimeSpan.TotalSeconds + 0.5),
                        PixelFormat = PixelFormat.BGRA_32bpp,
                        TargetBitrate = 10000000,
                        ContainsAudio = audioFormat != null,
                        AudioBitsPerSample = audioOutputFormat.BitsPerSample,
                        AudioChannels = audioOutputFormat.Channels,
                        AudioSamplesPerSecond = audioOutputFormat.SamplesPerSec,
                    });

                    var frameClock = Generators.Repeat(p, 0, TimeSpan.FromSeconds(1.0 / frameRate));
                    var interpolatedVideo = frameClock.Join(decodedVideo, Reproducible.Nearest<ImageCameraView>());
                    interpolatedVideo.Select(x => x.Item2.ViewedObject).PipeTo(mpegWriter);

                    audio
                        ?.Resample(new AudioResamplerConfiguration() { OutputFormat = audioOutputFormat, })
                        ?.PipeTo(mpegWriter.AudioIn);
                }
            }

            // Export scene understanding
            sceneUnderstanding?.Export("SceneUnderstanding", exportCommand.OutputPath, streamWritersToClose);

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
