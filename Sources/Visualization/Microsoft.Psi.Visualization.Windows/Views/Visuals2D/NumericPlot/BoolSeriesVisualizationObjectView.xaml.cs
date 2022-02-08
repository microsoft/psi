// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for BoolSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class BoolSeriesVisualizationObjectView : PlotSeriesVisualizationObjectView<BoolSeriesVisualizationObject, string, bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoolSeriesVisualizationObjectView"/> class.
        /// </summary>
        public BoolSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
