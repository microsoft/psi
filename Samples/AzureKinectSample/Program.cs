// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace AzureKinectSample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.AzureKinect;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Azure Kinect sample program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            SingleSensor();
        }

        /// <summary>
        /// Main Example using a Single Azure Sensor.
        /// </summary>
        public static void SingleSensor()
        {
            // camera resolution settings
            const ColorResolution resolution = ColorResolution.R720p;
            const int widthSource = 1280;
            const int heightSource = 720;

            // down sampled resolution
            const int widthOutput = 80;
            const int heightOutput = 45;
            const double scaleFactorWidth = (double)widthOutput / widthSource;
            const double scaleFactorHeight = (double)heightOutput / heightSource;

            // background subtraction beyond this depth
            const double maxDepth = 1.0; // meters

            const SensorOrientation initialOrientation = SensorOrientation.Default;

            using (var pipeline = Pipeline.Create("AzureKinectSample", DeliveryPolicy.LatestMessage))
            {
                var azureKinect = new AzureKinectSensor(
                    pipeline,
                    new AzureKinectSensorConfiguration()
                    {
                        OutputImu = true,
                        ColorResolution = resolution,
                        DepthMode = DepthMode.WFOV_Unbinned,
                        CameraFPS = FPS.FPS15,
                        BodyTrackerConfiguration = new AzureKinectBodyTrackerConfiguration()
                        {
                            CpuOnlyMode = true, // false if CUDA supported GPU available
                            SensorOrientation = initialOrientation,
                        },
                    });

                StringBuilder sb = new StringBuilder();
                SensorOrientation lastOrientation = (SensorOrientation)(-1); // detect orientation changes

                // consuming color, depth, IMU, body tracking, calibration
                azureKinect.ColorImage.Resize(widthOutput, heightOutput)
                    .Join(azureKinect.DepthImage)
                    .Join(azureKinect.Imu, TimeSpan.FromMilliseconds(10))
                    .Pair(azureKinect.Bodies)
                    .Pair(azureKinect.DepthDeviceCalibrationInfo)
                    .Do(message =>
                {
                    var (color, depth, imu, bodies, calib) = message;

                    // determine camera orientation from IMU
                    static SensorOrientation ImuOrientation(ImuSample imu)
                    {
                        const double halfGravity = 9.8 / 2;
                        return
                            (imu.AccelerometerSample.Z > halfGravity) ? SensorOrientation.Flip180 :
                            (imu.AccelerometerSample.Y > halfGravity) ? SensorOrientation.Clockwise90 :
                            (imu.AccelerometerSample.Y < -halfGravity) ? SensorOrientation.CounterClockwise90 :
                            SensorOrientation.Default; // upright
                    }

                    // enumerate image coordinates while correcting for orientation
                    static (IEnumerable<int>, IEnumerable<int>, bool) EnumerateCoordinates(SensorOrientation orientation)
                    {
                        var w = Enumerable.Range(0, widthOutput);
                        var h = Enumerable.Range(0, heightOutput);
                        return orientation switch
                        {
                            SensorOrientation.Clockwise90 => (h.Reverse(), w, true),
                            SensorOrientation.Flip180 => (w.Reverse(), h.Reverse(), false),
                            SensorOrientation.CounterClockwise90 => (h, w.Reverse(), true),
                            _ => (w, h, false), // normal
                        };
                    }

                    // render color frame as "ASCII art"
                    sb.Clear();
                    var bitmap = color.Resource.ToBitmap();
                    var orientation = ImuOrientation(imu);
                    var (horizontal, vertical, swap) = EnumerateCoordinates(orientation);
                    foreach (var j in vertical.Where(n => n % 2 == 0))
                    {
                        foreach (var i in horizontal)
                        {
                            var (x, y) = swap ? (j, i) : (i, j);

                            // subtract background beyond max depth
                            var d = DepthExtensions.ProjectToCameraSpace(calib, new Point2D(x / scaleFactorWidth, y / scaleFactorHeight), depth);
                            if (!d.HasValue || d.Value.Z < maxDepth)
                            {
                                var p = bitmap.GetPixel(x, y);
                                sb.Append(" .:-=+*#%@"[(int)((p.R + p.G + p.B) / 76.5)]);
                            }
                            else
                            {
                                sb.Append(' '); // subtract background
                            }
                        }

                        sb.Append(Environment.NewLine);
                    }

                    // clear console when orientation changes
                    if (orientation != lastOrientation)
                    {
                        Console.Clear();
                        lastOrientation = orientation;
                    }

                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine(sb.ToString());

                    // overlay head tracking
                    if (orientation == initialOrientation)
                    {
                        // body tracking works only in initially configured orientation
                        Console.BackgroundColor = ConsoleColor.Red;
                        foreach (var body in bodies)
                        {
                            var p = calib.ToColorSpace(body.Joints[JointId.Head].Pose.Origin);
                            var x = (int)(p.X * scaleFactorWidth);
                            var y = (int)(p.Y * scaleFactorHeight / 2);
                            if (x > 0 && x < widthOutput && y > 0 && y < heightOutput)
                            {
                                Console.SetCursorPosition(x, y / 2);
                                Console.Write(' ');
                            }
                        }

                        Console.BackgroundColor = ConsoleColor.Black;
                    }
                });

                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                pipeline.RunAsync();
                Console.ReadLine(); // press Enter to end
            }
        }

        /// <summary>
        /// Example of Wire Syncing multiple Azure Kinect.
        /// </summary>
        public static void AzureKinectSync()
        {
            using (var p = Pipeline.Create())
            {
                // The minimum time between depth sensors accroding to AzureKinect Samples.
                // https://github.com/microsoft/Azure-Kinect-Sensor-SDK/blob/develop/examples/green_screen/main.cpp
                var minTimeBetweenDepthImageSensors = TimeSpan.FromTicks(1600);

                var sensor1 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    DeviceIndex = 0,
                    WiredSyncMode = WiredSyncMode.Master,
                    PowerlineFrequency = AzureKinectSensorConfiguration.PowerlineFrequencyTypes.SixtyHz,
                    Exposure = TimeSpan.FromTicks(80000),
                    DepthDelayOffColor = minTimeBetweenDepthImageSensors / 2,
                });

                var sensor2 = new AzureKinectSensor(p, new AzureKinectSensorConfiguration()
                {
                    DeviceIndex = 1,
                    WiredSyncMode = WiredSyncMode.Subordinate,
                    PowerlineFrequency = AzureKinectSensorConfiguration.PowerlineFrequencyTypes.SixtyHz,
                    Exposure = TimeSpan.FromTicks(80000),
                    DepthDelayOffColor = -1 * (minTimeBetweenDepthImageSensors / 2),
                });

                // Check if the difference between Color Images from sycned sensors is less than 1 milliseconds.
                // Even thought it is synced, originating time might be different due to processing speed/USB speed, etc.
                sensor1.ColorImage.Join(sensor2.ColorImage, TimeSpan.FromMilliseconds(1)).Do(m =>
                {
                    Console.WriteLine("Color Image Streams Synced up to 1ms difference");
                });

                p.RunAsync();
                Console.ReadLine(); // Press Enter to exit.
            }
        }
    }
}
