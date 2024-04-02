// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    /// <summary>
    /// Represents an input event that triggers dialog planning.
    /// </summary>
    public class SpeechRecognitionInputEvent : IInputEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognitionInputEvent"/> class.
        /// </summary>
        /// <param name="speechRecognitionResult">The speech recognition result.</param>
        public SpeechRecognitionInputEvent(string speechRecognitionResult)
        {
            this.SpeechRecognitionResult = speechRecognitionResult;
        }

        /// <summary>
        /// Gets the speech recognition result.
        /// </summary>
        public string SpeechRecognitionResult { get; }
    }
}
