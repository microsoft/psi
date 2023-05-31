// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    /// <summary>
    /// this component...
    /// </summary>
    public readonly struct AudioFrameFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFrameFormat"/> struct.
        /// </summary>
        /// <param name="sampleRate">.</param>
        /// <param name="bitPerSample">..</param>
        /// <param name="channels">...</param>
        public AudioFrameFormat(int sampleRate, int bitPerSample, int channels)
        {
            this.SampleRate = sampleRate;
            this.BitPerSample = bitPerSample;
            this.Channels = channels;
            this.BytesPerSecond = sampleRate * bitPerSample / 8 * channels;
        }

        /// <summary>
        /// Gets....
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets....
        /// </summary>
        public int BitPerSample { get; }

        /// <summary>
        /// Gets....
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// Gets....
        /// </summary>
        public int BytesPerSecond { get; }
    }
}
