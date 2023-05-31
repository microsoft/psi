// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio.ComInterop
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Audio client properties (defined in AudioClient.h).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct AudioClientProperties
    {
        /// <summary>
        /// cbSize.
        /// </summary>
        public int Size;

        /// <summary>
        /// bIsOffload.
        /// </summary>
        public bool IsOffload;

        /// <summary>
        /// eCategory.
        /// </summary>
        public AudioStreamCategory Category;

        /// <summary>
        /// Options.
        /// </summary>
        public AudioClientStreamOptions Options;
    }
}