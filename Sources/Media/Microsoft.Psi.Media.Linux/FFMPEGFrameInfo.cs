// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if FFMPEG

namespace Microsoft.Psi.Media.Native.Linux
{
    /// <summary>
    /// Contains information about the current video/audio frame
    /// </summary>
    public class FFMPEGFrameInfo
    {
        /// <summary>
        /// Constant used to define frame data associated with the video stream
        /// </summary>
        public const int FrameTypeVideo = 0;

        /// <summary>
        /// Constant used to define frame data associated with the audio stream
        /// </summary>
        public const int FrameTypeAudio = 1;

        /// <summary>
        /// Gets or sets the type of frame data
        /// </summary>
        public int FrameType { get; set; } // Type of data to be returned next by call to ReadFrameData

        /// <summary>
        /// Gets or sets the buffer size of the current frame
        /// </summary>
        public int BufferSize { get; set; } // The size of the buffer required to hold the decompressed data
    }
}
#endif