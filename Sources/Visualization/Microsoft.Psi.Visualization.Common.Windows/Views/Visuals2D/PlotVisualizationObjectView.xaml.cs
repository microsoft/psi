// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for PlotVisualizationObjectView.xaml.
    /// </summary>
    public partial class PlotVisualizationObjectView : PlotVisualizationObjectView<PlotVisualizationObject, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlotVisualizationObjectView"/> class.
        /// </summary>
        public PlotVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
