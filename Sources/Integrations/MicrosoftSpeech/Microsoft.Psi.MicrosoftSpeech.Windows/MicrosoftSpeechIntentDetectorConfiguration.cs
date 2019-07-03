// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MicrosoftSpeech
{
    using System.Xml.Serialization;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Represents the configuration for the <see cref="MicrosoftSpeechIntentDetector"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="MicrosoftSpeechIntentDetector"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class MicrosoftSpeechIntentDetectorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftSpeechIntentDetectorConfiguration"/> class.
        /// </summary>
        public MicrosoftSpeechIntentDetectorConfiguration()
        {
            this.Language = "en-us";
            this.Grammars = null;
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
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the list of grammar files.
        /// </summary>
        /// <remarks>
        /// Use this to specify a set of grammar files that the recognizer should use for intent detection.
        /// Grammar files are XML-format files that conform to the
        /// <a href="http://go.microsoft.com/fwlink/?LinkId=201761">W3C Speech Recognition Grammar Specification (SRGS) Version 1.0</a>.
        /// </remarks>
        [XmlElement("Grammar")]
        public GrammarInfo[] Grammars { get; set; }
    }
}
