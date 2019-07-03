// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    /// <summary>
    /// AudioClientShareMode enumeration (defined in AudioClient.h).
    /// </summary>
    internal enum AudioClientShareMode
    {
        /// <summary>
        /// AUDCLNT_SHAREMODE_SHARED
        /// </summary>
        Shared,

        /// <summary>
        /// AUDCLNT_SHAREMODE_EXCLUSIVE
        /// </summary>
        Exclusive,
    }
}