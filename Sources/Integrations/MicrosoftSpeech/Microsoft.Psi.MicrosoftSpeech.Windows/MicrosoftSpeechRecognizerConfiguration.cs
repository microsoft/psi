// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MicrosoftSpeech
{
    using System;
    using System.Xml.Serialization;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Represents the configuration for the <see cref="MicrosoftSpeechRecognizer"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="MicrosoftSpeechRecognizer"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    [Obsolete("The MicrosoftSpeechRecognizer component has been deprecated. Consider using the SystemSpeechRecognizer component available in Microsoft.Psi.Speech.Windows instead.", false)]
    public sealed class MicrosoftSpeechRecognizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftSpeechRecognizerConfiguration"/> class.
        /// </summary>
        public MicrosoftSpeechRecognizerConfiguration()
        {
        }

        /// <summary>
        /// Gets or sets the speech recognition language.
        /// </summary>
        /// <remarks>
        /// Use this to set the language for speech recognition. The language must be an installed Runtime
        /// Language for speech recognition. The Microsoft Speech Platform Runtime does not include any
        /// Runtime Languages for speech recognition. You must download and install a Runtime Language for
        /// each language in which you want to recognize speech. A Runtime Language includes the language model,
        /// acoustic model, and other data necessary to provision a speech engine to perform speech recognition
        /// in a particular language. For a list of supported Runtime Languages and to download them, see
        /// http://go.microsoft.com/fwlink/?LinkID=223569.
        /// </remarks>
        public string Language { get; set; } = "en-us";

        /// <summary>
        /// Gets or sets the list of grammar files.
        /// </summary>
        /// <remarks>
        /// Use this to specify a set of grammar files that the recognizer should use for speech recognition.
        /// Grammar files are XML-format files that conform to the
        /// <a href="http://go.microsoft.com/fwlink/?LinkId=201761">W3C Speech Recognition Grammar Specification (SRGS) Version 1.0</a>.
        /// At least one grammar is required in order to use the <see cref="MicrosoftSpeechRecognizer"/>.
        /// </remarks>
        [XmlElement("Grammar")]
        public GrammarInfo[] Grammars { get; set; } = null;

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
        public int BufferLengthInMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the number of milliseconds during which the internal speech detection
        /// engine accepts input containing only silence before making a state transition.
        /// </summary>
        public int InitialSilenceTimeoutMs { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of milliseconds during which the internal speech detection
        /// engine accepts input containing only background noise before making a state transition.
        /// </summary>
        public int BabbleTimeoutMs { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of milliseconds of silence that the internal speech detection
        /// engine will accept at the end of unambiguous input before making a state transition.
        /// </summary>
        public int EndSilenceTimeoutMs { get; set; } = 150;

        /// <summary>
        /// Gets or sets the number of milliseconds of silence that the internal speech detection
        /// engine will accept at the end of ambiguous input before making a state transition.
        /// </summary>
        public int EndSilenceTimeoutAmbiguousMs { get; set; } = 500;

        /// <summary>
        /// Gets or sets the number of milliseconds to wait for the final recognition result
        /// once the component has been stopped during pipeline shutdown.
        /// </summary>
        public int FinalRecognitionResultTimeoutMs { get; set; } = 500;

        /// <summary>
        /// Gets or sets the expected input format of the audio stream.
        /// </summary>
        /// <remarks>
        /// Preferred input audio formats are 1-channel, 16-bit PCM samples. Use the
        /// <see cref="WaveFormat.Create16kHz1Channel16BitPcm"/> or
        /// <see cref="WaveFormat.Create16BitPcm(int, int)"/> static methods to create the appropriate
        /// <see cref="WaveFormat"/> object. If not specified, a default value of 16000 Hz is assumed.
        /// </remarks>
        public WaveFormat InputFormat { get; set; } = WaveFormat.Create16kHz1Channel16BitPcm();
    }
}
