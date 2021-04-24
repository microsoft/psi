// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents the base class for all visualization panel views.
    /// </summary>
    public abstract class VisualizationPanelView : UserControl, IContextMenuItemsSource
    {
        /// <inheritdoc/>
        public ContextMenuItemsSourceType ContextMenuItemsSourceType => ContextMenuItemsSourceType.VisualizationPanel;

        /// <inheritdoc/>
        public string ContextMenuObjectName => string.Empty;

        /// <inheritdoc/>
        public virtual void AppendContextMenuItems(List<MenuItem> menuItems)
        {
            if (this.DataContext is VisualizationPanel visualizationPanel)
            {
                if (visualizationPanel.VisualizationObjects.Count > 0)
                {
                    var visible = visualizationPanel.VisualizationObjects.Any(vo => vo.Visible);
                    menuItems.Add(MenuItemHelper.CreateMenuItem(
                        IconSourcePath.ToggleVisibility,
                        visible ? "Hide All Visualizers" : "Show All Visualizers",
                        visualizationPanel.ToggleAllVisualizersVisibilityCommand,
                        null,
                        true));
                }

                menuItems.Add(MenuItemHelper.CreateMenuItem(
                    IconSourcePath.ClearPanel,
                    $"Remove All Visualizers",
                    visualizationPanel.ClearPanelCommand,
                    null,
                    visualizationPanel.VisualizationObjects.Count > 0));
            }
        }
    }
}
