// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// MFT_INPUT_STREAM_INFO structure (defined in Mftransform.h).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MFTInputStreamInfo
    {
        /// <summary>
        /// hnsMaxLatency.
        /// </summary>
        internal long MaxLatency;

        /// <summary>
        /// dwFlags.
        /// </summary>
        internal int Flags;

        /// <summary>
        /// cbSize.
        /// </summary>
        internal int Size;

        /// <summary>
        /// cbMaxLookahead.
        /// </summary>
        internal int MaxLookahead;

        /// <summary>
        /// cbAlignment.
        /// </summary>
        internal int Alignment;
    }
}