// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Represents the base class for all instant visualization panel views.
    /// </summary>
    public abstract class InstantVisualizationPanelView : VisualizationPanelView
    {
        /// <inheritdoc/>
        public override void AppendContextMenuItems(List<MenuItem> menuItems)
        {
            if (this.DataContext is InstantVisualizationPanel visualizationPanel)
            {
                // Add Set Cursor Epsilon menu with sub-menu items
                var setCursorEpsilonMenuItem = MenuItemHelper.CreateMenuItem(
                    string.Empty,
                    "Set Default Cursor Epsilon",
                    null);

                _ = setCursorEpsilonMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Infinite Past",
                        new RelayCommand(() => this.UpdateDefaultCursorEpsilon(visualizationPanel, "Infinite Past", int.MaxValue, 0), true)));
                setCursorEpsilonMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Last 5 seconds",
                        new RelayCommand(() => this.UpdateDefaultCursorEpsilon(visualizationPanel, "Last 5 seconds", 5000, 0), true)));
                setCursorEpsilonMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Last 1 second",
                        new RelayCommand(() => this.UpdateDefaultCursorEpsilon(visualizationPanel, "Last 1 second", 1000, 0), true)));
                setCursorEpsilonMenuItem.Items.Add(
                    MenuItemHelper.CreateMenuItem(
                        null,
                        "Last 50 milliseconds",
                        new RelayCommand(() => this.UpdateDefaultCursorEpsilon(visualizationPanel, "Last 50 milliseconds", 50, 0), true)));

                menuItems.Add(setCursorEpsilonMenuItem);
            }

            base.AppendContextMenuItems(menuItems);
        }

        private void UpdateDefaultCursorEpsilon(InstantVisualizationPanel visualizationPanel, string name, int negMs, int posMs)
        {
            visualizationPanel.DefaultCursorEpsilonNegMs = negMs;
            visualizationPanel.DefaultCursorEpsilonPosMs = posMs;

            var anyVisualizersWithDifferentCursorEpsilon =
                visualizationPanel.VisualizationObjects.Any(vo => vo.CursorEpsilonNegMs != 50 || vo.CursorEpsilonPosMs != 0);

            if (anyVisualizersWithDifferentCursorEpsilon)
            {
                var result = new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Update visualizers?",
                    $"Some of the visualizers in this panel have a cursor epsilon that is different from {name}. Would you also like to change the cursor epsilon for these visualizers to {name}?",
                    "Yes",
                    "No").ShowDialog();
                if (result == true)
                {
                    foreach (var vo in visualizationPanel.VisualizationObjects)
                    {
                        vo.CursorEpsilonNegMs = negMs;
                        vo.CursorEpsilonPosMs = posMs;
                    }
                }
            }
        }
    }
}
