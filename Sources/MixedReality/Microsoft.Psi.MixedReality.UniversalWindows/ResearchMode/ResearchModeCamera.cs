// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.ResearchMode
{
    using System;
    using System.IO;
    using System.Numerics;
    using System.Threading;
    using System.Threading.Tasks;
    using HoloLens2ResearchMode;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;
    using Windows.Foundation;
    using Windows.Perception;
    using Windows.Perception.Spatial;
    using Windows.Perception.Spatial.Preview;
    using Windows.Storage;

    /// <summary>
    /// Represents an abstract base class for a HoloLens 2 research mode camera component.
    /// </summary>
    public abstract class ResearchModeCamera : ISourceComponent
    {
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
        // Camera basis (x - right, y - down, z - forward) relative
        // to the HoloLens basis (x - right, y - up, z - back)
        private static readonly Matrix4x4 CameraBasis = new (
            1,  0,  0,  0,
            0, -1,  0,  0,
            0,  0, -1,  0,
            0,  0,  0,  1);
#pragma warning restore SA1117 // Parameters should be on same line or separate lines

        private readonly Pipeline pipeline;
        private readonly ResearchModeCameraConfiguration configuration;
        private readonly string name;
        private readonly ResearchModeSensorDevice sensorDevice;
        private readonly ResearchModeCameraSensor cameraSensor;
        private readonly Task<ResearchModeSensorConsent> requestCameraAccessTask;
        private readonly SpatialLocator rigNodeLocator;

        private bool pipelineIsRunning = false;
        private CameraIntrinsics cameraIntrinsics = null;
        private CoordinateSystem cameraPose = null;
        private CalibrationPointsMap calibrationPointsMap = null;
        private Matrix4x4? invertedCameraExtrinsics = null;
        private Thread captureThread;
        private bool shutdown;

#if DEBUG
        private DateTime previousFrameOriginatingTime;
        private int outOfOrderFrameCount;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="ResearchModeCamera"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The research mode camera configuration.</param>
        /// <param name="name">An optional name for the component.</param>
        protected ResearchModeCamera(Pipeline pipeline, ResearchModeCameraConfiguration configuration, string name = nameof(ResearchModeCamera))
        {
            this.pipeline = pipeline;
            this.configuration = configuration;
            this.name = name;
            this.pipeline.PipelineRun += (_, _) => this.pipelineIsRunning = true;

            this.Pose = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.Pose));
            this.CameraIntrinsics = pipeline.CreateEmitter<ICameraIntrinsics>(this, nameof(this.CameraIntrinsics));
            this.CalibrationPointsMap = pipeline.CreateEmitter<CalibrationPointsMap>(this, nameof(this.CalibrationPointsMap));

            this.sensorDevice = new ResearchModeSensorDevice();
            this.requestCameraAccessTask = this.sensorDevice.RequestCameraAccessAsync().AsTask();
            this.cameraSensor = (ResearchModeCameraSensor)this.sensorDevice.GetSensor(this.configuration.SensorType);

            Guid rigNodeGuid = this.sensorDevice.GetRigNodeId();
            this.rigNodeLocator = SpatialGraphInteropPreview.CreateLocatorForNode(rigNodeGuid);

#if DEBUG
            // Debug stream to track out-of-order frames which occasionally occur
            this.DebugOutOfOrderFrames = pipeline.CreateEmitter<int>(this, nameof(this.DebugOutOfOrderFrames));
#endif
        }

        /// <summary>
        /// Gets the camera pose stream.
        /// </summary>
        public Emitter<CoordinateSystem> Pose { get; }

        /// <summary>
        /// Gets the camera intrinsics stream.
        /// </summary>
        public Emitter<ICameraIntrinsics> CameraIntrinsics { get; } = null;

        /// <summary>
        /// Gets the stream for calibration map (image points and corresponding 3D camera points).
        /// </summary>
        public Emitter<CalibrationPointsMap> CalibrationPointsMap { get; } = null;

        /// <summary>
        /// Gets the stream on which the count of out of order frames are posted.
        /// </summary>
#if DEBUG
        public Emitter<int> DebugOutOfOrderFrames { get; }
