// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Threading;
    using System.Threading.Tasks;
    using HoloLens2ResearchMode;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;
    using Windows.Foundation;
    using Windows.Perception.Spatial;
    using Windows.Perception.Spatial.Preview;

    /// <summary>
    /// Represents an abstract base class for a HoloLens 2 research mode camera component.
    /// </summary>
    public abstract class ResearchModeCamera : ISourceComponent
    {
        // Camera coordinate system (x - right, y - down, z - forward) relative
        // to the HoloLens coordinate system (x - right, y - up, z - back)
        private static readonly CoordinateSystem CameraCoordinateSystem =
            new (default, UnitVector3D.XAxis, UnitVector3D.YAxis.Negate(), UnitVector3D.ZAxis.Negate());

        private readonly Pipeline pipeline;
        private readonly ResearchModeSensorDevice sensorDevice;
        private readonly ResearchModeCameraSensor cameraSensor;
        private readonly Task<ResearchModeSensorConsent> requestCameraAccessTask;
        private readonly SpatialLocator rigNodeLocator;
        private readonly bool createCalibrationMap;
        private readonly bool computeCameraIntrinsics;

        private CalibrationPointsMap calibrationPointsMap;
        private ICameraIntrinsics cameraIntrinsics;
        private CoordinateSystem cameraExtrinsics;
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
        /// <param name="sensorType">The research mode sensor type.</param>
        /// <param name="createCalibrationMap">A value indicating whether to create a map of calibration points (needed to compute intrinsics).</param>
        /// <param name="computeCameraIntrinsics">A value indicating whether to compute camera intrinsics.</param>
        public ResearchModeCamera(Pipeline pipeline, ResearchModeSensorType sensorType, bool createCalibrationMap = true, bool computeCameraIntrinsics = true)
        {
            this.pipeline = pipeline;
            this.createCalibrationMap = createCalibrationMap;
            this.computeCameraIntrinsics = computeCameraIntrinsics;

            this.Pose = pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.Pose));

            if (this.computeCameraIntrinsics)
            {
                this.CameraIntrinsics = pipeline.CreateEmitter<ICameraIntrinsics>(this, nameof(this.CameraIntrinsics));
            }

            if (this.createCalibrationMap)
            {
                this.CalibrationPointsMap = pipeline.CreateEmitter<CalibrationPointsMap>(this, nameof(this.CalibrationPointsMap));
            }

            this.sensorDevice = new ResearchModeSensorDevice();
            this.requestCameraAccessTask = this.sensorDevice.RequestCameraAccessAsync().AsTask();
            this.cameraSensor = (ResearchModeCameraSensor)this.sensorDevice.GetSensor(sensorType);

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
        public Emitter<ICameraIntrinsics> CameraIntrinsics { get; }

        /// <summary>
        /// Gets the stream for calibration map (image points and corresponding 3D camera points).
        /// </summary>
        public Emitter<CalibrationPointsMap> CalibrationPointsMap { get; }

        /// <summary>
        /// Gets the stream on which the count of out of order frames are posted.
        /// </summary>
#if DEBUG
        public Emitter<int> DebugOutOfOrderFrames { get; }
#else
        public Emitter<int> DebugOutOfOrderFrames { get; } = null; // DEBUG builds only
#endif

        /// <summary>
        /// Gets the rig node locator.
        /// </summary>
        protected SpatialLocator RigNodeLocator => this.rigNodeLocator;

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
        protected ICameraIntrinsics GetCameraIntrinsics() => this.cameraIntrinsics;

        /// <summary>
        /// Gets the calibration points map (used for computing intrinsics)).
        /// </summary>
        /// <returns>The calibration points map.</returns>
        protected CalibrationPointsMap GetCalibrationPointsMap() => this.calibrationPointsMap;

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
            this.cameraExtrinsics ??= new CoordinateSystem(this.cameraSensor.GetCameraExtrinsicsMatrix().ToMathNetMatrix());

            // Transform the rig node location to camera pose in world coordinates
            var cameraPose = m.ToMathNetMatrix() * this.cameraExtrinsics.Invert() * CameraCoordinateSystem;

            // Convert to \psi basis
            return new CoordinateSystem(cameraPose.ChangeBasisHoloLensToPsi());
        }

        private void CaptureThread()
        {
            // ResearchMode requires that OpenStream() and GetNextBuffer() are called from the same thread
            this.cameraSensor.OpenStream();

            try
            {
                if (this.createCalibrationMap || this.computeCameraIntrinsics)
                {
                    // Get the resolution from the initial frame. We could also just have used constants
                    // based on the sensor type, but this approach keeps things more general/flexible.
                    var sensorFrame = this.cameraSensor.GetNextBuffer();
                    var resolution = sensorFrame.GetResolution();
                    var width = (int)resolution.Width;
                    var height = (int)resolution.Height;

                    // Compute a lookup table of calibration points
                    List<Point3D> cameraPoints = new ();
                    List<Point2D> imagePoints = new ();
                    float[] cameraUnitPlanePoints = new float[width * height * 2];

                    int ci = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            // Check the return value for success (HRESULT == S_OK)
                            if (this.cameraSensor.MapImagePointToCameraUnitPlane(new Point(x + 0.5, y + 0.5), out var xy) == 0)
                            {
                                // Add the camera space mapping for the image pixel
                                cameraUnitPlanePoints[ci++] = (float)xy.X;
                                cameraUnitPlanePoints[ci++] = (float)xy.Y;

                                var norm = Math.Sqrt((xy.X * xy.X) + (xy.Y * xy.Y) + 1.0);
                                imagePoints.Add(new Point2D(x + 0.5, y + 0.5));
                                cameraPoints.Add(new Point3D(xy.X / norm, xy.Y / norm, 1.0 / norm));
                            }
                            else
                            {
                                cameraUnitPlanePoints[ci++] = float.NaN;
                                cameraUnitPlanePoints[ci++] = float.NaN;
                            }
                        }
                    }

                    this.calibrationPointsMap = new CalibrationPointsMap(width, height, cameraUnitPlanePoints);

                    if (this.computeCameraIntrinsics)
                    {
                        // Compute instrinsics before the main loop as it could take a while. This avoids a long
                        // observed initial delay for the first posted frame while intrinsics are being computed.
                        this.cameraIntrinsics = this.ComputeCameraIntrinsics(width, height, cameraPoints, imagePoints);
                    }
                }

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
                    throw new UnauthorizedAccessException("Camera capability was not declared in the app manifest");
                case ResearchModeSensorConsent.UserPromptRequired:
                    throw new UnauthorizedAccessException("Permission to access to the camera must be requested first");
            }
        }

        /// <summary>
        /// Computes the camera intrinsics from a lookup table mapping image points to 3D points in camera space.
        /// </summary>
        /// <param name="width">The image width for the camera.</param>
        /// <param name="height">The image height for the camera.</param>
        /// <param name="cameraPoints">The list of 3D camera points to use for calibration.</param>
        /// <param name="imagePoints">The list of corresponding 2D image points.</param>
        /// <returns>The camera intrinsics.</returns>
        private ICameraIntrinsics ComputeCameraIntrinsics(int width, int height, List<Point3D> cameraPoints, List<Point2D> imagePoints)
        {
            // Initialize a starting camera matrix
            var initialCameraMatrix = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.Dense(3, 3);
            var initialDistortion = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(2);
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

            return new CameraIntrinsics(width, height, computedCameraMatrix, computedDistortionCoefficients, depthPixelSemantics: DepthPixelSemantics.DistanceToPoint);
        }
    }
}
