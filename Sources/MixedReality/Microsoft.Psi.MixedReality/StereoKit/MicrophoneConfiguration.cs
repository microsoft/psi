// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// The configuration for the <see cref="Microphone"/> component.
    /// </summary>
    public class MicrophoneConfiguration
    {
        /// <summary>
        /// Gets or sets the audio sampling interval.
        /// </summary>
        public TimeSpan SamplingInterval { get; set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Gets the audio format.
        /// </summary>
        /// <remarks>Currently only supports 1-channel WAVE_FORMAT_IEEE_FLOAT at 48kHz.</remarks>
        public WaveFormat AudioFormat { get; } = WaveFormat.CreateIeeeFloat(48000, 1);
    }
}