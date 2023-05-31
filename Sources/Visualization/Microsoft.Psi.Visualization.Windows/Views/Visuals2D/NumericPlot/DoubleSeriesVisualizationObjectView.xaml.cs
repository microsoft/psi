// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for DoubleSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class DoubleSeriesVisualizationObjectView : PlotSeriesVisualizationObjectView<DoubleSeriesVisualizationObject, string, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleSeriesVisualizationObjectView"/> class.
        /// </summary>
        public DoubleSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
