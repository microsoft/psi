// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    /// <summary>
    /// Represents the configuration for the <see cref="AudioPlayer"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="AudioPlayer"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class AudioPlayerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayerConfiguration"/> class.
        /// </summary>
        /// <param name="name">Device name (e.g. "plughw:0,0").</param>
        /// <param name="format">Wave format.</param>
        public AudioPlayerConfiguration(string name, WaveFormat format)
        {
            this.DeviceName = name;
            this.Format = format;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayerConfiguration"/> class.
        /// </summary>
        /// <remarks>Defaults to 16kHz, 1 channel, 16-bit PCM.</remarks>
        /// <param name="name">Device name (e.g. "plughw:0,0").</param>
        public AudioPlayerConfiguration(string name)
            : this(name, WaveFormat.Create16kHz1Channel16BitPcm())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioPlayerConfiguration"/> class.
        /// </summary>
        /// <remarks>Defaults to 16kHz, 1 channel, 16-bit PCM.</remarks>
        public AudioPlayerConfiguration()
            : this("plughw:0,0", WaveFormat.Create16kHz1Channel16BitPcm())
        {
        }

        /// <summary>
        /// Gets or sets the name of the audio device.
        /// </summary>
        /// <remarks>
        /// Use this to specify the name of the audio recording device from which to capture audio.
        /// </remarks>
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the desired format for the audio playback.
        /// </summary>
        public WaveFormat Format { get; set; }
    }
}
