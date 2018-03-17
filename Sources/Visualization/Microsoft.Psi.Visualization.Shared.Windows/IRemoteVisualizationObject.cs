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
    /// Represents a remote visualization object.
    /// </summary>
    [Guid(Guids.IRemoteVisualizationObjectIIDString)]
#if COM_SERVER
    [ComVisible(true)]
#elif COM_CLIENT
    [ComImport]
#endif
    public interface IRemoteVisualizationObject
    {
        /// <summary>
        /// Gets the navigator.
        /// </summary>
        IRemoteNavigator Navigator { get; }

        /// <summary>
        /// Gets the visualization container.
        /// </summary>
        IRemoteVisualizationContainer Container { get; }

        /// <summary>
        /// Gets the visualization panel.
        /// </summary>
        IRemoteVisualizationPanel Panel { get; }

        /// <summary>
        /// Adds advise to this remote visualization object.
        /// </summary>
        /// <param name="notifyVisualizationObjectChanged">Advise object to add.</param>
        /// <returns>Advise cookie for unadvising.</returns>
        uint Advise(INotifyRemoteConfigurationChanged notifyVisualizationObjectChanged);

        /// <summary>
        /// Brings this remote visualization object to the top of z-order within its containing panel.
        /// </summary>
        void BringToFront();

        /// <summary>
        /// Closes and removes this remote visualization object.
        /// </summary>
        void Close();

        /// <summary>
        /// Gets this remote visualization object's configuration as a JSON string.
        /// </summary>
        /// <returns>The configuration serialized into a JSON string.</returns>
        string GetConfiguration();

        /// <summary>
        /// Sends this remote visualization object to the back of z-order within its containing panel.
        /// </summary>
        void SendToBack();

        /// <summary>
        /// Sets this remove visualization object's configuration.
        /// </summary>
        /// <param name="jsonConfiguration">Configuration serialized as a JSON string.</param>
        void SetConfiguration(string jsonConfiguration);

        /// <summary>
        /// Removes advise from this remote visualization object.
        /// </summary>
        /// <param name="cookie">Advise cookie given when advising.</param>
        void Unadvise(uint cookie);
    }
}