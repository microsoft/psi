// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    /// <summary>
    /// Represents the configuration for the <see cref="AudioResampler"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="AudioResampler"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class AudioResamplerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioResamplerConfiguration"/> class.
        /// </summary>
        public AudioResamplerConfiguration()
        {
            this.TargetLatencyInMs = 20;
            this.InputFormat = WaveFormat.Create16kHz1Channel16BitPcm();
            this.OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm();
        }

        /// <summary>
        /// Gets or sets the target audio latency.
        /// </summary>
        /// <remarks>
        /// This parameter controls the amount of audio to resample and output at a time, which in
        /// turn determines the latency of the audio output. The larger this value, the more audio
        /// data is carried in each <see cref="AudioBuffer"/> and the longer the audio latency. For
        /// live audio capture, we normally want this value to be small as possible. By default,
        /// this value is set to 20 milliseconds. Is is safe to leave this unchanged.
        /// </remarks>
        public int TargetLatencyInMs { get; set; }

        /// <summary>
        /// Gets or sets the input format of the audio stream to be resampled.
        /// </summary>
        /// <remarks>
        /// This specifies the expected format of the audio arriving on the input stream. If not
        /// set, the <see cref="AudioResampler"/> component will attempt to infer the audio format
        /// from the <see cref="AudioBuffer"/> messages arriving on the input stream.
        /// </remarks>
        public WaveFormat InputFormat { get; set; }

        /// <summary>
        /// Gets or sets the output format for the resampled audio.
        /// </summary>
        public WaveFormat OutputFormat { get; set; }
    }
}
