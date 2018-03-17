// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if COM_SERVER
namespace Microsoft.Psi.Visualization.Server
#elif COM_CLIENT
namespace Microsoft.Psi.Visualization.Client
#endif
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a notification event handler to be invoked when a configuration is changed.
    /// </summary>
    [Guid(Guids.INotifyRemoteConfigurationChangedIIDString)]
#if COM_SERVER
    [ComImport]
#elif COM_CLIENT
    [ComVisible(true)]
#endif
    public interface INotifyRemoteConfigurationChanged
    {
        /// <summary>
        /// Invoked by the remote host when a configuration changes.
        /// </summary>
        /// <param name="jsonConfiguration">The new configuration serialized into a JSON string.</param>
        void OnRemoteConfigurationChanged(string jsonConfiguration);
    }
}
