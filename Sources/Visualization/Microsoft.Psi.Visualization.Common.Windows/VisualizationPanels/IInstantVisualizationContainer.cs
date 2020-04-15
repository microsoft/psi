// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Represents an instant visualization container panel.
    /// </summary>
    public interface IInstantVisualizationContainer
    {
        /// <summary>
        /// Gets the visualization Panels.
        /// </summary>
        ObservableCollection<VisualizationPanel> Panels { get; }
    }
}