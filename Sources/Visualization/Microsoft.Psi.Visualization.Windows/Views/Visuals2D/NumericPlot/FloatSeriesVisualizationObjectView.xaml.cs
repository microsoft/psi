// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for FloatSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class FloatSeriesVisualizationObjectView : PlotSeriesVisualizationObjectView<FloatSeriesVisualizationObject, string, float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatSeriesVisualizationObjectView"/> class.
        /// </summary>
        public FloatSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
