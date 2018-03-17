// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    /// <summary>
    /// Represents a speech recognition alternate.
    /// </summary>
    public interface ISpeechRecognitionAlternate
    {
        /// <summary>
        /// Gets the text of this alternate.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the confidence score of this alternate.
        /// </summary>
        double? Confidence { get; }
    }
}