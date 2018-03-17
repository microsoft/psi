// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System.Xml.Serialization;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents the configuration for the <see cref="SystemVoiceActivityDetector"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="SystemVoiceActivityDetector"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class SystemVoiceActivityDetectorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemVoiceActivityDetectorConfiguration"/> class.
        /// </summary>
        public SystemVoiceActivityDetectorConfiguration()
        {
            this.Language = "en-us";
            this.Grammars = null;
            this.BufferLengthInMs = 1000;
            this.VoiceActivityStartOffsetMs = -150;
            this.VoiceActivityEndOffsetMs = -150;

            // Defaults to 16 kHz, 16-bit, 1-channel PCM samples
            this.InputFormat = WaveFormat.Create16kHz1Channel16BitPcm();
        }

        /// <summary>
        /// Gets or sets the speech recognition language.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the list of grammar files.
        /// </summary>
        [XmlArrayItem("Grammar")]
        public GrammarInfo[] Grammars { get; set; }

        /// <summary>
        /// Gets or sets the length of the recognizer's input stream buffer in milliseconds.
        /// </summary>
        public int BufferLengthInMs { get; set; }

        /// <summary>
        /// Gets or sets the offset in milliseconds to add to the detected start of speech.
        /// </summary>
        public int VoiceActivityStartOffsetMs { get; set; }

        /// <summary>
        /// Gets or sets the offset in milliseconds to add to the detected end of speech.
        /// </summary>
        public int VoiceActivityEndOffsetMs { get; set; }

        /// <summary>
        /// Gets or sets the expected input format of the audio stream.
        /// </summary>
        public WaveFormat InputFormat { get; set; }
    }
}
