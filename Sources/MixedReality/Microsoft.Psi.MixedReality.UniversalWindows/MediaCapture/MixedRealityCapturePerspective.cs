// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.MediaCapture
{
    /// <summary>
    /// Enumeration which indicates the perspective from which holograms are rendered in the mixed-reality image.
    /// See https://docs.microsoft.com/en-us/windows/mixed-reality/develop/platform-capabilities-and-apis/mixed-reality-capture-for-developers for details.
    /// </summary>
    public enum MixedRealityCapturePerspective
    {
        /// <summary>
        /// Screen perspective.
        /// </summary>
        Display = 0,

        /// <summary>
        /// Photo video camera perspective.
        /// </summary>
        PhotoVideoCamera = 1,
    }
}
