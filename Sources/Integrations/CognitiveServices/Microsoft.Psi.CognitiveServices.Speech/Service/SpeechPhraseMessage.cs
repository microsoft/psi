// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a speech.phrase message from the service.
    /// </summary>
    internal class SpeechPhraseMessage : SpeechServiceTextMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechPhraseMessage"/> class.
        /// </summary>
        public SpeechPhraseMessage()
           : base("speech.phrase")
        {
        }

        /// <summary>
        /// Gets or sets the status of the recognition.
        /// </summary>
        public RecognitionStatus RecognitionStatus { get; set; }

        /// <summary>
        /// Gets or sets the recognized phrase after capitalization, punctuation, and inverse-text-
        /// normalization have been applied and profanity has been masked with asterisks.
        /// The DisplayText field is present only if the RecognitionStatus field has the value Success.
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Gets or sets the offset (in 100-nanosecond units) at which the phrase was recognized,
        /// relative to the start of the audio stream.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Gets or sets the duration (in 100-nanosecond units) of this speech phrase.
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// Gets or sets the N-best list of top speech alternates.
        /// </summary>
        public NBestValue[] NBest { get; set; }

        /// <summary>
        /// Represents a speech alternate value.
        /// </summary>
        internal class NBestValue
        {
            /// <summary>
            /// Gets or sets the confidence score of this alternate.
            /// </summary>
            public double Confidence { get; set; }

            /// <summary>
            /// Gets or sets the lexical form of the recognized text.
            /// </summary>
            public string Lexical { get; set; }

            /// <summary>
            /// Gets or sets the ITN form of the recognized text.
            /// </summary>
            public string ITN { get; set; }

            /// <summary>
            /// Gets or sets the masked ITN form of the recognized text.
            /// </summary>
            public string MaskedITN { get; set; }

            /// <summary>
            /// Gets or sets the display form of the recognized text.
            /// </summary>
            public string Display { get; set; }
        }
    }
}
