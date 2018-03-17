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
    /// Represents a remote visualization panel.
    /// </summary>
    [Guid(Guids.IRemoteVisualizationPanelIIDString)]
#if COM_SERVER
    [ComVisible(true)]
#elif COM_CLIENT
    [ComImport]
#endif
    public interface IRemoteVisualizationPanel
    {
        /// <summary>
        /// Gets the visualization container.
        /// </summary>
        IRemoteVisualizationContainer Container { get; }

        /// <summary>
        /// Gets or sets the current visualization object.
        /// </summary>
        IRemoteVisualizationObject CurrentVisualizationObject { get; set; }

        /// <summary>
        /// Gets a value indicating whether this is the current panel.
        /// </summary>
        bool IsCurrentPanel { get; }

        /// <summary>
        /// Gets the navigator.
        /// </summary>
        IRemoteNavigator Navigator { get; }

        /// <summary>
        /// Adds a new visualization object of the indicated type.
        /// </summary>
        /// <param name="type">Type of new visualization object to add.</param>
        /// <returns>An instance of the new visualization object.</returns>
        IRemoteVisualizationObject AddVisualizationObject(string type);

        /// <summary>
        /// Adds a new visualization object of the indicated type from within the indicated assembly.
        /// </summary>
        /// <param name="assemblyPath">Assembly to find the type in.</param>
        /// <param name="type">Type of new visualization object to add.</param>
        /// <returns>An instance of the new visualization object.</returns>
        IRemoteVisualizationObject AddVisualizationObject(string assemblyPath, string type);

        /// <summary>
        /// Adds advise to this remote visualization panel.
        /// </summary>
        /// <param name="notifyVisualizationPanelChanged">Advise object to add.</param>
        /// <returns>Advise cookie for unadvising.</returns>
        uint Advise(INotifyRemoteConfigurationChanged notifyVisualizationPanelChanged);

        /// <summary>
        /// Brings the indictated remote visualization object to the front of z-order within this panel.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to move.</param>
        void BringToFront(IRemoteVisualizationObject visualizationObject);

        /// <summary>
        /// Clears the visualization panel.
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets this remote visualization panel's configuration as a JSON string.
        /// </summary>
        /// <returns>The configuration serialized into a JSON string.</returns>
        string GetConfiguration();

        /// <summary>
        /// Removes the indicated visualization object.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to remove.</param>
        void RemoveVisualizationObject(IRemoteVisualizationObject visualizationObject);

        /// <summary>
        /// Sends the indictated remote visualization object to the back of z-order within this panel.
        /// </summary>
        /// <param name="visualizationObject">The visualization object to move.</param>
        void SendToBack(IRemoteVisualizationObject visualizationObject);

        /// <summary>
        /// Sets this remove visualization panel's configuration.
        /// </summary>
        /// <param name="jsonConfiguration">Configuration serialized as a JSON string.</param>
        void SetConfiguration(string jsonConfiguration);

        /// <summary>
        /// Removes advise from this remote visualization panel.
        /// </summary>
        /// <param name="cookie">Advise cookie given when advising.</param>
        void Unadvise(uint cookie);
    }
}
