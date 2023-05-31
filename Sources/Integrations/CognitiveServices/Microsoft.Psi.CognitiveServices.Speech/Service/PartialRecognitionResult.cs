// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents a partial speech recognition result.
    /// </summary>
    public class PartialRecognitionResult : SpeechResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialRecognitionResult"/> class.
        /// </summary>
        /// <param name="speechHypothesisMessage">The speech.hypothesis message returned by the service.</param>
        internal PartialRecognitionResult(SpeechHypothesisMessage speechHypothesisMessage)
            : base(speechHypothesisMessage.Offset, speechHypothesisMessage.Duration)
        {
            this.Text = speechHypothesisMessage.Text;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialRecognitionResult"/> class.
        /// </summary>
        /// <param name="speechFragmentMessage">The speech.fragment message returned by the service.</param>
        internal PartialRecognitionResult(SpeechFragmentMessage speechFragmentMessage)
            : base(speechFragmentMessage.Offset, speechFragmentMessage.Duration)
        {
            this.Text = speechFragmentMessage.Text;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialRecognitionResult"/> class.
        /// </summary>
        internal PartialRecognitionResult()
            : base()
        {
            this.Text = string.Empty;
        }

        /// <summary>
        /// Gets the recognized text.
        /// </summary>
        public string Text { get; }
    }
}
