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
        private string partialResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialSpeechResponseEventArgs"/> class.
        /// </summary>
        /// <param name="partialResult">The partial speech recognition result text.</param>
        internal PartialSpeechResponseEventArgs(string partialResult)
        {
            this.partialResult = partialResult;
        }

        /// <summary>
        /// Gets the partial speech recognition result text.
        /// </summary>
        public string PartialResult => this.partialResult;
    }
}
