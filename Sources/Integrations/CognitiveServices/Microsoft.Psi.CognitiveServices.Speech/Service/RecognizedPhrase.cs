// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a single recognized phrase alternate.
    /// </summary>
    public class RecognizedPhrase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecognizedPhrase"/> class.
        /// </summary>
        /// <param name="value">The N-best value representing the alternate.</param>
        internal RecognizedPhrase(SpeechPhraseMessage.NBestValue value)
        {
            this.LexicalForm = value.Lexical;
            this.DisplayText = value.Display;
            this.InverseTextNormalizationResult = value.ITN;
            this.MaskedInverseTextNormalizationResult = value.MaskedITN;
            this.Confidence = value.Confidence;
        }

        /// <summary>
        /// Gets the lexical form of the recognized text.
        /// </summary>
        public string LexicalForm { get; }

        /// <summary>
        /// Gets the display form of the recognized text.
        /// </summary>
        public string DisplayText { get; }

        /// <summary>
        /// Gets the ITN form of the recognized text.
        /// </summary>
        public string InverseTextNormalizationResult { get; }

        /// <summary>
        /// Gets the masked ITN form of the recognized text.
        /// </summary>
        public string MaskedInverseTextNormalizationResult { get; }

        /// <summary>
        /// Gets the confidence score of this alternate.
        /// </summary>
        public double Confidence { get; }
    }
}
