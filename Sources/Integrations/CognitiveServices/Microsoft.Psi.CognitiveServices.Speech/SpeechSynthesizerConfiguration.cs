// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    /// <summary>
    /// Represents the configuration for the <see cref="SpeechSynthesizer"/> component.
    /// </summary>
    public sealed class SpeechSynthesizerConfiguration
    {
        /// <summary>
        /// Gets or sets the subscription key.
        /// </summary>
        public string SubscriptionKey { get; set; } = null;

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        public string Region { get; set; } = null;

        /// <summary>
        /// Gets or sets the max size of the packets of audio to be posted (number of bytes).
        /// </summary>
        public int AudioPacketSize { get; set; } = 4096;
    }
}
