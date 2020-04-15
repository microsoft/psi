// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;

    /// <summary>
    /// Represents a visualization panel factory.
    /// </summary>
    public static class VisualizationPanelFactory
    {
        /// <summary>
        /// Creates a new visualization panel.
        /// </summary>
        /// <param name="visualizationPanelType">The type of visualization panel to create.</param>
        /// <returns>A new visualization panel.</returns>
        public static VisualizationPanel CreateVisualizationPanel(VisualizationPanelType visualizationPanelType)
        {
            switch (visualizationPanelType)
            {
                case VisualizationPanelType.Timeline:
                    return Activator.CreateInstance<TimelineVisualizationPanel>();
                case VisualizationPanelType.XY:
                    return Activator.CreateInstance<XYVisualizationPanel>();
                case VisualizationPanelType.XYZ:
                    return Activator.CreateInstance<XYZVisualizationPanel>();
                default:
                    throw new ArgumentException(string.Format("Unknown visualiation panel type {0}.", visualizationPanelType.ToString()));
            }
        }
    }
}