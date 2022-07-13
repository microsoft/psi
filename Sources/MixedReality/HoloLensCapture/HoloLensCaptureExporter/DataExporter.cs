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
        private const string VideoStreamName = "VideoImageCameraView";
        private const string VideoEncodedStreamName = "VideoEncodedImageCameraView";
        private const string AudioStreamName = "Audio";

        /// <summary>
        /// Exports data based on the specified export command.
        /// </summary>
        /// <param name="exportCommand">The export command.</param>
        /// <returns>An error code or 0 if success.</returns>
        public static int Run(Verbs.ExportCommand exportCommand)
        {
            var pngEncoder = new ImageToPngStreamEncoder();
            var decoder = new ImageFromStreamDecoder();

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
            var eyes = store.OpenStreamOrDefault<Ray3D>("Eyes");
            var hands = store.OpenStreamOrDefault<(Hand Left, Hand Right)>("Hands");
            var audio = store.OpenStreamOrDefault<AudioBuffer>(AudioStreamName);
            var videoEncodedImageCameraView = store.OpenStreamOrDefault<EncodedImageCameraView>(VideoEncodedStreamName);
            var videoImageCameraView = store.OpenStreamOrDefault<ImageCameraView>(VideoStreamName);
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
            VerifyMutualExclusivity(ahatInfraredEncodedImageCameraView, ahatInfraredImageCameraView, "ahat-infrared");
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
            Export("AhatInfrared", ahatInfraredImageCameraView, ahatInfraredEncodedImageCameraView);
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
            accelerometer?.SelectManyImuSamples().Export("IMU", "Accelerometer", exportCommand.OutputPath, streamWritersToClose);
            gyroscope?.SelectManyImuSamples().Export("IMU", "Gyroscope", exportCommand.OutputPath, streamWritersToClose);
            magnetometer?.SelectManyImuSamples().Export("IMU", "Magnetometer", exportCommand.OutputPath, streamWritersToClose);

            // Export head, eyes and hands streams
            head?.Export("Head", exportCommand.OutputPath, streamWritersToClose);
            eyes?.Export("Eyes", exportCommand.OutputPath, streamWritersToClose);
            hands?.Select(x => x.Left).Export("Hands", "Left", exportCommand.OutputPath, streamWritersToClose);
            hands?.Select(x => x.Right).Export("Hands", "Right", exportCommand.OutputPath, streamWritersToClose);

            // Export audio
            audio?.Export("Audio", exportCommand.OutputPath, streamWritersToClose);

            // Export scene understanding
            sceneUnderstanding?.Export("SceneUnderstanding", exportCommand.OutputPath, streamWritersToClose);

            // If we have any video frames, we will export to MPEG in a new pipeline later
            long videoFrameCount = 0;
            var videoFrameTimeSpan = default(TimeSpan);
            IStreamMetadata videoMeta = null;
            if (decodedVideo is not null)
            {
                videoMeta = store.Contains(VideoStreamName) ? store.GetMetadata(VideoStreamName) : store.GetMetadata(VideoEncodedStreamName);
                videoFrameCount = videoMeta.MessageCount;
                videoFrameTimeSpan = videoMeta.LastMessageOriginatingTime - videoMeta.FirstMessageOriginatingTime;
            }

            Console.WriteLine($"Exporting {exportCommand.StoreName} to {exportCommand.OutputPath}");
            p.RunAsync(ReplayDescriptor.ReplayAll, progress: new Progress<double>(p => Console.Write($"Progress: {p:P}\r")));
            p.WaitAll();

            foreach (var sw in streamWritersToClose)
            {
                sw.Close();
            }

            Console.WriteLine();
            Console.WriteLine("Done.");

            // Export MPEG video
            if (videoFrameCount > 0)
            {
                // Create a new pipeline
                using var mpegPipeline = Pipeline.Create(deliveryPolicy: DeliveryPolicy.Throttle);

                // Open the psi store for reading
                store = PsiStore.Open(mpegPipeline, exportCommand.StoreName, exportCommand.StorePath);

                // Get info needed for the mpeg writer
                (var width, var height, var mpegStartTime, var audioFormat) = GetAudioAndVideoInfo(exportCommand.StoreName, exportCommand.StorePath);

                // Write the "start time" of the mpeg to file (the minimum time of the first video message and first audio message)
                var mpegTimingFile = File.CreateText(EnsurePathExists(Path.Combine(exportCommand.OutputPath, "Video", "VideoMpegStartTime.txt")));
                mpegTimingFile.WriteLine($"{mpegStartTime.ToText()}");
                mpegTimingFile.Close();

                // Configure and initialize the mpeg writer
                var frameRateNumerator = (uint)(videoFrameCount - 1);
                var frameRateDenominator = (uint)(videoFrameTimeSpan.TotalSeconds + 0.5);
                var frameRate = frameRateNumerator / frameRateDenominator;
                var mpegFile = EnsurePathExists(Path.Combine(exportCommand.OutputPath, "Video", $"Video.mpeg"));
                var audioOutputFormat = WaveFormat.Create16BitPcm((int)(audioFormat?.SamplesPerSec ?? 0), audioFormat?.Channels ?? 0);
                var mpegWriter = new Mpeg4Writer(mpegPipeline, mpegFile, new Mpeg4WriterConfiguration()
                {
                    ImageWidth = (uint)width,
                    ImageHeight = (uint)height,
                    FrameRateNumerator = frameRateNumerator,
                    FrameRateDenominator = frameRateDenominator,
                    PixelFormat = PixelFormat.BGRA_32bpp,
                    TargetBitrate = 10000000,
                    ContainsAudio = audioFormat != null,
                    AudioBitsPerSample = audioOutputFormat.BitsPerSample,
                    AudioChannels = audioOutputFormat.Channels,
                    AudioSamplesPerSecond = audioOutputFormat.SamplesPerSec,
                });

                // Audio
                store.OpenStreamOrDefault<AudioBuffer>(AudioStreamName)
                    ?.Resample(new AudioResamplerConfiguration() { OutputFormat = audioOutputFormat, })
                    ?.PipeTo(mpegWriter.AudioIn);

                // Video
                decodedVideo = store.OpenStreamOrDefault<ImageCameraView>(VideoStreamName) ??
                    store.OpenStreamOrDefault<EncodedImageCameraView>(VideoEncodedStreamName).Decode(decoder);

                // interpolate with a frame clock for a consistent frame rate into the Mpeg4Writer
                var audioMeta = store.GetMetadata(AudioStreamName);
                var firstMediaOriginatingTime = (videoMeta.FirstMessageOriginatingTime < audioMeta.FirstMessageOriginatingTime ? videoMeta : audioMeta).FirstMessageOriginatingTime;
                var lastMediaOriginatingTime = (videoMeta.LastMessageOriginatingTime > audioMeta.LastMessageOriginatingTime ? videoMeta : audioMeta).LastMessageOriginatingTime;
                var frameClockInterval = TimeSpan.FromSeconds(1.0 / frameRate);
                var frameClockTime = firstMediaOriginatingTime;
                IEnumerable<(int, DateTime)> FrameClockTicks()
                {
                    while (frameClockTime < lastMediaOriginatingTime)
                    {
                        yield return (0, frameClockTime);
                        frameClockTime += frameClockInterval;
                    }
                }

                var frameClock = Generators.Sequence(mpegPipeline, FrameClockTicks());
                var interpolatedVideo = frameClock.Join(decodedVideo, Reproducible.Nearest<ImageCameraView>());

                // The mpeg writer component processes input video frames and audio buffers, and writes to the output mpeg file.
                // Because of the way the component works internally, if both input streams use a delivery policy of Throttle,
                // the component ends up being much slower than if enough data is always queued at its inputs ready to be processed.
                // Since the video arrives at a slower rate than the audio at the inputs to the component, we relax the
                // throttling threshold at the video input such that as many video frames as possible (up to a maximum of 1000)
                // are available to be processed by the mpeg writer. This avoids constantly throttling and unthrottling the
                // video source, which was causing a significant slowdown in the mpeg file export.
                interpolatedVideo.Select(x => x.Item2.ViewedObject).PipeTo(mpegWriter, DeliveryPolicy.QueueSizeThrottled(1000));

                // Execute the pipeline
                Console.WriteLine("Exporting MPEG video");
                mpegPipeline.RunAsync(ReplayDescriptor.ReplayAll, progress: new Progress<double>(p => Console.Write($"Progress: {p:P}\r")));
                mpegPipeline.WaitAll();
                Console.WriteLine();
                Console.WriteLine("Done.");
            }

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

        private static (int Width, int Height, DateTime StartTime, WaveFormat audioFormat) GetAudioAndVideoInfo(string storeName, string storePath)
        {
            // determine properties for the mpeg writer by peeking at the first video and audio messages
            using var p = Pipeline.Create();
            var store = PsiStore.Open(p, storeName, storePath);

            // Get the image width and height by looking at the first message of the video stream.
            // Also record the time of that first message.
            var width = 0;
            var height = 0;
            var videoStartTime = default(DateTime);
            var videoWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            if (store.Contains(VideoStreamName))
            {
                store.OpenStream<ImageCameraView>(VideoStreamName).First().Do((v, env) =>
                {
                    (width, height) = GetWidthAndHeight(v.ViewedObject.Resource);
                    videoStartTime = env.OriginatingTime;
                    videoWaitHandle.Set();
                });
            }
            else
            {
                store.OpenStream<EncodedImageCameraView>(VideoEncodedStreamName).First().Do((v, env) =>
                {
                    (width, height) = GetWidthAndHeight(v.ViewedObject.Resource);
                    videoStartTime = env.OriginatingTime;
                    videoWaitHandle.Set();
                });
            }

            // Get the audio format by examining the first audio message (if one exists).
            // Also record the time of that first message.
            WaveFormat audioFormat = null;
            var audioStartTime = default(DateTime);
            var audioWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            bool TryGetMetadata(string stream, out IStreamMetadata meta)
            {
                if (store.Contains(stream))
                {
                    meta = store.GetMetadata(stream);
                    return true;
                }
                else
                {
                    meta = null;
                    return false;
                }
            }

            if (TryGetMetadata(AudioStreamName, out var audioMeta) && audioMeta.MessageCount > 0)
            {
                store.OpenStream<AudioBuffer>(AudioStreamName).First().Do((a, env) =>
                {
                    audioFormat = a.Format;
                    audioStartTime = env.OriginatingTime;
                    audioWaitHandle.Set();
                });
            }
            else
            {
                audioWaitHandle.Set();
            }

            // Run the pipeline, just until we've read the first video and audio message
            p.RunAsync();
            WaitHandle.WaitAll(new WaitHandle[2] { videoWaitHandle, audioWaitHandle });

            // Determine the earlier of the two start times
            var startTime = videoStartTime;
            if (audioStartTime != default && audioStartTime < videoStartTime)
            {
                startTime = audioStartTime;
            }

            return (width, height, startTime, audioFormat);
        }

        private static (int Width, int Height) GetWidthAndHeight(IImage image)
        {
            if (image.PixelFormat != PixelFormat.BGRA_32bpp)
            {
                throw new ArgumentException($"Expected video stream of {PixelFormat.BGRA_32bpp} (found {image.PixelFormat}).");
            }

            return (image.Width, image.Height);
        }
    }
}
