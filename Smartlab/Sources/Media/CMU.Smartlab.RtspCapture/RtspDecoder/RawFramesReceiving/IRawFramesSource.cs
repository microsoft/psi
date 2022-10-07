// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;
    using RtspClientSharp.RawFrames;

    /// <summary>
    /// Component that .......
    /// </summary>
    public interface IRawFramesSource
    {
        /// <summary>
        /// Gets or sets........
        /// </summary>
        EventHandler<RawFrame> FrameReceived { get; set; }

        /// <summary>
        /// Gets or sets ......
        /// </summary>
        EventHandler<string> ConnectionStatusChanged { get; set; }

        /// <summary>
        /// ........
        /// </summary>
        void Start();

        /// <summary>
        ///  ......
        /// </summary>
        void Stop();
    }
}