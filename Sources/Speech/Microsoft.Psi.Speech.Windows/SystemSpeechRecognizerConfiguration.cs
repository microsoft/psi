// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System.Xml.Serialization;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents the configuration for the <see cref="SystemSpeechRecognizer"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="SystemSpeechRecognizer"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class SystemSpeechRecognizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechRecognizerConfiguration"/> class.
        /// </summary>
        public SystemSpeechRecognizerConfiguration()
        {
            this.Language = "en-us";
            this.Grammars = null;
            this.BufferLengthInMs = 1000;

            // Defaults to 16 kHz, 16-bit, 1-channel PCM samples
            this.InputFormat = WaveFormat.Create16kHz1Channel16BitPcm();
        }

        /// <summary>
        /// Gets or sets the speech recognition language.
        /// </summary>
        /// <remarks>
        /// Use this to set the locale for speech recognition. If not specified, this defaults to "en-us"
        /// (U.S. English). Other supported locales include "en-gb", "de-de", "es-es", "fr-fr", "ja-jp",
        /// "zh-cn" and "zh-tw".
        /// </remarks>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the list of grammar files.
        /// </summary>
        /// <remarks>
        /// Use this to specify a set of grammar files that the recognizer should use for speech recognition.
        /// Grammar files are XML-format files that conform to the
        /// <a href="http://go.microsoft.com/fwlink/?LinkId=201761">W3C Speech Recognition Grammar Specification (SRGS) Version 1.0</a>.
        /// If this configuration property is not specified or set to null, the recognizer will use a default
        /// context-free grammar used for free text dictation.
        /// </remarks>
        [XmlArrayItem("Grammar")]
        public GrammarInfo[] Grammars { get; set; }

        /// <summary>
        /// Gets or sets the length of the recognizer's input stream buffer in milliseconds.
        /// </summary>
        /// <remarks>
        /// Audio arriving on the input stream will be stored in an internal buffer for the speech recognition
        /// engine to read as it is able. This buffer will block when full until the recognition engine is able
        /// to read from the buffer. Set this value to modify the length of the buffer, which is computed based
        /// on the length of audio to buffer in milliseconds and the audio input format. By default, a 1000 ms
        /// buffer is used. It is safe to leave this value unchanged.
        /// </remarks>
        public int BufferLengthInMs { get; set; }

        /// <summary>
        /// Gets or sets the number of milliseconds during which the internal speech detection
        /// engine accepts input containing only silence before making a state transition.
        /// </summary>
        public int InitialSilenceTimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the number of milliseconds during which the internal speech detection
        /// engine accepts input containing only background noise before making a state transition.
        /// </summary>
        public int BabbleTimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the number of milliseconds of silence that the internal speech detection
        /// engine will accept at the end of unambiguous input before making a state transition.
        /// </summary>
        public int EndSilenceTimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets the number of milliseconds of silence that the internal speech detection
        /// engine will accept at the end of ambiguous input before making a state transition.
        /// </summary>
        public int EndSilenceTimeoutAmbiguousMs { get; set; }

        /// <summary>
        /// Gets or sets the expected input format of the audio stream.
        /// </summary>
        /// <remarks>
        /// Preferred input audio formats are 1-channel, 16-bit PCM samples. Use the
        /// <see cref="WaveFormat.Create16kHz1Channel16BitPcm"/> or
        /// <see cref="WaveFormat.Create16BitPcm(int, int)"/> static methods to create the appropriate
        /// <see cref="WaveFormat"/> object. If not specified, a default value of 16000 Hz is assumed.
        /// </remarks>
        public WaveFormat InputFormat { get; set; }
    }
}
