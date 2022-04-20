// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.Generic;
    using System.Windows.Controls;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for XYVisualizationPanelView.xaml.
    /// </summary>
    public partial class XYVisualizationPanelView : InstantVisualizationPanelView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XYVisualizationPanelView"/> class.
        /// </summary>
        public XYVisualizationPanelView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the visualization panel.
        /// </summary>
        protected XYVisualizationPanel VisualizationPanel => (XYVisualizationPanel)this.DataContext;

        /// <inheritdoc/>
        public override void AppendContextMenuItems(List<MenuItem> menuItems)
        {
            menuItems.Add(MenuItemHelper.CreateMenuItem(
                null,
                "Auto-Fit Axes",
                this.VisualizationPanel.SetAutoAxisComputeModeCommand,
                null,
                this.VisualizationPanel.AxisComputeMode == AxisComputeMode.Manual));

            base.AppendContextMenuItems(menuItems);
        }
    }
}
