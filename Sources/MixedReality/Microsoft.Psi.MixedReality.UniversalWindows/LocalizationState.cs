// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    /// <summary>
    /// An enumeration of the possible mixed-reality localization states.
    /// </summary>
    public enum LocalizationState
    {
        /// <summary>
        /// No world spatial anchor was found.
        /// </summary>
        NotLocalized,

        /// <summary>
        /// Attempting to obtain and/or locate the world spatial anchor.
        /// </summary>
        Localizing,

        /// <summary>
        /// The world spatial anchor is valid in the current environment.
        /// </summary>
        Localized,

        /// <summary>
        /// The world spatial anchor was found to be invalid in the current environment.
        /// </summary>
        Invalidated,
    }
}
