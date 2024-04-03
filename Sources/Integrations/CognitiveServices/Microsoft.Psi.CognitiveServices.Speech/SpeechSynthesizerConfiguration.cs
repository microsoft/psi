// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System;
    using Microsoft.Psi.Speech;

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
        /// Gets or sets the speech synthesis voice name.
        /// </summary>
        public string VoiceName { get; set; } = null;

        /// <summary>
        /// Gets or sets the max size of the packets of audio to be posted (number of bytes).
        /// </summary>
        public int AudioPacketSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets a value indicating whether the audio buffers are streamed in real-time.
        /// </summary>
        public bool StreamAudioBuffersInRealTime { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum real-time anticipation when streaming audio buffers in real-time.
        /// </summary>
        /// <remarks>
        /// When configured to stream audio buffers in real-time, the speech synthesizer will send audio buffers
        /// if the originating time of the audio buffer is within this time-span of the current time. This is to
        /// ensure adequate time for the audio buffers to be processed and played out in real-time.
        /// </remarks>
        public TimeSpan MaxRealTimeAnticipationWhenStreamingAudioBuffersInRealTime { get; set; } = TimeSpan.FromMilliseconds(300);

        /// <summary>
        /// Gets or sets the cache to use for speech synthesis.
        /// </summary>
        public SpeechSynthesisCache Cache { get; set; } = null;
    }
}
