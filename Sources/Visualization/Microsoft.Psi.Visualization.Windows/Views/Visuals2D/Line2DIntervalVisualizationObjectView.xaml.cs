// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for Line2DIntervalVisualizationObjectView.xaml.
    /// </summary>
    public partial class Line2DIntervalVisualizationObjectView : Line2DIntervalVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Line2DIntervalVisualizationObjectView"/> class.
        /// </summary>
        public Line2DIntervalVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
