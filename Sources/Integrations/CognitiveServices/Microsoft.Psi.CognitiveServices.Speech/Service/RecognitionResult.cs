// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a speech recognition result.
    /// </summary>
    public class RecognitionResult : SpeechResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecognitionResult"/> class.
        /// </summary>
        /// <param name="speechPhraseMessage">The speech.phrase message returned by the service.</param>
        internal RecognitionResult(SpeechPhraseMessage speechPhraseMessage)
            : base(speechPhraseMessage.Offset, speechPhraseMessage.Duration)
        {
            this.RecognitionStatus = speechPhraseMessage.RecognitionStatus;

            if (speechPhraseMessage.NBest != null)
            {
                var phraseResults = new RecognizedPhrase[speechPhraseMessage.NBest.Length];

                for (int i = 0; i < phraseResults.Length; i++)
                {
                    phraseResults[i] = new RecognizedPhrase(speechPhraseMessage.NBest[i]);
                }

                this.Results = phraseResults;
            }
        }

        /// <summary>
        /// Gets the recognition status code.
        /// </summary>
        public RecognitionStatus RecognitionStatus { get; }

        /// <summary>
        /// Gets the list of recognized phrases.
        /// </summary>
        public RecognizedPhrase[] Results { get; }
    }
}
