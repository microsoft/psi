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

            // These values affect the latency of results from the VAD. Due to inherent delay
            // between the time audio is sent to the internal recognition engine and when the
            // engine detects that speech is present and makes a state transition, we need to
            // add these offsets to the computed time at which the state transition occurs to
            // ensure proper alignment between the audio and VAD result. A negative value
            // will shift the result earlier in time to account for this delay. However, this
            // will also contribute to the latency of the VAD output, so we should tune this
            // to be as close to zero as possible while still maintaining correctness. Values
            // of between -50ms and -150ms appear to be reasonable.
            this.VoiceActivityStartOffsetMs = -150;
            this.VoiceActivityEndOffsetMs = -150;

            // Defaults to 16 kHz, 16-bit, 1-channel PCM samples
            this.InputFormat = WaveFormat.Create16kHz1Channel16BitPcm();

            // Modify these values to improve VAD responsiveness. The EndSilenceTimeoutMs and
            // EndSilenceTimeoutAmbiguousMs parameters seems to matter most. Initialized to the
            // default values as specified in the documentation here:
            // https://docs.microsoft.com/en-us/dotnet/api/system.speech.recognition.speechrecognitionengine?view=netframework-4.8#properties
            this.InitialSilenceTimeoutMs = 0;
            this.BabbleTimeoutMs = 0;
            this.EndSilenceTimeoutAmbiguousMs = 500;
            this.EndSilenceTimeoutMs = 150;
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
        public WaveFormat InputFormat { get; set; }
    }
}
