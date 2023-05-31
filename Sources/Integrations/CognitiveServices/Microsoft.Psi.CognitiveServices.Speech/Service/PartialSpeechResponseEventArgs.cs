// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;

    /// <summary>
    /// Represents the arguments of a partial speech response event.
    /// </summary>
    public class PartialSpeechResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartialSpeechResponseEventArgs"/> class.
        /// </summary>
        /// <param name="partialResult">The partial speech recognition result text.</param>
        internal PartialSpeechResponseEventArgs(PartialRecognitionResult partialResult)
        {
            this.PartialResult = partialResult;
        }

        /// <summary>
        /// Gets the partial speech recognition result.
        /// </summary>
        public PartialRecognitionResult PartialResult { get; }
    }
}
