// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if COM_SERVER
namespace Microsoft.Psi.Visualization.Server
#elif COM_CLIENT
namespace Microsoft.Psi.Visualization.Client
#endif
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents remote navigation modes.
    /// </summary>
#if COM_SERVER
    [ComVisible(true)]
#endif
    public enum RemoteNavigationMode
    {
        /// <summary>
        /// Live navigation mode.
        /// </summary>
        Live,

        /// <summary>
        /// Playback navigation mode.
        /// </summary>
        Playback
    }
}