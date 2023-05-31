// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    /// <summary>
    /// Defines an incremental, streaming speech recognition result which may be partial or final.
    /// </summary>
    /// <remarks>
    /// Components that perform streaming speech recognition (where partial hypotheses are generated while
    /// the speech is in progress) may output speech recognition results that implement this interface.
    /// </remarks>
    public interface IStreamingSpeechRecognitionResult : ISpeechRecognitionResult
    {
        /// <summary>
        /// Gets a value indicating whether this result is final (true), or if it is a partial hypothesis (false).
        /// </summary>
        bool IsFinal { get; }
    }
}
