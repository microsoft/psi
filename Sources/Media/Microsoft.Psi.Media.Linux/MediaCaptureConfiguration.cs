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
            Width = 1280,
            Height = 720,
            DeviceId = "/dev/video0",
            PixelFormat = PixelFormatId.YUYV,
        };

        /// <summary>
        /// Gets or sets encapsulates configuration for Video Camera component.
        /// </summary>
        public int Width { get; set; }

       /// <summary>
        /// Gets or sets the camera resolution height.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets device id used to identify the camera.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets device pixel format.
        /// </summary>
        public PixelFormatId PixelFormat { get; set; }
    }
}