// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System.Xml.Serialization;

    /// <summary>
    /// Represents the configuration for the <see cref="SystemSpeechIntentDetector"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="SystemSpeechIntentDetector"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class SystemSpeechIntentDetectorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechIntentDetectorConfiguration"/> class.
        /// </summary>
        public SystemSpeechIntentDetectorConfiguration()
        {
            this.Language = "en-us";
            this.Grammars = null;
        }

        /// <summary>
        /// Gets or sets the intent detection language.
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
        /// Use this to specify a set of grammar files that the intent detector should use.
        /// Grammar files are XML-format files that conform to the
        /// <a href="http://go.microsoft.com/fwlink/?LinkId=201761">W3C Speech Recognition Grammar Specification (SRGS) Version 1.0</a>.
        /// </remarks>
        [XmlArrayItem("Grammar")]
        public GrammarInfo[] Grammars { get; set; }
    }
}
