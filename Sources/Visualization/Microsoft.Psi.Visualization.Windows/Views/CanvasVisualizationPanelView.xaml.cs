// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Interaction logic for CanvasVisualizationPanelView.xaml.
    /// </summary>
    public partial class CanvasVisualizationPanelView : InstantVisualizationPanelView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CanvasVisualizationPanelView"/> class.
        /// </summary>
        public CanvasVisualizationPanelView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets the visualization panel.
        /// </summary>
        protected CanvasVisualizationPanel VisualizationPanel => (CanvasVisualizationPanel)this.DataContext;
    }
}
