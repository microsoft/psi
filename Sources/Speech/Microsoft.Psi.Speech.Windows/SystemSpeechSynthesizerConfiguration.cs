// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents the configuration for the <see cref="SystemSpeechSynthesizer"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="SystemSpeechSynthesizer"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class SystemSpeechSynthesizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechSynthesizerConfiguration"/> class.
        /// </summary>
        public SystemSpeechSynthesizerConfiguration()
        {
        }

        /// <summary>
        /// Gets or sets the text-to-speech voice to use.
        /// </summary>
        public string Voice { get; set; } = "Microsoft Zira Desktop";

        /// <summary>
        /// Gets or sets a value indicating whether the output audio stream is persisted.
        /// </summary>
        public bool PersistAudio { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether synthesized speech audio
        /// output should be redirected to the default audio device instead of
        /// the output stream. Useful for debugging purposes.
        /// </summary>
        public bool UseDefaultAudioPlaybackDevice { get; set; } = false;

        /// <summary>
        /// Gets or sets the length of the synthesizer's output audio buffer in milliseconds.
        /// </summary>
        public int BufferLengthInMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the prosody rate for the speech.
        /// </summary>
        public double ProsodyRate { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the prosody pitch for the speech. Possible values: x-low, low, medium, high, x-high, or default
        /// Todo: make this an enum.
        /// </summary>
        public string ProsodyPitch { get; set; } = "default";

        /// <summary>
        /// Gets or sets the prosody volume for the speech. Possible values: silent, x-soft, soft, medium, loud, x-loud, or default
        /// Todo: make this an enum.
        /// </summary>
        public string ProsodyVolume { get; set; } = "default";

        /// <summary>
        /// Gets or sets the output format of the audio stream.
        /// </summary>
        public WaveFormat OutputFormat { get; set; } = WaveFormat.Create16kHz1Channel16BitPcm();
    }
}
