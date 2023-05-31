// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;

    /// <summary>
    /// ..
    /// </summary>
    public class DecodedVideoFrame : IDecodedVideoFrame
    {
        private readonly Action<IntPtr, int, TransformParameters> transformAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecodedVideoFrame"/> class.
        /// </summary>
        /// <param name="transformAction">.</param>
        public DecodedVideoFrame(Action<IntPtr, int, TransformParameters> transformAction)
        {
            this.transformAction = transformAction;
        }

        /// <summary>
        /// ..
        /// </summary>
        /// <param name="buffer">.</param>
        /// <param name="bufferStride">........</param>
        /// <param name="transformParameters">.............</param>
        public void TransformTo(IntPtr buffer, int bufferStride, TransformParameters transformParameters)
        {
            this.transformAction(buffer, bufferStride, transformParameters);
        }
    }
}