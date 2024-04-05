// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for Line2DListVisualizationObjectView.xaml.
    /// </summary>
    public partial class Line2DListVisualizationObjectView : Line2DListVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Line2DListVisualizationObjectView"/> class.
        /// </summary>
        public Line2DListVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
