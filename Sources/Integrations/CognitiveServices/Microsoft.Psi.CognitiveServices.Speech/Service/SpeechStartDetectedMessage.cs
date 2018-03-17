// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a speech.startDetected message from the service.
    /// </summary>
    internal class SpeechStartDetectedMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechStartDetectedMessage"/> class.
        /// </summary>
        public SpeechStartDetectedMessage()
           : base("speech.startdetected")
        {
        }

        /// <summary>
        /// Gets or sets the the offset (in 100-nanosecond units) when speech was detected in the
        /// audio stream, relative to the start of the stream.
        /// </summary>
        public int Offset { get; set; }
    }
}
