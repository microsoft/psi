// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Windows.Media;

    /// <summary>
    /// represetns a visualization panel type attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class VisualizationPanelTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationPanelTypeAttribute"/> class.
        /// </summary>
        /// <param name="visualizationPanelType">The visualization panel type.</param>
        public VisualizationPanelTypeAttribute(VisualizationPanelType visualizationPanelType)
        {
            this.VisualizationPanelType = visualizationPanelType;
        }

        /// <summary>
        /// Gets the text of the command.
        /// </summary>
        public VisualizationPanelType VisualizationPanelType { get; private set; }
    }
}