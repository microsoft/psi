// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;

    /// <summary>
    /// Represents the arguments of a speech response event.
    /// </summary>
    public class SpeechResponseEventArgs : EventArgs
    {
        private RecognitionResult result;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechResponseEventArgs"/> class.
        /// </summary>
        /// <param name="result">The speech recognition result.</param>
        internal SpeechResponseEventArgs(RecognitionResult result)
        {
            this.result = result;
        }

        /// <summary>
        /// Gets the speech recognition result.
        /// </summary>
        public RecognitionResult PhraseResponse => this.result;
    }
}
