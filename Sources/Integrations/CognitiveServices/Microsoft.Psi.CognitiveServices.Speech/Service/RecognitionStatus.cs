// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// Represents the possible recognition status values returned by the service.
    /// </summary>
    public enum RecognitionStatus
    {
        /// <summary>
        /// The recognition was successful and the DisplayText field is present.
        /// </summary>
        Success,

        /// <summary>
        /// Speech was detected in the audio stream, but no words from the target language were matched.
        /// </summary>
        NoMatch,

        /// <summary>
        /// The start of the audio stream contained only silence, and the service timed out waiting for speech.
        /// </summary>
        InitialSilenceTimeout,

        /// <summary>
        /// The start of the audio stream contained only noise, and the service timed out waiting for speech.
        /// </summary>
        BabbleTimeout,

        /// <summary>
        /// The recognition service encountered an internal error and could not continue.
        /// </summary>
        Error,

        /// <summary>
        /// The service detected the end of dictation in dictation mode.
        /// </summary>
        EndOfDictation,
    }
}
