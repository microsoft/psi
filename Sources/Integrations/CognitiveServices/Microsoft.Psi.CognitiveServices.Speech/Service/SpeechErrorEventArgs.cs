// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;

    /// <summary>
    /// Represents the arguments of a speech error event.
    /// </summary>
    public class SpeechErrorEventArgs : EventArgs
    {
        private string speechErrorText;
        private SpeechClientStatus speechErrorCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechErrorEventArgs"/> class.
        /// </summary>
        /// <param name="speechErrorCode">The error code.</param>
        /// <param name="speechErrorText">The error text.</param>
        internal SpeechErrorEventArgs(SpeechClientStatus speechErrorCode, string speechErrorText)
        {
            this.speechErrorCode = speechErrorCode;
            this.speechErrorText = speechErrorText;
        }

        /// <summary>
        /// Gets the speech error code.
        /// </summary>
        public SpeechClientStatus SpeechErrorCode => this.speechErrorCode;

        /// <summary>
        /// Gets the speech error text.
        /// </summary>
        public string SpeechErrorText => this.speechErrorText;
    }
}
