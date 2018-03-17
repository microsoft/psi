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
    /// Represents a remote visualization service.
    /// </summary>
    [Guid(Guids.IRemoteVisualizationServiceIIDString)]
#if COM_SERVER
    [ComVisible(true)]
#elif COM_CLIENT
    [ComImport]
#endif
    public interface IRemoteVisualizationService
    {
        /// <summary>
        /// Gets the visualization container.
        /// </summary>
        IRemoteVisualizationContainer CurrentContainer { get; }

        /// <summary>
        /// Ensure the specified stream is currently open. If stream is not open, it will be upon retun.
        /// </summary>
        /// <param name="jsonStreamBinding">Stream binding, JSON serialized, inidicting which stream to open</param>
        void EnsureBinding(string jsonStreamBinding);
    }
}
