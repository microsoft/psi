// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Defines a speech recognition result.
    /// </summary>
    /// <remarks>
    /// Components that perform speech recognition may output speech recognition results that implement this interface.
    /// </remarks>
    public interface ISpeechRecognitionResult
    {
        /// <summary>
        /// Gets the text of this ISpeechRecognitionResult.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the confidence of this ISpeechRecognitionResult.
        /// </summary>
        double? Confidence { get; }

        /// <summary>
        /// Gets the alternates of this ISpeechRecognitionResult.
        /// </summary>
        ISpeechRecognitionAlternate[] Alternates { get; }

        /// <summary>
        /// Gets the audio buffer that produced the recognition result. This may be empty
        /// where the audio is not available.
        /// </summary>
        AudioBuffer Audio { get; }

        /// <summary>
        /// Gets the duration of the audio that produced the recognition result.
        /// </summary>
        TimeSpan? Duration { get; }
    }
}