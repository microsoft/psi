// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

namespace Microsoft.Psi.DeviceManagement
{
    using System.Collections.Generic;

    /// <summary>
    /// Information about a camera device.
    /// </summary>
    public class CameraDeviceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CameraDeviceInfo"/> class.
        /// </summary>
        public CameraDeviceInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraDeviceInfo"/> class.
        /// </summary>
        /// <param name="friendlyName">Human readable name for this device.</param>
        /// <param name="deviceName">Name for this device.</param>
        /// <param name="serialNumber">Serial number for this device (maybe empty string).</param>
        public CameraDeviceInfo(string friendlyName, string deviceName, string serialNumber)
        {
            this.FriendlyName = friendlyName;
            this.DeviceName = deviceName;
            this.SerialNumber = serialNumber;
        }

        /// <summary>
        /// Gets or sets the device type.
        /// </summary>
        public string DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the human readable name for this device.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the name for this device.
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the serial number for this device. Maybe empty string.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the list of available sensors for this device.
        /// </summary>
        public List<Sensor> Sensors { get; set; }

        /// <summary>
        /// Defines a sensor that is part of this camera.
        /// </summary>
        public class Sensor
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CameraDeviceInfo.Sensor"/> class.
            /// </summary>
            public Sensor()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CameraDeviceInfo.Sensor"/> class.
            /// </summary>
            /// <param name="type">Type of sensor.</param>
            public Sensor(SensorType type)
            {
                this.Type = type;
            }

            /// <summary>
            /// Defines type of sensors available.
            /// </summary>
            public enum SensorType
            {
                /// <summary>
                /// Used for sensors that are of type RGB.
                /// </summary>
                Color,

                /// <summary>
                /// Used for sensors that are depth cameras.
                /// </summary>
                Depth,

                /// <summary>
                /// Used for sensors that are of type IR.
                /// </summary>
                IR,

                /// <summary>
                /// Used for sensors that are of type IR and the left sensor.
                /// </summary>
                LeftIR,

                /// <summary>
                /// Used for sensors that are of type IR and the right sensor.
                /// </summary>
                RightIR,
            }

            /// <summary>
            /// Gets or sets the type of sensor.
            /// </summary>
            public SensorType Type { get; set; }

            /// <summary>
            /// Gets or sets the list of available modes this sensor supports.
            /// </summary>
            public List<ModeInfo> Modes { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="CameraDeviceInfo.Sensor.ModeInfo"/> class.
            /// </summary>
            public class ModeInfo
            {
                /// <summary>
                /// Gets or sets the width in pixels for this mode.
                /// </summary>
                public uint ResolutionWidth { get; set; }

                /// <summary>
                /// Gets or sets the height in pixels for this mode.
                /// </summary>
                public uint ResolutionHeight { get; set; }

                /// <summary>
                /// Gets or sets the frame rates numerator for this mode.
                /// </summary>
                public uint FrameRateNumerator { get; set; }

                /// <summary>
                /// Gets or sets the frame rates denominator for this mode.
                /// </summary>
                public uint FrameRateDenominator { get; set; }

                /// <summary>
                /// Gets or sets the pixel format for this mode.
                /// </summary>
                public Microsoft.Psi.Imaging.PixelFormat Format { get; set; }

                /// <summary>
                /// Gets or sets the device specific mode.
                /// </summary>
                public uint Mode { get; set; }
            }
        }
    }
}
