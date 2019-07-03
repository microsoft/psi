// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System;

    /// <summary>
    /// _AUDCLNT_BUFFERFLAGS enumeration (defined in AudioClient.h).
    /// </summary>
    [Flags]
    internal enum AudioClientBufferFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// AUDCLNT_BUFFERFLAGS_DATA_DISCONTINUITY
        /// </summary>
        DataDiscontinuity = 0x1,

        /// <summary>
        /// AUDCLNT_BUFFERFLAGS_SILENT
        /// </summary>
        Silent = 0x2,

        /// <summary>
        /// AUDCLNT_BUFFERFLAGS_TIMESTAMP_ERROR
        /// </summary>
        TimestampError = 0x4,
    }
}