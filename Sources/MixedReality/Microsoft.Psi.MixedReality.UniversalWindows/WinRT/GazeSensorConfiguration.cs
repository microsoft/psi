// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.WinRT
{
    using System;

    /// <summary>
    /// The configuration for the <see cref="GazeSensor"/> component.
    /// </summary>
    public class GazeSensorConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to emit head gaze poses.
        /// </summary>
        /// <remarks>The origin of the pose is between the user's eyes.</remarks>
        public bool OutputHeadGaze { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to emit eye gaze poses.
        /// </summary>
        /// <remarks>The origin of the pose ray is between the user's eyes.</remarks>
        public bool OutputEyeGaze { get; set; } = true;

        /// <summary>
        /// Gets or sets the desired interval for querying the gaze and emitting (default is 60 Hz).
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1.0 / 60.0);
    }
}