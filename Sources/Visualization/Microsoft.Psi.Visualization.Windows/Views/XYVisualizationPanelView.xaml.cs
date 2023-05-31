// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
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
    }
}
