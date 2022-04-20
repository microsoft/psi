// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Image = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Component that captures core sensor streams (color, depth, IR, and IMU) from the Azure Kinect device.
    /// </summary>
    internal sealed class AzureKinectCore : ISourceComponent, IDisposable
    {
        private static readonly object CameraOpenLock = new object();
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly AzureKinectSensorConfiguration configuration;

        /// <summary>
        /// The underlying Azure Kinect device.
        /// </summary>
        private Device device = null;
        private Thread captureThread = null;
        private Thread imuSampleThread = null;
        private bool shutdown = false;

        private int colorImageWidth;
        private int colorImageHeight;
        private int depthImageWidth;
        private int depthImageHeight;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKinectCore"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="config">Configuration to use for the device.</param>
        /// <param name="name">An optional name for the component.</param>
        public AzureKinectCore(Pipeline pipeline, AzureKinectSensorConfiguration config = null, string name = nameof(AzureKinectCore))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.configuration = config ?? new AzureKinectSensorConfiguration();

            if (this.configuration.OutputColor)
            {
                if (this.configuration.ColorResolution == ColorResolution.Off)
                {
                    throw new ArgumentException("Invalid configuration: Cannot output color stream when color resolution is set to Off.");
                }

                if (this.configuration.ColorFormat != ImageFormat.ColorBGRA32)
                {
                    throw new NotImplementedException("Invalid configuration: Psi so far only supports BGRA32 pixel format for the AzureKinect color camera");
                }
            }

            if (this.configuration.OutputDepth)
            {
                if (this.configuration.DepthMode == DepthMode.Off)
                {
                    throw new ArgumentException("Invalid configuration: Cannot output depth stream when depth mode is set to Off.");
                }

                if (this.configuration.DepthMode == DepthMode.PassiveIR)
                {
                    throw new ArgumentException("Invalid configuration: Cannot output depth stream when depth mode is set to PassiveIR.");
                }
            }

            if (this.configuration.OutputInfrared && this.configuration.DepthMode == DepthMode.Off)
            {
                throw new ArgumentException("Invalid configuration: Cannot output IR stream when depth mode is set to Off. Try DepthMode=PassiveIR if the intent is to capture only IR.");
            }

            this.DepthImage = pipeline.CreateEmitter<Shared<DepthImage>>(this, nameof(this.DepthImage));
            this.InfraredImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.InfraredImage));
            this.ColorImage = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.ColorImage));
            this.Imu = pipeline.CreateEmitter<ImuSample>(this, nameof(this.Imu));
            this.DepthDeviceCalibrationInfo = pipeline.CreateEmitter<IDepthDeviceCalibrationInfo>(this, nameof(this.DepthDeviceCalibrationInfo));
            this.AzureKinectSensorCalibration = pipeline.CreateEmitter<Calibration>(this, nameof(this.AzureKinectSensorCalibration));
            this.FrameRate = pipeline.CreateEmitter<double>(this, nameof(this.FrameRate));
            this.Temperature = pipeline.CreateEmitter<float>(this, nameof(this.Temperature));
            this.DepthAndIRImages = pipeline.CreateEmitter<(Shared<DepthImage>, Shared<Image>)>(this, nameof(this.DepthAndIRImages));

            this.DetermineImageDimensions();
        }

        /// <summary>
        /// Gets the current image from the color camera.
        /// </summary>
        public Emitter<Shared<Image>> ColorImage { get; private set; }

        /// <summary>
        /// Gets the current infrared image.
        /// </summary>
        public Emitter<Shared<Image>> InfraredImage { get; private set; }

        /// <summary>
        /// Gets the current depth image.
        /// </summary>
        public Emitter<Shared<DepthImage>> DepthImage { get; private set; }

        /// <summary>
        /// Gets the current frames-per-second actually achieved.
        /// </summary>
        public Emitter<double> FrameRate { get; private set; }

        /// <summary>
        /// Gets the current IMU sample.
        /// </summary>
        public Emitter<ImuSample> Imu { get; private set; }

        /// <summary>
        /// Gets the Azure Kinect depth device calibration information as an <see cref="IDepthDeviceCalibrationInfo"/> object (see Microsoft.Psi.Calibration).
        /// </summary>
        public Emitter<IDepthDeviceCalibrationInfo> DepthDeviceCalibrationInfo { get; private set; }

        /// <summary>
        /// Gets the underlying device calibration (provided directly by Azure Kinect SDK and required by the body tracker).
        /// </summary>
        public Emitter<Calibration> AzureKinectSensorCalibration { get; private set; }

        /// <summary>
        /// Gets the Kinect's temperature in degrees Celsius.
        /// </summary>
        public Emitter<float> Temperature { get; private set; }

        /// <summary>
        /// Gets both the depth and IR images together (required by the body tracker).
        /// </summary>
        internal Emitter<(Shared<DepthImage> Depth, Shared<Image> IR)> DepthAndIRImages { get; private set; }

        /// <summary>
        /// Returns the number of Kinect for Azure devices available on the system.
        /// </summary>
        /// <returns>Number of available devices.</returns>
        public static int GetInstalledCount()
        {
            return Device.GetInstalledCount();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.device != null)
            {
                this.device.Dispose();
                this.device = null;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            // Prevent device open race condition.
            lock (CameraOpenLock)
            {
                this.device = Device.Open(this.configuration.DeviceIndex);
            }

            // check the synchronization arguments
            if (this.configuration.WiredSyncMode != WiredSyncMode.Standalone)
            {
                if (this.configuration.WiredSyncMode == WiredSyncMode.Master && !this.device.SyncOutJackConnected)
                {
                    throw new ArgumentException("Invalid configuration: Cannot set Sensor as Master if SyncOut Jack is not connected");
                }

                if (this.configuration.WiredSyncMode == WiredSyncMode.Subordinate && !this.device.SyncInJackConnected)
                {
                    throw new ArgumentException("Invalid configuration: Cannot set Sensor as Subordinate if SyncIn Jack is not connected");
                }
            }

            if (this.configuration.ExposureTime > TimeSpan.Zero)
            {
                // one tick is 100 nano seconds (0.1 microseconds). The exposure time is set in microseconds.
                this.device.SetColorControl(ColorControlCommand.ExposureTimeAbsolute, ColorControlMode.Manual, (int)(this.configuration.ExposureTime.Ticks / 10));
            }

            if (this.configuration.PowerlineFrequency != AzureKinectSensorConfiguration.PowerlineFrequencyTypes.Default)
            {
                this.device.SetColorControl(ColorControlCommand.PowerlineFrequency, ColorControlMode.Manual, (int)this.configuration.PowerlineFrequency);
            }

            this.device.StartCameras(new DeviceConfiguration()
            {
                ColorFormat = this.configuration.ColorFormat,
                ColorResolution = this.configuration.ColorResolution,
                DepthMode = this.configuration.DepthMode,
                CameraFPS = this.configuration.CameraFPS,
                SynchronizedImagesOnly = this.configuration.SynchronizedImagesOnly,
                WiredSyncMode = this.configuration.WiredSyncMode,
                SuboridinateDelayOffMaster = this.configuration.SuboridinateDelayOffMaster,
                DepthDelayOffColor = this.configuration.DepthDelayOffColor,
            });

            this.captureThread = new Thread(new ThreadStart(this.CaptureThreadProc));
            this.captureThread.Start();

            if (this.configuration.OutputImu)
            {
                this.device.StartImu();
                this.imuSampleThread = new Thread(new ThreadStart(this.ImuSampleThreadProc));
                this.imuSampleThread.Start();
            }
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.shutdown = true;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);
            if (this.captureThread != null && this.captureThread.Join(waitTime) != true)
            {
                this.captureThread.Abort();
            }

            this.device.StopCameras();

            if (this.configuration.OutputImu)
            {
                if (this.imuSampleThread != null && this.imuSampleThread.Join(waitTime) != true)
                {
                    this.imuSampleThread.Abort();
                }

                this.device.StopImu();
            }

            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void CaptureThreadProc()
        {
            if (this.configuration.ColorResolution == ColorResolution.Off &&
                this.configuration.DepthMode == DepthMode.Off)
            {
                return;
            }

            var colorImageFormat = PixelFormat.BGRA_32bpp;
            var infraredImageFormat = PixelFormat.Gray_16bpp;

            var calibrationPosted = false;

            Stopwatch sw = new Stopwatch();
            int frameCount = 0;
            sw.Start();

            while (this.device != null && !this.shutdown)
            {
                if (this.configuration.OutputCalibration && !calibrationPosted)
                {
                    // Compute and post the device's calibration object.
                    var currentTime = this.pipeline.GetCurrentTime();
                    var calibration = this.device.GetCalibration();

                    if (calibration != null)
                    {
                        this.AzureKinectSensorCalibration.Post(calibration, currentTime);

                        var colorExtrinsics = calibration.ColorCameraCalibration.Extrinsics;
                        var colorIntrinsics = calibration.ColorCameraCalibration.Intrinsics;
                        var depthIntrinsics = calibration.DepthCameraCalibration.Intrinsics;

                        if (colorIntrinsics.Type == CalibrationModelType.Rational6KT || depthIntrinsics.Type == CalibrationModelType.Rational6KT)
                        {
                            throw new Exception("Calibration output not permitted for deprecated internal Azure Kinect cameras. Only Brown_Conrady calibration supported.");
                        }
                        else if (colorIntrinsics.Type != CalibrationModelType.BrownConrady || depthIntrinsics.Type != CalibrationModelType.BrownConrady)
                        {
                            throw new Exception("Calibration output only supported for Brown_Conrady model.");
                        }
                        else
                        {
                            Matrix<double> colorCameraMatrix = Matrix<double>.Build.Dense(3, 3);
                            colorCameraMatrix[0, 0] = colorIntrinsics.Parameters[2];
                            colorCameraMatrix[1, 1] = colorIntrinsics.Parameters[3];
                            colorCameraMatrix[0, 2] = colorIntrinsics.Parameters[0];
                            colorCameraMatrix[1, 2] = colorIntrinsics.Parameters[1];
                            colorCameraMatrix[2, 2] = 1;
                            Matrix<double> depthCameraMatrix = Matrix<double>.Build.Dense(3, 3);
                            depthCameraMatrix[0, 0] = depthIntrinsics.Parameters[2];
                            depthCameraMatrix[1, 1] = depthIntrinsics.Parameters[3];
                            depthCameraMatrix[0, 2] = depthIntrinsics.Parameters[0];
                            depthCameraMatrix[1, 2] = depthIntrinsics.Parameters[1];
                            depthCameraMatrix[2, 2] = 1;
                            Matrix<double> depthToColorMatrix = Matrix<double>.Build.Dense(4, 4);
                            for (int i = 0; i < 3; i++)
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    // The AzureKinect SDK assumes that vectors are row vectors, while the MathNet SDK assumes
                                    // column vectors, so we need to flip them here.
                                    depthToColorMatrix[i, j] = colorExtrinsics.Rotation[(j * 3) + i];
                                }
                            }

                            depthToColorMatrix[3, 0] = colorExtrinsics.Translation[0];
                            depthToColorMatrix[3, 1] = colorExtrinsics.Translation[1];
                            depthToColorMatrix[3, 2] = colorExtrinsics.Translation[2];
                            depthToColorMatrix[3, 3] = 1.0;
                            var metersToMillimeters = Matrix<double>.Build.Dense(4, 4);
                            metersToMillimeters[0, 0] = 1000.0;
                            metersToMillimeters[1, 1] = 1000.0;
                            metersToMillimeters[2, 2] = 1000.0;
                            metersToMillimeters[3, 3] = 1.0;
                            var millimetersToMeters = Matrix<double>.Build.Dense(4, 4);
                            millimetersToMeters[0, 0] = 1.0 / 1000.0;
                            millimetersToMeters[1, 1] = 1.0 / 1000.0;
                            millimetersToMeters[2, 2] = 1.0 / 1000.0;
                            millimetersToMeters[3, 3] = 1.0;
                            depthToColorMatrix = (metersToMillimeters * depthToColorMatrix * millimetersToMeters).Transpose();

                            double[] colorRadialDistortion = new double[6]
                            {
                                colorIntrinsics.Parameters[4],
                                colorIntrinsics.Parameters[5],
                                colorIntrinsics.Parameters[6],
                                colorIntrinsics.Parameters[7],
                                colorIntrinsics.Parameters[8],
                                colorIntrinsics.Parameters[9],
                            };
                            double[] colorTangentialDistortion = new double[2] { colorIntrinsics.Parameters[13], colorIntrinsics.Parameters[12] };
                            double[] depthRadialDistortion = new double[6]
                            {
                                depthIntrinsics.Parameters[4],
                                depthIntrinsics.Parameters[5],
                                depthIntrinsics.Parameters[6],
                                depthIntrinsics.Parameters[7],
                                depthIntrinsics.Parameters[8],
                                depthIntrinsics.Parameters[9],
                            };
                            double[] depthTangentialDistortion = new double[2] { depthIntrinsics.Parameters[13], depthIntrinsics.Parameters[12] };

                            // Azure Kinect uses a basis under the hood that assumes Forward=Z, Right=X, Down=Y.
                            var kinectBasis = new CoordinateSystem(default, UnitVector3D.ZAxis, UnitVector3D.XAxis.Negate(), UnitVector3D.YAxis.Negate());

                            var cameraCalibration = new DepthDeviceCalibrationInfo(
                                calibration.ColorCameraCalibration.ResolutionWidth,
                                calibration.ColorCameraCalibration.ResolutionHeight,
                                colorCameraMatrix,
                                colorRadialDistortion,
                                colorTangentialDistortion,
                                kinectBasis.Invert() * depthToColorMatrix * kinectBasis,
                                calibration.DepthCameraCalibration.ResolutionWidth,
                                calibration.DepthCameraCalibration.ResolutionHeight,
                                depthCameraMatrix,
                                depthRadialDistortion,
                                depthTangentialDistortion,
                                CoordinateSystem.CreateIdentity(4));

                            this.DepthDeviceCalibrationInfo.Post(cameraCalibration, currentTime);
                        }

                        calibrationPosted = true;
                    }
                }

                // Wait for a capture on a thread pool thread
                using var capture = this.device.GetCapture(this.configuration.DeviceCaptureTimeout);
                if (capture != null)
                {
                    var currentTime = this.pipeline.GetCurrentTime();

                    if (this.configuration.OutputColor && capture.Color != null)
                    {
                        using var sharedColorImage = ImagePool.GetOrCreate(this.colorImageWidth, this.colorImageHeight, colorImageFormat);
                        sharedColorImage.Resource.CopyFrom(capture.Color.Memory.ToArray());
                        this.ColorImage.Post(sharedColorImage, currentTime);
                    }

                    Shared<Image> sharedIRImage = null;
                    Shared<DepthImage> sharedDepthImage = null;

                    if (this.configuration.OutputInfrared && capture.IR != null)
                    {
                        sharedIRImage = ImagePool.GetOrCreate(this.depthImageWidth, this.depthImageHeight, infraredImageFormat);
                        sharedIRImage.Resource.CopyFrom(capture.IR.Memory.ToArray());
                        this.InfraredImage.Post(sharedIRImage, currentTime);
                    }

                    if (this.configuration.OutputDepth && capture.Depth != null)
                    {
                        sharedDepthImage = DepthImagePool.GetOrCreate(
                            this.depthImageWidth,
                            this.depthImageHeight,
                            DepthValueSemantics.DistanceToPlane,
                            0.001);
                        sharedDepthImage.Resource.CopyFrom(capture.Depth.Memory.ToArray());
                        this.DepthImage.Post(sharedDepthImage, currentTime);

                        if (sharedIRImage != null)
                        {
                            this.DepthAndIRImages.Post((sharedDepthImage, sharedIRImage), currentTime);
                        }
                    }

                    sharedIRImage?.Dispose();
                    sharedDepthImage?.Dispose();

                    this.Temperature.Post(capture.Temperature, currentTime);

                    ++frameCount;
                    if (sw.Elapsed > this.configuration.FrameRateReportingFrequency)
                    {
                        this.FrameRate.Post((double)frameCount / sw.Elapsed.TotalSeconds, currentTime);
                        frameCount = 0;
                        sw.Restart();
                    }
                }
            }
        }

        private void ImuSampleThreadProc()
        {
            while (this.device != null && !this.shutdown)
            {
                this.Imu.Post(this.device.GetImuSample(TimeSpan.MaxValue), this.pipeline.GetCurrentTime());
            }
        }

        private void DetermineImageDimensions()
        {
            // Initialize the color image width and height based on config
            switch (this.configuration.ColorResolution)
            {
                case ColorResolution.R720p:
                    this.colorImageWidth = 1280;
                    this.colorImageHeight = 720;
                    break;
                case ColorResolution.R1080p:
                    this.colorImageWidth = 1920;
                    this.colorImageHeight = 1080;
                    break;
                case ColorResolution.R1440p:
                    this.colorImageWidth = 2560;
                    this.colorImageHeight = 1440;
                    break;
                case ColorResolution.R1536p:
                    this.colorImageWidth = 2048;
                    this.colorImageHeight = 1536;
                    break;
                case ColorResolution.R2160p:
                    this.colorImageWidth = 3840;
                    this.colorImageHeight = 2160;
                    break;
                case ColorResolution.R3072p:
                    this.colorImageWidth = 4096;
                    this.colorImageHeight = 3072;
                    break;
                case ColorResolution.Off:
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected Azure Kinect Color Resolution: {this.configuration.ColorResolution}");
            }

            // Initialize the depth image width and height based on config
            switch (this.configuration.DepthMode)
            {
                case DepthMode.NFOV_2x2Binned:
                    this.depthImageWidth = 320;
                    this.depthImageHeight = 288;
                    break;
                case DepthMode.NFOV_Unbinned:
                    this.depthImageWidth = 640;
                    this.depthImageHeight = 576;
                    break;
                case DepthMode.WFOV_2x2Binned:
                    this.depthImageWidth = 512;
                    this.depthImageHeight = 512;
                    break;
                case DepthMode.WFOV_Unbinned:
                case DepthMode.PassiveIR:
                    this.depthImageWidth = 1024;
                    this.depthImageHeight = 1024;
                    break;
                case DepthMode.Off:
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected Azure Kinect Depth Mode: {this.configuration.DepthMode}");
            }
        }
    }
}