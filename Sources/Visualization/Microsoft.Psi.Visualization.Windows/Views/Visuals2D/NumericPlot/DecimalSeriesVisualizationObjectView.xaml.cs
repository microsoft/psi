// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for DecimalSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class DecimalSeriesVisualizationObjectView : PlotSeriesVisualizationObjectView<DecimalSeriesVisualizationObject, string, decimal>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalSeriesVisualizationObjectView"/> class.
        /// </summary>
        public DecimalSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
