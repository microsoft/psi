// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;

    /// <summary>
    /// Represents a visualization panel that 2D visualizers can be rendered in.
    /// </summary>
    public class XYVisualizationPanel : VisualizationPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XYVisualizationPanel"/> class.
        /// </summary>
        public XYVisualizationPanel()
        {
            this.Name = "2D Panel";
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(XYVisualizationPanelView));
        }
    }
}