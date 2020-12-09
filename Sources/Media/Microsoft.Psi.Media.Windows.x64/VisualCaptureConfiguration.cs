// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using System.Windows.Media;

    /// <summary>
    /// Encapsulates configuration for VisualCapture component.
    /// </summary>
    public class VisualCaptureConfiguration
    {
        /// <summary>
        /// Default configuration.
        /// </summary>
        public static readonly VisualCaptureConfiguration Default = new VisualCaptureConfiguration()
        {
        };

        /// <summary>
        /// Gets or sets the interval at which to render and emit frames of the rendered visual..
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the Windows Media Visual to stream.
        /// </summary>
        public Visual Visual { get; set; }

        /// <summary>
        /// Gets or sets the pixel width at which to render the visual.
        /// </summary>
        public int PixelWidth { get; set; }

        /// <summary>
        /// Gets or sets the pixel height at which to render the visual.
        /// </summary>
        public int PixelHeight { get; set; }
    }
}
