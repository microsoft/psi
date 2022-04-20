// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.DeviceManagement;
    using DepthImage = Microsoft.Psi.Imaging.DepthImage;
    using Image = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Component that captures all sensor streams and tracked bodies from the Azure Kinect device.
    /// </summary>
    public class AzureKinectSensor : Subpipeline
    {
        private static List<CameraDeviceInfo> allDevices = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKinectSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Configuration to use for the sensor.</param>
        /// <param name="defaultDeliveryPolicy">An optional default delivery policy for the subpipeline (defaults is LatestMessage).</param>
        /// <param name="bodyTrackerDeliveryPolicy">An optional delivery policy for sending the depth-and-IR images stream to the body tracker (default is LatestMessage).</param>
        /// <param name="name">An optional name for the component.</param>
        public AzureKinectSensor(
            Pipeline pipeline,
            AzureKinectSensorConfiguration configuration = null,
            DeliveryPolicy defaultDeliveryPolicy = null,
            DeliveryPolicy bodyTrackerDeliveryPolicy = null,
            string name = nameof(AzureKinectSensor))
            : base(pipeline, name, defaultDeliveryPolicy ?? DeliveryPolicy.LatestMessage)
        {
            this.Configuration = configuration ?? new AzureKinectSensorConfiguration();

            if (this.Configuration.BodyTrackerConfiguration != null)
            {
                if (!configuration.OutputCalibration)
                {
                    throw new Exception($"The body tracker requires that the {nameof(AzureKinectSensor)} component must be configured to output calibration.");
                }

                if (!this.Configuration.OutputInfrared || !this.Configuration.OutputDepth)
                {
                    throw new Exception($"The body tracker requires that the {nameof(AzureKinectSensor)} component must be configured to output both Depth and IR streams.");
                }
            }

            var azureKinectCore = new AzureKinectCore(this, this.Configuration);

            // Connect the sensor streams
            this.ColorImage = azureKinectCore.ColorImage.BridgeTo(pipeline, nameof(this.ColorImage)).Out;
            this.Imu = azureKinectCore.Imu.BridgeTo(pipeline, nameof(this.Imu)).Out;
            this.DepthImage = azureKinectCore.DepthImage.BridgeTo(pipeline, nameof(this.DepthImage)).Out;
            this.InfraredImage = azureKinectCore.InfraredImage.BridgeTo(pipeline, nameof(this.InfraredImage)).Out;
            this.FrameRate = azureKinectCore.FrameRate.BridgeTo(pipeline, nameof(this.FrameRate)).Out;
            this.Temperature = azureKinectCore.Temperature.BridgeTo(pipeline, nameof(this.Temperature)).Out;
            this.DepthDeviceCalibrationInfo = azureKinectCore.DepthDeviceCalibrationInfo.BridgeTo(pipeline, nameof(this.DepthDeviceCalibrationInfo)).Out;
            this.AzureKinectSensorCalibration = azureKinectCore.AzureKinectSensorCalibration.BridgeTo(pipeline, nameof(this.AzureKinectSensorCalibration)).Out;

            // Pipe captures and calibration to the body tracker
            if (this.Configuration.BodyTrackerConfiguration != null)
            {
                var bodyTracker = new AzureKinectBodyTracker(this, this.Configuration.BodyTrackerConfiguration);
                azureKinectCore.DepthAndIRImages.PipeTo(bodyTracker, bodyTrackerDeliveryPolicy ?? DeliveryPolicy.LatestMessage);
                azureKinectCore.AzureKinectSensorCalibration.PipeTo(bodyTracker.AzureKinectSensorCalibration, DeliveryPolicy.Unlimited);
                this.Bodies = bodyTracker.BridgeTo(pipeline, nameof(this.Bodies)).Out;
            }
            else
            {
                // create unused emitter to allow wiring while OutputBodies=false
                this.Bodies = pipeline.CreateEmitter<List<AzureKinectBody>>(this, nameof(this.Bodies));
            }
        }

        /// <summary>
        /// Gets a list of all available capture devices.
        /// </summary>
        public static IEnumerable<CameraDeviceInfo> AllDevices
        {
            get
            {
                if (allDevices == null)
                {
                    allDevices = new List<CameraDeviceInfo>();
                    int numDevices = Device.GetInstalledCount();
                    for (int i = 0; i < numDevices; i++)
                    {
                        var di = new CameraDeviceInfo
                        {
                            FriendlyName = $"AzureKinect-{i}",
                            DeviceName = $"AzureKinect-{i}",
                            DeviceType = "AzureKinect",
                            DeviceId = i,
                        };
                        Device dev;
                        try
                        {
                            dev = Device.Open(i);
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        di.SerialNumber = dev.SerialNum;
                        dev.Dispose();
                        di.Sensors = new List<CameraDeviceInfo.Sensor>();
                        for (int k = 0; k < 3; k++)
                        {
                            var sensor = new CameraDeviceInfo.Sensor();
                            uint[,] resolutions = null;
                            switch (k)
                            {
                                case 0: // color mode
                                    sensor.Type = CameraDeviceInfo.Sensor.SensorType.Color;
                                    resolutions = new uint[,]
                                    {
                                        { 1280, 720, 1 },
                                        { 1920, 1080, 1 },
                                        { 2560, 1440, 1 },
                                        { 3840, 2160, 1 },
                                        { 2048, 1536, 1 },
                                        { 4096, 3072, 0 },
                                    };
                                    break;
                                case 1: // depth mode
                                    sensor.Type = CameraDeviceInfo.Sensor.SensorType.Depth;
                                    resolutions = new uint[,]
                                    {
                                        { 640, 576, 1 },
                                        { 320, 288, 1 },
                                        { 512, 512, 1 },
                                        { 1024, 1024, 0 },
                                    };
                                    break;
                                case 2: // IR mode
                                    sensor.Type = CameraDeviceInfo.Sensor.SensorType.IR;
                                    resolutions = new uint[,]
                                    {
                                        { 1024, 1024, 1 },
                                    };
                                    break;
                            }

                            sensor.Modes = new List<CameraDeviceInfo.Sensor.ModeInfo>();
                            uint[] frameRates = { 30, 15, 5 };
                            for (int j = 0; j < resolutions.Length / 3; j++)
                            {
                                foreach (var fr in frameRates)
                                {
                                    if (fr == 30 && resolutions[j, 2] == 0)
                                    {
                                        continue; // Mode doesn't support 30fps
                                    }

                                    var mi = new CameraDeviceInfo.Sensor.ModeInfo
                                    {
                                        Format = Imaging.PixelFormat.BGRA_32bpp,
                                        FrameRateNumerator = fr,
                                        FrameRateDenominator = 1,
                                        ResolutionWidth = resolutions[j, 0],
                                        ResolutionHeight = resolutions[j, 1],
                                    };
                                    sensor.Modes.Add(mi);
                                }
                            }

                            di.Sensors.Add(sensor);
                        }

                        allDevices.Add(di);
                    }
                }

                return allDevices;
            }
        }

        // Note: the following emitters mirror those in AzureKinectSensorCore

        /// <summary>
        /// Gets the sensor configuration.
        /// </summary>
        public AzureKinectSensorConfiguration Configuration { get; } = null;

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
        /// Gets the Azure Kinect's depth device calibration information.
        /// </summary>
        public Emitter<IDepthDeviceCalibrationInfo> DepthDeviceCalibrationInfo { get; private set; }

        /// <summary>
        /// Gets the underlying device calibration (provided by Azure Kinect SDK).
        /// </summary>
        public Emitter<Calibration> AzureKinectSensorCalibration { get; private set; }

        /// <summary>
        /// Gets the Kinect's temperature in degrees Celsius.
        /// </summary>
        public Emitter<float> Temperature { get; private set; }

        // Note: the following emitter mirrors that in AzureKinectBodyTracker

        /// <summary>
        /// Gets the emitter of lists of currently tracked bodies.
        /// </summary>
        public Emitter<List<AzureKinectBody>> Bodies { get; private set; }

        /// <summary>
        /// Returns the number of Kinect for Azure devices available on the system.
        /// </summary>
        /// <returns>Number of available devices.</returns>
        public static int GetInstalledCount()
        {
            return Device.GetInstalledCount();
        }
    }
}