// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// MFT_OUTPUT_DATA_BUFFER structure (defined in Mftransform.h).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MFTOutputDataBuffer
    {
        /// <summary>
        /// dwStreamID.
        /// </summary>
        internal int StreamID;

        /// <summary>
        /// pSample.
        /// </summary>
        internal IMFSample Sample;

        /// <summary>
        /// dwStatus.
        /// </summary>
        internal int Status;

        /// <summary>
        /// pEvents.
        /// </summary>
        internal IMFCollection Events;
    }
}