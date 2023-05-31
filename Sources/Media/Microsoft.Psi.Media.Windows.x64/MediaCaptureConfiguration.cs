// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    /// <summary>
    /// Encapsulates configuration for Video Camera component.
    /// </summary>
    public class MediaCaptureConfiguration
    {
        /// <summary>
        /// Default configuration.
        /// </summary>
        public static readonly MediaCaptureConfiguration Default = new MediaCaptureConfiguration()
        {
        };

        /// <summary>
        /// Gets or sets a value indicating whether the capture device should include audio capture.
        /// </summary>
        public bool CaptureAudio { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether Backlight Compensation is being applied.
        /// </summary>
        public PropertyValue<bool> BacklightCompensation { get; set; } = new PropertyValue<bool>();

        /// <summary>
        /// Gets or sets a value that defines the current brightness.
        /// </summary>
        public PropertyValue<int> Brightness { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines whether color is enable.
        /// </summary>
        public PropertyValue<bool> ColorEnable { get; set; } = new PropertyValue<bool>();

        /// <summary>
        /// Gets or sets a value that defines the current contrast.
        /// </summary>
        public PropertyValue<int> Contrast { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines the current gain.
        /// </summary>
        public PropertyValue<int> Gain { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines the current gamma.
        /// </summary>
        public PropertyValue<int> Gamma { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines the current hue.
        /// </summary>
        public PropertyValue<int> Hue { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines the current saturation.
        /// </summary>
        public PropertyValue<int> Saturation { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines the current sharpness.
        /// </summary>
        public PropertyValue<int> Sharpness { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines the current white balance.
        /// </summary>
        public PropertyValue<int> WhiteBalance { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value that defines the focus distance.
        /// </summary>
        public PropertyValue<int> Focus { get; set; } = new PropertyValue<int>();

        /// <summary>
        /// Gets or sets a value indicating whether the camera device is shared amongst multiple applications.
        /// </summary>
        public bool UseInSharedMode { get; set; } = false;

        /// <summary>
        /// Gets or sets the camera resolution width.
        /// </summary>
        public int Width { get; set; } = 1280;

        /// <summary>
        /// Gets or sets the camera resolution height.
        /// </summary>
        public int Height { get; set; } = 720;

        /// <summary>
        /// Gets or sets the camera framerate.
        /// </summary>
        public double Framerate { get; set; } = 15;

        /// <summary>
        /// Gets or sets device id used to identify the camera.
        /// </summary>
        public string DeviceId { get; set; } = null;

        /// <summary>
        /// Defines the type of a property on the media capture device.
        /// </summary>
        /// <typeparam name="T">Type used for the current Value.</typeparam>
        public class PropertyValue<T>
        {
            /// <summary>
            /// Gets or sets a value indicating the current value of the property.
            /// </summary>
            public T Value { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the current value was manually set
            /// or automatically set by the device.
            /// </summary>
            public bool Auto { get; set; }
        }
    }
}
