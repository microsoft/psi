// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;

    /// <summary>
    /// Represents a placeholder for another instant visualization panel.
    /// </summary>
    public class InstantVisualizationPlaceholderPanel : VisualizationPanel
    {
        /// <inheritdoc/>
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(InstantVisualizationPlaceholderPanelView));
        }
    }
}
