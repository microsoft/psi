// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    /// <summary>
    /// KinectSensorConfiguration defines how to configure the Kinect.
    /// </summary>
    public class KinectSensorConfiguration
    {
        /// <summary>
        /// Default returns a default configuration for the Kinect.
        /// </summary>
        public static readonly KinectSensorConfiguration Default = new KinectSensorConfiguration() { OutputColor = true, OutputDepth = true, OutputBodies = true };

        /// <summary>
        /// Gets or sets a value indicating whether the depth stream is emitted from the Kinect.
        /// </summary>
        public bool OutputDepth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the color stream is emitted from the Kinect.
        /// </summary>
        public bool OutputColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the infrared stream is emitted from the Kinect.
        /// </summary>
        public bool OutputInfrared { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the long exposure infrared stream is emitted from the Kinect.
        /// </summary>
        public bool OutputLongExposureInfrared { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the bodies stream is emitted from the Kinect.
        /// </summary>
        public bool OutputBodies { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to output an RGB (color) + D (depth) image.
        /// Each component in the image is 16b per pixel.
        /// </summary>
        public bool OutputRGBD { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the audio stream is emitted.
        /// </summary>
        public bool OutputAudio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the calibration stream is emitted.
        /// </summary>
        public bool OutputCalibration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a mapping from color to depth is emitted.
        /// </summary>
        public bool OutputColorToCameraMapping { get; set; }
    }
}
