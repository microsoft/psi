// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a speech.endDetected message from the service.
    /// </summary>
    internal class SpeechEndDetectedMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechEndDetectedMessage"/> class.
        /// </summary>
        public SpeechEndDetectedMessage()
            : base("speech.enddetected")
        {
        }

        /// <summary>
        /// Gets or sets the offset when the end of speech was detected in 100-nanosecond units from the start of audio.
        /// </summary>
        public long Offset { get; set; }
    }
}
