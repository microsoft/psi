// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;

    /// <summary>
    /// ...
    /// </summary>
    public interface IDecodedVideoFrame
    {
        /// <summary>
        /// .........
        /// </summary>
        /// <param name="buffer">.</param>
        /// <param name="bufferStride">..</param>
        /// <param name="transformParameters">...</param>
        void TransformTo(IntPtr buffer, int bufferStride, TransformParameters transformParameters);
    }
}