// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    /// <summary>
    /// AudioClientStreamOptions enumeration (defined in AudioClient.h).
    /// </summary>
    internal enum AudioClientStreamOptions
    {
        /// <summary>
        /// AUDCLNT_STREAMOPTIONS_NONE
        /// </summary>
        None = 0,

        /// <summary>
        /// AUDCLNT_STREAMOPTIONS_RAW
        /// </summary>
        Raw = 0x1,

        /// <summary>
        /// AUDCLNT_STREAMOPTIONS_MATCH_FORMAT
        /// </summary>
        MatchFormat = 0x2,
    }
}