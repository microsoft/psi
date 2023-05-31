// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;

    /// <summary>
    /// ...
    /// </summary>
    public interface IDecodedAudioFrame
    {
        /// <summary>
        /// Gets....
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets....
        /// </summary>
        ArraySegment<byte> DecodedBytes { get; }

        /// <summary>
        /// Gets....
        /// </summary>
        AudioFrameFormat Format { get; }
    }
}