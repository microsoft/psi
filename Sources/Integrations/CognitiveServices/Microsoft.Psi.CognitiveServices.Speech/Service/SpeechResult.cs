// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;

    /// <summary>
    /// Represents the abstract base class of a speech service result.
    /// </summary>
    public abstract class SpeechResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechResult"/> class.
        /// </summary>
        /// <param name="offsetInTicks">The offset of the speech from the start of the audio, in ticks.</param>
        /// <param name="durationInTicks">The duration of the speech, in ticks.</param>
        internal SpeechResult(long offsetInTicks, long durationInTicks)
        {
            this.Offset = TimeSpan.FromTicks(offsetInTicks);
            this.Duration = TimeSpan.FromTicks(durationInTicks);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechResult"/> class.
        /// </summary>
        internal SpeechResult()
        {
            this.Offset = TimeSpan.Zero;
            this.Duration = TimeSpan.Zero;
        }

        /// <summary>
        /// Gets the offset of the speech from the start of the audio.
        /// </summary>
        public TimeSpan Offset { get; }

        /// <summary>
        /// Gets duration of the speech.
        /// </summary>
        public TimeSpan Duration { get; }
    }
}
