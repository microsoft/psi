// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    /// <summary>
    /// MF_ATTRIBUTES_MATCH_TYPE enum (defined in mfobjects.h).
    /// </summary>
    internal enum MFAttributesMatchType
    {
        /// <summary>
        /// MF_ATTRIBUTES_MATCH_OUR_ITEMS = 0
        /// </summary>
        OutItems = 0,

        /// <summary>
        /// MF_ATTRIBUTES_MATCH_THEIR_ITEMS = 1
        /// </summary>
        TheirItems = 1,

        /// <summary>
        /// MF_ATTRIBUTES_MATCH_ALL_ITEMS = 2
        /// </summary>
        AllItems = 2,

        /// <summary>
        /// MF_ATTRIBUTES_MATCH_INTERSECTION = 3
        /// </summary>
        Intersection = 3,

        /// <summary>
        /// MF_ATTRIBUTES_MATCH_SMALLER = 4
        /// </summary>
        Smaller = 4,
    }
}