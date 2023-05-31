// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for IntSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class IntSeriesVisualizationObjectView : PlotSeriesVisualizationObjectView<IntSeriesVisualizationObject, string, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntSeriesVisualizationObjectView"/> class.
        /// </summary>
        public IntSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
