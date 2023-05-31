// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a speech.fragment message from the service.
    /// </summary>
    internal class SpeechFragmentMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechFragmentMessage"/> class.
        /// </summary>
        public SpeechFragmentMessage()
            : base("speech.fragment")
        {
        }

        /// <summary>
        /// Gets or sets the text of the fragment.
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
