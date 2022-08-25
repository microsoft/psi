﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    /// <summary>
    /// Configuration for the <see cref="MediaCaptureMicrophone"/> component.
    /// </summary>
    public class MediaCaptureMicrophoneConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether the audio buffer is emitted.
        /// </summary>
        public bool OutputAudio { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the audio buffer should output single-channel data.
        /// </summary>
        public bool SingleChannel { get; set; } = true;

        /// <summary>
        /// Gets or sets the outputted audio channel number if more than one channels are available.
        /// </summary>
        public uint AudioChannelNumber { get; set; } = 1;
    }
}
