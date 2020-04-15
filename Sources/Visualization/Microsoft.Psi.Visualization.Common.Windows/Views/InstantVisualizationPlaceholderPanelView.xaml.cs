// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for InstantVisualizationPlaceholderPanelView.xaml.
    /// </summary>
    public partial class InstantVisualizationPlaceholderPanelView : VisualizationPanelViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationPlaceholderPanelView"/> class.
        /// </summary>
        public InstantVisualizationPlaceholderPanelView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets ths visualization panel.
        /// </summary>
        protected InstantVisualizationPlaceholderPanel VisualizationPanel => (InstantVisualizationPlaceholderPanel)this.DataContext;
    }
}