#else
        public Emitter<int> DebugOutOfOrderFrames { get; } = null; // DEBUG builds only
#endif

        /// <summary>
        /// Gets the research mode camera configuration.
        /// </summary>
        protected ResearchModeCameraConfiguration Configuration => this.configuration;

        /// <summary>
        /// Gets the rig node locator.
        /// </summary>
        protected SpatialLocator RigNodeLocator => this.rigNodeLocator;

        /// <summary>
        /// Calibrates the camera sensor.
        /// </summary>
        public void Calibrate()
        {
            if (this.pipelineIsRunning)
            {
                throw new InvalidOperationException($"The {nameof(this.Calibrate)}() method should only be called before the pipeline has started running.");
            }

            this.calibrationPointsMap = this.ComputeCalibrationPointsMap();
            this.cameraIntrinsics = this.calibrationPointsMap.ComputeCameraIntrinsics();
        }

        /// <summary>
        /// Calibrates the camera sensor, using stored files.
        /// </summary>
        /// <param name="calibrationFolder">The device folder containing calibration files. Defaults to Documents.</param>
        /// <returns>True when complete.</returns>
        /// <remarks>
        /// Camera intrinsics are loaded from the file: {calibrationFolder}/{ResearchModeSensorType}Intrinsics.xml,
        /// and the map of calibration points are loaded from the file: {calibrationFolder}/{ResearchModeSensorType}Points.map.
        /// Follow these steps to give your app read/write access to the Documents folder:
        /// https://docs.microsoft.com/en-us/uwp/api/windows.storage.knownfolders.documentslibrary?view=winrt-22000#prerequisites
        /// If the files do not already exist, they will be created, and calibration will be computed from scratch and saved.
        /// </remarks>
        public async Task<bool> CalibrateFromFileAsync(StorageFolder calibrationFolder = null)
        {
            if (this.pipelineIsRunning)
            {
                throw new InvalidOperationException($"The {nameof(this.CalibrateFromFileAsync)}() method should only be called before the pipeline has started running.");
            }

            // Use the Documents folder by default.
            calibrationFolder ??= KnownFolders.DocumentsLibrary;

            // Attempt to load from file
            var sensorType = this.cameraSensor.GetSensorType();
            string calibrationPointsMapFileName = $"{sensorType}Points.map";
            string cameraIntrinsicsFileName = $"{sensorType}Intrinsics.xml";

            try
            {
                // Get the file (throws FileNotFoundException if it doesn't exist, handled below).
                var calibrationPointsMapFile = await calibrationFolder.GetFileAsync(calibrationPointsMapFileName);
                this.calibrationPointsMap = await calibrationPointsMapFile.DeserializeCalibrationPointsMapAsync();
            }
            catch (FileNotFoundException)
            {
                // Create the file if it didn't exist.
                var calibrationPointsMapFile = await calibrationFolder.CreateFileAsync(calibrationPointsMapFileName, CreationCollisionOption.FailIfExists);

                // Compute and serialize
                this.calibrationPointsMap = this.ComputeCalibrationPointsMap();
                await this.calibrationPointsMap.SerializeAsync(calibrationPointsMapFile);
            }

            try
            {
                // Get the file (throws FileNotFoundException if it doesn't exist, handled below).
                var cameraIntrinsicsFile = await calibrationFolder.GetFileAsync(cameraIntrinsicsFileName);
                this.cameraIntrinsics = await cameraIntrinsicsFile.DeserializeCameraIntrinsicsAsync();
            }
            catch (FileNotFoundException)
            {
                // Create the file if it didn't exist.
                var cameraIntrinsicsFile = await calibrationFolder.CreateFileAsync(cameraIntrinsicsFileName, CreationCollisionOption.FailIfExists);

                // Compute and serialize
                this.cameraIntrinsics = this.calibrationPointsMap.ComputeCameraIntrinsics();
                await this.cameraIntrinsics.SerializeAsync(cameraIntrinsicsFile);
            }

            return true;
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            var consent = this.requestCameraAccessTask.Result;
            this.CheckConsentAndThrow(consent);

            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.captureThread = new Thread(this.CaptureThread);
            this.captureThread.Start();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.shutdown = true;
            this.captureThread.Join(5000);

            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes a sensor frame received from the sensor.
        /// </summary>
        /// <param name="sensorFrame">The sensor frame.</param>
        /// <param name="resolution">The resolution of the sensor frame.</param>
        /// <param name="frameTicks">The sensor frame ticks.</param>
        /// <param name="originatingTime">The originating time for the sensor frame.</param>
        protected abstract void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame, ResearchModeSensorResolution resolution, ulong frameTicks, DateTime originatingTime);

        /// <summary>
        /// Gets the camera intrinsics.
        /// </summary>
        /// <returns>The camera's intrinsics.</returns>
        protected CameraIntrinsics GetCameraIntrinsics() => this.cameraIntrinsics;

        /// <summary>
        /// Gets the calibration points map (used for computing intrinsics)).
        /// </summary>
        /// <returns>The calibration points map.</returns>
        protected CalibrationPointsMap GetCalibrationPointsMap() => this.calibrationPointsMap;

        /// <summary>
        /// Gets the camera pose.
        /// </summary>
        /// <returns>The camera's pose.</returns>
        protected CoordinateSystem GetCameraPose() => this.cameraPose;

        /// <summary>
        /// Converts the rig node location to the camera pose.
        /// </summary>
        /// <param name="rigNodeLocation">The rig node location.</param>
        /// <returns>The coordinate system representing the camera pose.</returns>
        protected CoordinateSystem ToCameraPose(SpatialLocation rigNodeLocation)
        {
            var q = rigNodeLocation.Orientation;
            var m = Matrix4x4.CreateFromQuaternion(q);
            var p = rigNodeLocation.Position;
            m.Translation = p;

            // Extrinsics of the camera relative to the rig node
            if (!this.invertedCameraExtrinsics.HasValue)
            {
                Matrix4x4.Invert(this.cameraSensor.GetCameraExtrinsicsMatrix(), out var invertedMatrix);
                this.invertedCameraExtrinsics = invertedMatrix;
            }

            // Transform the rig node location to camera pose in world coordinates
            var cameraPose = CameraBasis * this.invertedCameraExtrinsics.Value * m;

            // Convert to \psi basis
            return cameraPose.RebaseToMathNetCoordinateSystem();
        }

        private void CaptureThread()
        {
            // ResearchMode requires that OpenStream() and GetNextBuffer() are called from the same thread
            this.cameraSensor.OpenStream();

            try
            {
                // Compute the map of calibration points if we don't have it already, and we need
                // it, either b/c we need to compute camera intrinsics, or b/c we need to emit them.
                if (this.calibrationPointsMap is null && this.configuration.RequiresCalibrationPointsMap())
                {
                    this.calibrationPointsMap = this.ComputeCalibrationPointsMap();
                }

                if (this.cameraIntrinsics is null && this.configuration.RequiresCameraIntrinsics())
                {
                    this.cameraIntrinsics = this.calibrationPointsMap.ComputeCameraIntrinsics();
                }

                // Main capture loop
                while (!this.shutdown)
                {
                    var sensorFrame = this.cameraSensor.GetNextBuffer();
                    var frameTicks = sensorFrame.GetTimeStamp().HostTicks;
                    var resolution = sensorFrame.GetResolution();

                    int imageWidth = (int)resolution.Width;
                    int imageHeight = (int)resolution.Height;
                    var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks((long)frameTicks);

#if DEBUG
                    if (originatingTime <= this.previousFrameOriginatingTime)
                    {
                        System.Diagnostics.Trace.WriteLine($"Attempted to post out of order message with originating time {originatingTime.TimeOfDay} from {this.GetType().Name}");

                        // Post the total number of out-of-order frames received on the debug stream
                        this.DebugOutOfOrderFrames.Post(++this.outOfOrderFrameCount, originatingTime);

                        // Continue to the next frame
                        continue;
                    }

                    this.previousFrameOriginatingTime = originatingTime;
#endif

                    // Sensor-specific processing implemented by derived class
                    if (!this.shutdown)
                    {
                        // Post the map of calibration points (used for computing camera intrinsics) if requested
                        if (this.configuration.OutputCalibrationPointsMap &&
                            (originatingTime - this.CalibrationPointsMap.LastEnvelope.OriginatingTime) > this.configuration.OutputCalibrationPointsMapMinInterval)
                        {
                            this.CalibrationPointsMap.Post(this.GetCalibrationPointsMap(), originatingTime);
                        }

                        // Post the camera intrinsics if requested
                        if (this.configuration.OutputCameraIntrinsics &&
                            (originatingTime - this.CameraIntrinsics.LastEnvelope.OriginatingTime) > this.configuration.OutputMinInterval)
                        {
                            this.CameraIntrinsics.Post(this.cameraIntrinsics, originatingTime);
                        }

                        // compute the camera pose if needed
                        if (this.configuration.RequiresPose())
                        {
                            var timestamp = PerceptionTimestampHelper.FromSystemRelativeTargetTime(TimeSpan.FromTicks((long)frameTicks));
                            var rigNodeLocation = this.RigNodeLocator.TryLocateAtTimestamp(timestamp, MixedReality.WorldSpatialCoordinateSystem);

                            // The rig node may not always be locatable, so we need a null check
                            if (rigNodeLocation != null)
                            {
                                // Compute the camera pose from the rig node location
                                this.cameraPose = this.ToCameraPose(rigNodeLocation);
                            }
                        }

                        // Post the camera pose if requested
                        if (this.configuration.OutputPose &&
                            (originatingTime - this.Pose.LastEnvelope.OriginatingTime) > this.configuration.OutputMinInterval)
                        {
                            this.Pose.Post(this.cameraPose, originatingTime);
                        }

                        this.ProcessSensorFrame(sensorFrame, resolution, frameTicks, originatingTime);
                    }
                }
            }
            finally
            {
                this.cameraSensor.CloseStream();
            }
        }

        private void CheckConsentAndThrow(ResearchModeSensorConsent consent)
        {
            switch (consent)
            {
                case ResearchModeSensorConsent.Allowed:
                    return;
                case ResearchModeSensorConsent.DeniedBySystem:
                    throw new UnauthorizedAccessException("Access to the camera was denied by the system");
                case ResearchModeSensorConsent.DeniedByUser:
                    throw new UnauthorizedAccessException("Access to the camera was denied by the user");
                case ResearchModeSensorConsent.NotDeclaredByApp:
                    throw new UnauthorizedAccessException("Webcam capability was not declared in the app manifest");
                case ResearchModeSensorConsent.UserPromptRequired:
                    throw new UnauthorizedAccessException("Permission to access to the camera must be requested first");
            }
        }

        private CalibrationPointsMap ComputeCalibrationPointsMap()
        {
            int width, height;
            switch (this.cameraSensor.GetSensorType())
            {
                case ResearchModeSensorType.DepthLongThrow:
                    width = 320;
                    height = 288;
                    break;
                case ResearchModeSensorType.DepthAhat:
                    width = 512;
                    height = 512;
                    break;
                case ResearchModeSensorType.LeftFront:
                case ResearchModeSensorType.RightFront:
                case ResearchModeSensorType.LeftLeft:
                case ResearchModeSensorType.RightRight:
                    width = 640;
                    height = 480;
                    break;
                default:
                    throw new InvalidOperationException("Invalid research mode camera for computing calibration.");
            }

            // Compute a lookup table of calibration points
            double[] cameraUnitPlanePoints = new double[width * height * 2];

            int ci = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Check the return value for success (HRESULT == S_OK)
                    if (this.cameraSensor.MapImagePointToCameraUnitPlane(new Point(x + 0.5, y + 0.5), out var xy) == 0)
                    {
                        // Add the camera space mapping for the image pixel
                        cameraUnitPlanePoints[ci++] = xy.X;
                        cameraUnitPlanePoints[ci++] = xy.Y;
                    }
                    else
                    {
                        cameraUnitPlanePoints[ci++] = double.NaN;
                        cameraUnitPlanePoints[ci++] = double.NaN;
                    }
                }
            }

            return new CalibrationPointsMap()
            {
                Width = width,
                Height = height,
                CameraUnitPlanePoints = cameraUnitPlanePoints,
            };
        }
    }
}
