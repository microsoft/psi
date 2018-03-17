// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a speech.hypothesis message from the service.
    /// </summary>
    internal class SpeechHypothesisMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechHypothesisMessage"/> class.
        /// </summary>
        public SpeechHypothesisMessage()
            : base("speech.hypothesis")
        {
        }

        /// <summary>
        /// Gets or sets the text of the hypothesis.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the offset (in 100-nanosecond units) when the phrase was recognized,
        /// relative to the start of the audio stream.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Gets or sets the duration (in 100-nanosecond units) of this speech phrase.
        /// </summary>
        public long Duration { get; set; }
    }
}
