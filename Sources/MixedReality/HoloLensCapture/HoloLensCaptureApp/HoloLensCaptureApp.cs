// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureApp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using HoloLens2ResearchMode;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.MixedReality.SceneUnderstanding;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.MixedReality.MediaCapture;
    using Microsoft.Psi.MixedReality.ResearchMode;
    using Microsoft.Psi.MixedReality.StereoKit;
    using Microsoft.Psi.MixedReality.WinRT;
    using Microsoft.Psi.Remoting;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKit;
    using Windows.Storage;
    using Color = System.Drawing.Color;
    using Microphone = Microsoft.Psi.MixedReality.StereoKit.Microphone;
    using OpenXRHandsSensor = Microsoft.Psi.MixedReality.OpenXR.HandsSensor;
    using StereoKitHeadSensor = Microsoft.Psi.MixedReality.StereoKit.HeadSensor;
    using WinRTGazeSensor = Microsoft.Psi.MixedReality.WinRT.GazeSensor;

    /// <summary>
    /// Capture app used to stream sensor data to the accompanying HoloLensCaptureServer.
    /// </summary>
    public class HoloLensCaptureApp
    {
        // version number shared by capture app and server to ensure compatiblity.
        private const string Version = "v1";

        // image quality settings for JPEG encoder (0.0 - 1.0)
        private const double GrayImageJpegQuality = 0.8;
        private const double InfraredImageJpegQuality = 0.9;

        // frame edges
        private const float FrameThickness = 0.001f;
        private const float FrameDistance = 0.30f;
        private const float FrameWidth = 0.285f;
        private const float FrameHeight = 0.145f;
        private const float FrameBottomClip = 0.023f; // clip bottom of frame to stay within camera view
        private const float FrameLabelInset = 0.006f;

        // Config settings
        private static readonly bool IncludeDiagnostics = true;

        private static readonly bool IncludeVideo = true;
        private static readonly bool IncludePreview = false;

        // see https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/locatable-camera#hololens-2
        // for different possible modes, such as: 896x504 @30/15; 960x540 @30,15; 1128x636 @30,15; 1280x720 @30/15 etc.
        private static readonly int PhotoVideoFps = 30;
        private static readonly int PhotoVideoImageWidth = 896;
        private static readonly int PhotoVideoImageHeight = 504;

        private static readonly bool IncludeDepth = true;
        private static readonly bool IncludeDepthCalibrationMap = false;
        private static readonly bool IncludeAhat = false;
        private static readonly bool IncludeAhatCalibrationMap = false;
        private static readonly bool IncludeDepthInfrared = false;
        private static readonly bool IncludeAhatInfrared = false;
        private static readonly bool EncodeInfrared = false;

        private static readonly bool IncludeGrayFrontCameras = true;
        private static readonly bool IncludeGrayFrontCameraCalibrationMap = false;
        private static readonly bool IncludeGraySideCameras = false;
        private static readonly bool IncludeGraySideCameraCalibrationMap = false;
        private static readonly GrayImageEncode EncodeGrayMethod = GrayImageEncode.Jpeg;
        private static readonly TimeSpan GrayInterval = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan CalibrationMapInterval = TimeSpan.FromHours(1);

        private static readonly string CalibrationFolderName = "Calibration";

        private static readonly bool IncludeImu = true;

        private static readonly bool IncludeHead = true;
        private static readonly TimeSpan HeadInterval = TimeSpan.FromMilliseconds(20);
        private static readonly bool IncludeEyes = true;
        private static readonly TimeSpan EyesInterval = TimeSpan.FromSeconds(1.0 / 45.0);
        private static readonly bool IncludeHands = true;
        private static readonly TimeSpan HandsInterval = TimeSpan.FromSeconds(1.0 / 45.0);

        private static readonly bool IncludeAudio = true;

        private static readonly bool IncludeSceneUnderstanding = true;
        private static readonly TimeSpan SceneUnderstandingInterval = TimeSpan.FromSeconds(60);
        private static readonly SceneQuerySettings SceneUnderstandingSettings = new ()
        {
            EnableSceneObjectMeshes = true,
            EnableSceneObjectQuads = true,
            EnableWorldMesh = true,
            EnableOnlyObservedSceneObjects = true,
            RequestedMeshLevelOfDetail = SceneMeshLevelOfDetail.Medium,
        };

        private static readonly Rectangle3D FrameRectangle = new (
            new (FrameDistance, 0, 0),
            UnitVector3D.YAxis.Negate(),
            UnitVector3D.ZAxis,
            -(FrameWidth / 2),
            -(FrameHeight / 3) * 2 + FrameBottomClip,
            FrameWidth,
            FrameHeight - FrameBottomClip);

        private static readonly Vec2 LabelSize = new (FrameWidth - FrameLabelInset * 2f, 0.008f);

        private static string captureServerAddress = "0.0.0.0";

        private enum State
        {
            WaitingToStart, // showing menu to Start / Exit
            ConstructPipeline, // construct pipeline to capture
            ConstructingPipeline, // constructing the pipeline
            CalibrateCameras, // calibrate the cameras
            CalibratingCameras, // camera calibration in progress
            ConnectToCaptureServer, // connect to the capture server
            WaitingForCaptureServer, // wating for capture server to connect
            Capturing, // pipeline running and capturing streams
            StoppingPipeline, // stopping pipeline
            Exited, // app has exited
        }

        private enum GrayImageEncode
        {
            None,
            Jpeg,
            Gzip,
        }

        private static void Main()
        {
            // Initialize StereoKit
            if (!SK.Initialize(
                new SKSettings
                {
                    appName = "HoloLensCaptureApp",
                    assetsFolder = "Assets",
                }))
            {
                throw new Exception("StereoKit failed to initialize.");
            }

            // Initialize MixedReality statics
            MixedReality.Initialize(regenerateDefaultWorldSpatialAnchorIfNeeded: true);

            // Attempt to get server address from config file
            var docs = KnownFolders.DocumentsLibrary;
            InitializeCaptureServerAddressAsync(docs, "CaptureServerIP.txt").GetAwaiter().GetResult();

            var pipeline = default(Pipeline);

            var accelerometer = default(Accelerometer);
            var gyroscope = default(Gyroscope);
            var magnetometer = default(Magnetometer);
            var camera = default(PhotoVideoCamera);
            var scene = default(SceneUnderstanding);
            var depthCamera = default(DepthCamera);
            var depthAhatCamera = default(DepthCamera);
            var leftFrontCamera = default(VisibleLightCamera);
            var rightFrontCamera = default(VisibleLightCamera);
            var leftLeftCamera = default(VisibleLightCamera);
            var rightRightCamera = default(VisibleLightCamera);
            var head = default(StereoKitHeadSensor);
            var eyes = default(WinRTGazeSensor);
            var hands = default(OpenXRHandsSensor);
            IProducer<AudioBuffer> audio = null;

            string errorMessage = null;
            var windowCoordinateSystem = default(CoordinateSystem);
            var windowPose = default(Pose);
            var captureServerProcess = default(Rendezvous.Process);

            var lastServerHeartBeat = DateTime.MaxValue;
            var serverHeartBeatTimeout = TimeSpan.FromSeconds(5);

            var videoFps = 0f;
            var depthFps = 0f;

            var state = State.WaitingToStart;

            while (SK.Step(() =>
            {
                // Setup the window coordinate system (the startup location of the menu)
                if (windowCoordinateSystem == null)
                {
                    var head = Input.Head.ToCoordinateSystem();
                    var ahead = head.Origin + head.XAxis.ScaleBy(0.7);
                    var origin = new Point3D(ahead.X, ahead.Y, head.Origin.Z);
                    var axisX = (head.Origin - origin).Normalize();
                    var axisY = UnitVector3D.ZAxis.CrossProduct(axisX);
                    var axisZ = axisX.CrossProduct(axisY);
                    windowCoordinateSystem = new CoordinateSystem(origin, axisX, axisY, axisZ);
                    windowPose = windowCoordinateSystem.ToStereoKitPose();
                }

                // Setup the handle
                var windowBounds = Bounds.FromCorner(new Vec3(0, 0, -2) * U.cm, new Vec3(4, 4, 4) * U.cm);

                switch (state)
                {
                    case State.WaitingToStart:
                        UI.EnableFarInteract = true;
                        lastServerHeartBeat = DateTime.MaxValue;
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        UI.Label($"Server: {captureServerAddress}");
                        if (UI.Button($"Start"))
                        {
                            state = State.ConstructPipeline;
                        }

                        if (UI.Button("Exit"))
                        {
                            state = State.Exited;
                            Task.Run(() => SK.Shutdown());
                        }

                        UI.HandleEnd();
                        break;
                    case State.ConstructPipeline:
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        UI.Label("Please wait! Constructing capture pipeline ...");
                        UI.HandleEnd();
                        DrawFrame(Color.Yellow.ToStereoKitColor());
                        state = State.ConstructingPipeline;
                        Task.Run(() =>
                        {
                            pipeline = Pipeline.Create(
                                enableDiagnostics: IncludeDiagnostics,
                                diagnosticsConfiguration: new DiagnosticsConfiguration()
                                {
                                    SamplingInterval = TimeSpan.FromSeconds(5),
                                });

                            // IMU SENSORS
                            accelerometer = IncludeImu ? new Accelerometer(pipeline) : null;
                            gyroscope = IncludeImu ? new Gyroscope(pipeline) : null;
                            magnetometer = IncludeImu ? new Magnetometer(pipeline) : null;

                            // HEAD, EYES, AND HANDS
                            head = IncludeHead ? new StereoKitHeadSensor(pipeline, HeadInterval) : null;
                            eyes = IncludeEyes ? new WinRTGazeSensor(pipeline, new GazeSensorConfiguration()
                            {
                                OutputEyeGaze = true,
                                OutputHeadGaze = false,
                                Interval = EyesInterval,
                            }) : null;

                            hands = IncludeHands ? new OpenXRHandsSensor(pipeline, HandsInterval) : null;

                            // AUDIO
                            audio = IncludeAudio ? new Microphone(pipeline).Reframe(16384, DeliveryPolicy.Unlimited) : null;

                            // PHOTOVIDEO CAMERA
                            var videoStreamSettings = IncludeVideo ? new PhotoVideoCameraConfiguration.StreamSettings
                            {
                                FrameRate = PhotoVideoFps,
                                ImageWidth = PhotoVideoImageWidth,
                                ImageHeight = PhotoVideoImageHeight,
                                OutputEncodedImage = false,
                                OutputEncodedImageCameraView = true,
                            }
                            : null;

                            var previewStreamSettings = IncludePreview ? new PhotoVideoCameraConfiguration.StreamSettings
                            {
                                FrameRate = PhotoVideoFps,
                                ImageWidth = PhotoVideoImageWidth,
                                ImageHeight = PhotoVideoImageHeight,
                                OutputEncodedImage = false,
                                OutputEncodedImageCameraView = true,
                                MixedRealityCapture = new (),
                            }
                            : null;

                            camera = IncludeVideo || IncludePreview ? new PhotoVideoCamera(pipeline, new PhotoVideoCameraConfiguration
                            {
                                VideoStreamSettings = videoStreamSettings,
                                PreviewStreamSettings = previewStreamSettings,
                            })
                            : null;

                            // DEPTH CAMERA - LONG THROW
                            depthCamera = (IncludeDepth || IncludeDepthInfrared || IncludeDepthCalibrationMap) ? new DepthCamera(pipeline, new DepthCameraConfiguration
                            {
                                DepthSensorType = ResearchModeSensorType.DepthLongThrow,
                                OutputCameraIntrinsics = false,
                                OutputPose = false,
                                OutputDepthImage = false,
                                OutputInfraredImage = false,
                                OutputDepthImageCameraView = IncludeDepth,
                                OutputInfraredImageCameraView = IncludeDepthInfrared,
                                OutputCalibrationPointsMap = IncludeDepthCalibrationMap,
                                OutputCalibrationPointsMapMinInterval = CalibrationMapInterval,
                            })
                            : null;

                            // DEPTH CAMERA - AHAT
                            depthAhatCamera = (IncludeAhat || IncludeAhatInfrared || IncludeAhatCalibrationMap) ? new DepthCamera(pipeline, new DepthCameraConfiguration
                            {
                                DepthSensorType = ResearchModeSensorType.DepthAhat,
                                OutputCameraIntrinsics = false,
                                OutputPose = false,
                                OutputDepthImage = false,
                                OutputInfraredImage = false,
                                OutputDepthImageCameraView = IncludeAhat,
                                OutputInfraredImageCameraView = IncludeAhatInfrared,
                                OutputCalibrationPointsMap = IncludeAhatCalibrationMap,
                                OutputCalibrationPointsMapMinInterval = CalibrationMapInterval,
                            })
                            : null;

                            // GRAY FRONT CAMERAS
                            leftFrontCamera = IncludeGrayFrontCameras ? new VisibleLightCamera(pipeline, new VisibleLightCameraConfiguration
                            {
                                VisibleLightSensorType = ResearchModeSensorType.LeftFront,
                                OutputMinInterval = GrayInterval,
                                OutputCameraIntrinsics = false,
                                OutputPose = false,
                                OutputImage = false,
                                OutputImageCameraView = true,
                                OutputCalibrationPointsMap = IncludeGrayFrontCameraCalibrationMap,
                                OutputCalibrationPointsMapMinInterval = CalibrationMapInterval,
                            })
                            : null;

                            rightFrontCamera = IncludeGrayFrontCameras ? new VisibleLightCamera(pipeline, new VisibleLightCameraConfiguration
                            {
                                VisibleLightSensorType = ResearchModeSensorType.RightFront,
                                OutputMinInterval = GrayInterval,
                                OutputCameraIntrinsics = false,
                                OutputPose = false,
                                OutputImage = false,
                                OutputImageCameraView = true,
                                OutputCalibrationPointsMap = IncludeGrayFrontCameraCalibrationMap,
                                OutputCalibrationPointsMapMinInterval = CalibrationMapInterval,
                            })
                            : null;

                            // GRAY SIDE CAMERAS
                            leftLeftCamera = IncludeGraySideCameras ? new VisibleLightCamera(pipeline, new VisibleLightCameraConfiguration
                            {
                                VisibleLightSensorType = ResearchModeSensorType.LeftLeft,
                                OutputMinInterval = GrayInterval,
                                OutputCameraIntrinsics = false,
                                OutputPose = false,
                                OutputImage = false,
                                OutputImageCameraView = true,
                                OutputCalibrationPointsMap = IncludeGraySideCameraCalibrationMap,
                                OutputCalibrationPointsMapMinInterval = CalibrationMapInterval,
                            })
                            : null;

                            rightRightCamera = IncludeGraySideCameras ? new VisibleLightCamera(pipeline, new VisibleLightCameraConfiguration
                            {
                                VisibleLightSensorType = ResearchModeSensorType.RightRight,
                                OutputMinInterval = GrayInterval,
                                OutputCameraIntrinsics = false,
                                OutputPose = false,
                                OutputImage = false,
                                OutputImageCameraView = true,
                                OutputCalibrationPointsMap = IncludeGraySideCameraCalibrationMap,
                                OutputCalibrationPointsMapMinInterval = CalibrationMapInterval,
                            })
                            : null;

                            // SCENE UNDERSTANDING
                            scene = IncludeSceneUnderstanding ? new SceneUnderstanding(pipeline, new SceneUnderstandingConfiguration
                            {
                                MinQueryInterval = SceneUnderstandingInterval,
                                SceneQuerySettings = SceneUnderstandingSettings,
                            }) : null;

                            if (IncludeDepth || IncludeAhat || IncludeDepthInfrared || IncludeAhatInfrared || IncludeGrayFrontCameras || IncludeGraySideCameras)
                            {
                                state = State.CalibrateCameras;
                            }
                            else
                            {
                                state = State.ConnectToCaptureServer;
                            }
                        });
                        break;
                    case State.ConstructingPipeline:
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        if (errorMessage != null)
                        {
                            UI.Label($"Error: {errorMessage}");
                        }

                        UI.Label("Please wait! Constructing capture pipeline ...");
                        UI.HandleEnd();
                        DrawFrame(Color.Yellow.ToStereoKitColor());
                        break;
                    case State.CalibrateCameras:
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        UI.Label("Please wait! Calibrating cameras ...");
                        UI.HandleEnd();
                        DrawFrame(Color.Yellow.ToStereoKitColor());
                        state = State.CalibratingCameras;

                        // Calibrate all cameras with async tasks
                        List<Task> calibrationTasks = new ();
                        Task.Run(async () =>
                        {
                            // Open (or create) the Documents folder containing calibration files
                            var calibrationFolder = await docs.CreateFolderAsync(CalibrationFolderName, CreationCollisionOption.OpenIfExists);
                            calibrationTasks.Add(depthCamera?.CalibrateFromFileAsync(calibrationFolder));
                            calibrationTasks.Add(depthAhatCamera?.CalibrateFromFileAsync(calibrationFolder));
                            calibrationTasks.Add(leftFrontCamera?.CalibrateFromFileAsync(calibrationFolder));
                            calibrationTasks.Add(rightFrontCamera?.CalibrateFromFileAsync(calibrationFolder));
                            calibrationTasks.Add(leftLeftCamera?.CalibrateFromFileAsync(calibrationFolder));
                            calibrationTasks.Add(rightRightCamera?.CalibrateFromFileAsync(calibrationFolder));
                            await Task.WhenAll(calibrationTasks.Where(t => t is not null).ToArray());
                            state = State.ConnectToCaptureServer;
                        });
                        break;
                    case State.CalibratingCameras:
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        if (errorMessage != null)
                        {
                            UI.Label($"Error: {errorMessage}");
                        }

                        UI.Label("Please wait! Calibrating cameras ...");
                        UI.HandleEnd();
                        DrawFrame(Color.Yellow.ToStereoKitColor());
                        break;
                    case State.ConnectToCaptureServer:
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        UI.Label($"Please wait! Connecting to capture server: {captureServerAddress}");
                        UI.HandleEnd();
                        DrawFrame(Color.Yellow.ToStereoKitColor());
                        state = State.WaitingForCaptureServer;
                        Task.Run(() =>
                        {
                            // Create the rendezvous client and process
                            var rendezvousClient = new RendezvousClient(captureServerAddress);
                            var remoteClock = new RemoteClockExporter();

                            void ResetState()
                            {
                                rendezvousClient?.Rendezvous.TryRemoveProcess(nameof(HoloLensCaptureApp));
                                rendezvousClient?.Dispose();
                                remoteClock?.Dispose();
                                captureServerProcess = null;
                                errorMessage = null;
                                state = State.WaitingToStart;
                            }

                            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                            {
                                ResetState();
                            };

                            pipeline.PipelineExceptionNotHandled += (_, ex) =>
                            {
                                Trace.WriteLine($"Pipeline Error: {ex.Exception.Message}");
                                ResetState();
                            };

                            pipeline.PipelineCompleted += (_, _) =>
                            {
                                ResetState();
                            };

                            try
                            {
                                rendezvousClient.Start();
                            }
                            catch (Exception ex)
                            {
                                errorMessage = ex.Message;
                            }

                            if (rendezvousClient.IsActive)
                            {
                                rendezvousClient.Connected.WaitOne();
                                rendezvousClient.Rendezvous.TryRemoveProcess(nameof(HoloLensCaptureApp)); // in case a previous instance crashed

                                var headsetAddress = rendezvousClient.ClientAddress;

                                var process = new Rendezvous.Process(nameof(HoloLensCaptureApp), Version);

                                // Sync clocks
                                process.AddEndpoint(remoteClock.ToRendezvousEndpoint(headsetAddress));

                                // Publish streams to the rendezvous process
                                void Write<T>(string name, IProducer<T> producer, int port, IFormatSerializer serializer, DeliveryPolicy deliveryPolicy)
                                {
                                    var tcpWriter = new TcpWriter<T>(pipeline, port, serializer);
                                    producer.PipeTo(tcpWriter, deliveryPolicy);
                                    process.AddEndpoint(tcpWriter.ToRendezvousEndpoint<T>(headsetAddress, name));
                                }

                                var port = 30000;

                                if (IncludeImu)
                                {
                                    Write("Accelerometer", accelerometer?.Out, port++, Serializers.ImuFormat(), DeliveryPolicy.LatestMessage);
                                    Write("Gyroscope", gyroscope?.Out, port++, Serializers.ImuFormat(), DeliveryPolicy.LatestMessage);
                                    Write("Magnetometer", magnetometer?.Out, port++, Serializers.ImuFormat(), DeliveryPolicy.LatestMessage);
                                }

                                if (IncludeHead)
                                {
                                    Write("Head", head?.Out, port++, Serializers.CoordinateSystemFormat(), DeliveryPolicy.LatestMessage);
                                }

                                if (IncludeEyes)
                                {
                                    Write("Eyes", eyes?.Eyes, port++, Serializers.WinRTEyesFormat(), DeliveryPolicy.LatestMessage);
                                }

                                if (IncludeHands)
                                {
                                    Write("Hands", hands?.Out, port++, Serializers.OpenXRHandsFormat(), DeliveryPolicy.LatestMessage);
                                }

                                if (IncludeAudio)
                                {
                                    Write("Audio", audio?.Out, port++, Serializers.AudioBufferFormat(), DeliveryPolicy.Unlimited);
                                }

                                if (IncludeVideo)
                                {
                                    Write("VideoEncodedImageCameraView", camera.VideoEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
                                }

                                if (IncludePreview)
                                {
                                    Write("PreviewEncodedImageCameraView", camera.PreviewEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
                                }

                                if (IncludeDepth)
                                {
                                    Write("DepthImageCameraView", depthCamera?.DepthImageCameraView, port++, Serializers.DepthImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
#if DEBUG
                                    Write("DepthDebugOutOfOrderFrames", depthCamera?.DebugOutOfOrderFrames, port++, Serializers.Int32Format(), DeliveryPolicy.Unlimited);
#endif
                                }

                                if (IncludeDepthInfrared)
                                {
                                    if (EncodeInfrared)
                                    {
                                        var infraredEncodedImageCameraView = depthCamera?.InfraredImageCameraView.Encode(new ImageToGZipStreamEncoder(), DeliveryPolicy.LatestMessage);
                                        Write("DepthInfraredEncodedImageCameraView", infraredEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
                                    }
                                    else
                                    {
                                        Write("DepthInfraredImageCameraView", depthCamera?.InfraredImageCameraView, port++, Serializers.ImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
                                    }
                                }

                                if (IncludeDepthCalibrationMap)
                                {
                                    Write("DepthCalibrationMap", depthCamera?.CalibrationPointsMap, port++, Serializers.CalibrationPointsMapFormat(), DeliveryPolicy.Unlimited);
                                }

                                if (IncludeAhat)
                                {
                                    Write("AhatDepthImageCameraView", depthAhatCamera?.DepthImageCameraView, port++, Serializers.DepthImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
#if DEBUG
                                    Write("AhatDebugOutOfOrderFrames", depthAhatCamera?.DebugOutOfOrderFrames, port++, Serializers.Int32Format(), DeliveryPolicy.Unlimited);
#endif
                                }

                                if (IncludeAhatInfrared)
                                {
                                    if (EncodeInfrared)
                                    {
                                        var infraredEncodedImageCameraView = depthAhatCamera?.InfraredImageCameraView.Encode(new ImageToGZipStreamEncoder(), DeliveryPolicy.LatestMessage);
                                        Write("AhatDepthInfraredEncodedImageCameraView", infraredEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
                                    }
                                    else
                                    {
                                        Write("AhatDepthInfraredImageCameraView", depthAhatCamera?.InfraredImageCameraView, port++, Serializers.ImageCameraViewFormat(), DeliveryPolicy.LatestMessage);
                                    }
                                }

                                if (IncludeAhatCalibrationMap)
                                {
                                    Write("AhatDepthCalibrationMap", depthAhatCamera?.CalibrationPointsMap, port++, Serializers.CalibrationPointsMapFormat(), DeliveryPolicy.Unlimited);
                                }

                                if (IncludeGrayFrontCameras)
                                {
                                    switch (EncodeGrayMethod)
                                    {
                                        case GrayImageEncode.Jpeg:
                                            var leftFrontJpegEncodedImageCameraView = leftFrontCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToJpegStreamEncoder(GrayImageJpegQuality), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("LeftFrontEncodedImageCameraView", leftFrontJpegEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            var rightFrontJpegEncodedImageCameraView = rightFrontCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToJpegStreamEncoder(GrayImageJpegQuality), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("RightFrontEncodedImageCameraView", rightFrontJpegEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            break;

                                        case GrayImageEncode.Gzip:
                                            var leftFrontGZipEncodedImageCameraView = leftFrontCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToGZipStreamEncoder(), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("LeftFrontGzipImageCameraView", leftFrontGZipEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            var rightFrontGZipEncodedImageCameraView = rightFrontCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToGZipStreamEncoder(), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("RightFrontGzipImageCameraView", rightFrontGZipEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            break;

                                        case GrayImageEncode.None:
                                            var leftFrontImageView = leftFrontCamera.ImageCameraView;
                                            Write("LeftFrontImageView", leftFrontImageView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            var rightFrontImageView = rightFrontCamera.ImageCameraView;
                                            Write("RightFrontImageView", rightFrontImageView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            break;
                                    }

                                    if (IncludeGrayFrontCameraCalibrationMap)
                                    {
                                        Write("LeftFrontCalibrationMap", leftFrontCamera?.CalibrationPointsMap, port++, Serializers.CalibrationPointsMapFormat(), DeliveryPolicy.Unlimited);
                                        Write("RightFrontCalibrationMap", rightFrontCamera?.CalibrationPointsMap, port++, Serializers.CalibrationPointsMapFormat(), DeliveryPolicy.Unlimited);
                                    }
                                }

                                if (IncludeGraySideCameras)
                                {
                                    switch (EncodeGrayMethod)
                                    {
                                        case GrayImageEncode.Jpeg:
                                            var leftLeftJpegEncodedImageCameraView = leftLeftCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToJpegStreamEncoder(GrayImageJpegQuality), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("LeftLeftEncodedImageCameraView", leftLeftJpegEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            var rightRightJpegEncodedImageCameraView = rightRightCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToJpegStreamEncoder(GrayImageJpegQuality), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("RightRightEncodedImageCameraView", rightRightJpegEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            break;

                                        case GrayImageEncode.Gzip:
                                            var leftLeftGZipEncodedImageCameraView = leftLeftCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToGZipStreamEncoder(), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("LeftLeftGzipImageCameraView", leftLeftGZipEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            var rightRightGZipEncodedImageCameraView = rightRightCamera?.ImageCameraView
                                                .Convert(PixelFormat.BGRA_32bpp, DeliveryPolicy.LatestMessage)
                                                .Encode(new ImageToGZipStreamEncoder(), DeliveryPolicy.SynchronousOrThrottle);
                                            Write("RightRightGzipImageCameraView", rightRightGZipEncodedImageCameraView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            break;

                                        case GrayImageEncode.None:
                                            var leftLeftImageView = leftLeftCamera.ImageCameraView;
                                            Write("LeftLeftImageView", leftLeftImageView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            var rightRightImageView = rightRightCamera.ImageCameraView;
                                            Write("RightRightImageView", rightRightImageView, port++, Serializers.EncodedImageCameraViewFormat(), DeliveryPolicy.LatestMessage);

                                            break;
                                    }

                                    if (IncludeGraySideCameraCalibrationMap)
                                    {
                                        Write("LeftLeftCalibrationMap", leftLeftCamera?.CalibrationPointsMap, port++, Serializers.CalibrationPointsMapFormat(), DeliveryPolicy.Unlimited);
                                        Write("RightRightCalibrationMap", rightRightCamera?.CalibrationPointsMap, port++, Serializers.CalibrationPointsMapFormat(), DeliveryPolicy.Unlimited);
                                    }
                                }

                                if (IncludeSceneUnderstanding)
                                {
                                    Write("SceneUnderstanding", scene?.Out, port++, Serializers.SceneObjectCollectionFormat(), DeliveryPolicy.LatestMessage);
                                }

                                if (IncludeDiagnostics)
                                {
                                    Write("HoloLensDiagnostics", pipeline.Diagnostics, port++, Serializers.PipelineDiagnosticsFormat(), DeliveryPolicy.LatestMessage);
                                }

                                rendezvousClient.Rendezvous.ProcessAdded += (_, process) =>
                                {
                                    if (process.Name == "HoloLensCaptureServer")
                                    {
                                        if (process.Version != Version)
                                        {
                                            throw new Exception($"Connection received from unexpected version of HoloLensCaptureServer (expected {Version}, actual {process.Version}).");
                                        }

                                        captureServerProcess = process;
                                    }
                                };

                                rendezvousClient.Rendezvous.ProcessRemoved += (_, process) =>
                                {
                                    if (process.Name == "HoloLensCaptureServer")
                                    {
                                        Trace.WriteLine($"Server shutdown");
                                        ResetState();
                                    }
                                };

                                rendezvousClient.Rendezvous.TryAddProcess(process);
                            }
                        });
                        break;
                    case State.WaitingForCaptureServer:
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        if (errorMessage != null)
                        {
                            UI.Label($"Error: {errorMessage}");
                            if (UI.Button("Exit"))
                            {
                                state = State.Exited;
                                Task.Run(() => SK.Shutdown());
                            }
                        }
                        else
                        {
                            UI.Label($"Please wait! Connecting to capture server: {captureServerAddress}");
                            if (UI.Button($"Stop"))
                            {
                                state = State.StoppingPipeline;
                                Task.Run(() =>
                                {
                                    pipeline.Dispose();
                                    pipeline = null;
                                });
                            }
                        }

                        UI.HandleEnd();
                        DrawFrame(Color.Yellow.ToStereoKitColor());

                        if (captureServerProcess != default)
                        {
                            // Get the server heartbeat stream from the rendezvous server
                            foreach (var endpoint in captureServerProcess.Endpoints)
                            {
                                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint)
                                {
                                    foreach (var stream in tcpEndpoint.Streams)
                                    {
                                        if (stream.StreamName == $"ServerHeartbeat")
                                        {
                                            // note: using captureServerAddress -- ignoring tcpEndpoint.Host (0.0.0.0)
                                            var serverHeartbeat = new TcpSource<(float, float)>(pipeline, captureServerAddress, tcpEndpoint.Port, Serializers.HeartbeatFormat());
                                            serverHeartbeat.Do(fps =>
                                            {
                                                (videoFps, depthFps) = fps;
                                                lastServerHeartBeat = DateTime.UtcNow;
                                            });

                                            // Run the pipeline
                                            pipeline.PipelineExceptionNotHandled += (_, e) => errorMessage = e.Exception.Message;
                                            pipeline.RunAsync();
                                            state = State.Capturing;
                                        }
                                    }
                                }
                                else
                                {
                                    throw new Exception("Unexpected endpoint type.");
                                }
                            }
                        }

                        break;
                    case State.Capturing:
                        UI.EnableFarInteract = false;
                        var frameColor = Color.Yellow;
                        var labelText = string.Empty;
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        if (errorMessage != null)
                        {
                            labelText = $"Error: {errorMessage}";
                            UI.Label(labelText);
                        }

                        if (lastServerHeartBeat == DateTime.MaxValue)
                        {
                            UI.Label("Please wait! Looking for server heartbeat ...");
                        }
                        else if (DateTime.UtcNow - lastServerHeartBeat > serverHeartBeatTimeout)
                        {
                            labelText = "Server connection lost";
                            UI.Label($"{labelText} ({lastServerHeartBeat.ToLocalTime()})");
                            videoFps = 0;
                            depthFps = 0;
                        }
                        else if (StereoKitTransforms.WorldHierarchy == null)
                        {
                            frameColor = Color.Red;
                            labelText = "Localization temporarily lost! Re-localizing...";
                        }
                        else if (errorMessage == null)
                        {
                            UI.Label("Capturing ...");
                            labelText = $"FPS - Video:{videoFps:0.#} Depth:{depthFps:0.#}";
                            frameColor = Color.DarkGray;
                        }

                        if (UI.Button($"Stop"))
                        {
                            state = State.StoppingPipeline;
                            Task.Run(() =>
                            {
                                pipeline.Dispose();
                                pipeline = null;
                            });
                        }

                        UI.HandleEnd();
                        DrawFrame(frameColor.ToStereoKitColor(), labelText);

                        break;
                    case State.StoppingPipeline:
                        UI.HandleBegin("Handle", ref windowPose, windowBounds, true);
                        if (errorMessage != null)
                        {
                            UI.Label($"Error: {errorMessage}");
                        }

                        UI.Label("Please wait! Capture shutting down ...");
                        UI.HandleEnd();
                        DrawFrame(Color.Yellow.ToStereoKitColor());
                        break;
                }
            }))
            {
            }

            pipeline?.Dispose();

            SK.Shutdown();
        }

        private static async Task InitializeCaptureServerAddressAsync(StorageFolder folder, string configFile)
        {
            try
            {
                var config = await folder.GetFileAsync(configFile);
                captureServerAddress = await FileIO.ReadTextAsync(config);
            }
            catch (FileNotFoundException)
            {
                // use default and save sample settings file
                var config = await folder.CreateFileAsync(configFile, CreationCollisionOption.FailIfExists);
                await FileIO.WriteTextAsync(config, captureServerAddress);
            }
        }

        private static void DrawFrame(Color32 color, string labelText = null)
        {
            // position relative to head pose
            var head = Input.Head.ToCoordinateSystem();
            var rect = head.Transform(FrameRectangle);

            // draw border lines
            var p0 = rect.TopLeft.ToVec3();
            var p1 = rect.TopRight.ToVec3();
            var p2 = rect.BottomRight.ToVec3();
            var p3 = rect.BottomLeft.ToVec3();
            Lines.Add(p0, p1, color, FrameThickness);
            Lines.Add(p1, p2, color, FrameThickness);
            Lines.Add(p2, p3, color, FrameThickness);
            Lines.Add(p3, p0, color, FrameThickness);

            // render label
            var widthAxis = (rect.BottomRight - rect.BottomLeft).Normalize();
            var heightAxis = (rect.TopLeft - rect.BottomLeft).Normalize();
            var normalAxis = widthAxis.CrossProduct(heightAxis);
            var psiCoordinateSystem = new CoordinateSystem(rect.GetCenter(), normalAxis, widthAxis, heightAxis);
            var labelPose = psiCoordinateSystem.ToStereoKitMatrix();
            Text.Add(labelText, labelPose, LabelSize, TextFit.Squeeze, offY: -(FrameHeight / 2 - FrameLabelInset));
        }
    }
}
