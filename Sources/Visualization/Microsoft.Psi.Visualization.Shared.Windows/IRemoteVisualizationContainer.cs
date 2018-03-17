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
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a remote visualization container.
    /// </summary>
    [Guid(Guids.IRemoteVisualizationContainerIIDString)]
#if COM_SERVER
    [ComVisible(true)]
#elif COM_CLIENT
    [ComImport]
#endif
    public interface IRemoteVisualizationContainer
    {
        /// <summary>
        /// Gets or sets the current visualization panel.
        /// </summary>
        IRemoteVisualizationPanel CurrentPanel { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the navigator.
        /// </summary>
        IRemoteNavigator Navigator { get; }

        /// <summary>
        /// Adds a new visualization panel of the indicated type.
        /// </summary>
        /// <param name="type">Type of new visualization panel.</param>
        /// <returns>An instance of the new visualization panel.</returns>
        IRemoteVisualizationPanel AddPanel(string type);

        /// <summary>
        /// Clears the visualization container.
        /// </summary>
        void Clear();

        /// <summary>
        /// Removes the indicated panel.
        /// </summary>
        /// <param name="panel">The visualization panel to remove.</param>
        void RemovePanel(IRemoteVisualizationPanel panel);
    }
}
