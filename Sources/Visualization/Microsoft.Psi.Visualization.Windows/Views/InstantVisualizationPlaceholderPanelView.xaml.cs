// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.Generic;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for InstantVisualizationPlaceholderPanelView.xaml.
    /// </summary>
    public partial class InstantVisualizationPlaceholderPanelView : VisualizationPanelView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationPlaceholderPanelView"/> class.
        /// </summary>
        public InstantVisualizationPlaceholderPanelView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the visualization panel.
        /// </summary>
        protected InstantVisualizationPlaceholderPanel VisualizationPanel => (InstantVisualizationPlaceholderPanel)this.DataContext;

        /// <inheritdoc/>
        public override void AppendContextMenuItems(List<MenuItem> menuItems)
        {
            // Placeholder objects are not deletable nor clearable, so do not call
            // the base class to have these menuitems added to the context menu.
        }
    }
}
