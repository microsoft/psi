// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;

    /// <summary>
    /// Encapsulates configuration for WindowCapture component.
    /// </summary>
    public class WindowCaptureConfiguration
    {
        /// <summary>
        /// Default configuration.
        /// </summary>
        public static readonly WindowCaptureConfiguration Default = new WindowCaptureConfiguration();

        /// <summary>
        /// Gets or sets the interval at which to render and emit frames of the window..
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets the Window handle to capture (default=desktop window/primary screen).
        /// </summary>
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;
    }
}
