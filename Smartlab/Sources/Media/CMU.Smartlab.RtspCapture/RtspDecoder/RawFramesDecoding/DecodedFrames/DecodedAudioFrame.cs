// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;

    /// <summary>
    /// this component...
    /// </summary>
    public class DecodedAudioFrame : IDecodedAudioFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecodedAudioFrame"/> class.
        /// </summary>
        /// <param name="timestamp">.</param>
        /// <param name="decodedData">..</param>
        /// <param name="format">...</param>
        public DecodedAudioFrame(DateTime timestamp, ArraySegment<byte> decodedData, AudioFrameFormat format)
        {
            this.Timestamp = timestamp;
            this.DecodedBytes = decodedData;
            this.Format = format;
        }

        /// <summary>
        /// Gets....
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets....
        /// </summary>
        public ArraySegment<byte> DecodedBytes { get; }

        /// <summary>
        /// Gets....
        /// </summary>
        public AudioFrameFormat Format { get; }
    }
}
