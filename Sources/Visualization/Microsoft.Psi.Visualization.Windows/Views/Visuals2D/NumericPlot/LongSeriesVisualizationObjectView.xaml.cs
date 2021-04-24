// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for LongSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class LongSeriesVisualizationObjectView : PlotSeriesVisualizationObjectView<LongSeriesVisualizationObject, string, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LongSeriesVisualizationObjectView"/> class.
        /// </summary>
        public LongSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
