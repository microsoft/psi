// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    /// <summary>
    /// Represents the available speech recognition modes.
    /// </summary>
    public enum SpeechRecognitionMode
    {
        /// <summary>
        /// In interactive mode, a user makes short requests and expects the application to perform an action in response.
        /// </summary>
        Interactive,

        /// <summary>
        /// In conversation mode, users are engaged in a human-to-human conversation.
        /// </summary>
        Conversation,

        /// <summary>
        /// In dictation mode, users recite longer utterances to the application for further processing.
        /// </summary>
        Dictation,
    }
}
