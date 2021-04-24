// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for ScatterRectangleVisualizationObjectView.xaml.
    /// </summary>
    public partial class LabeledRectangleListVisualizationObjectView : LabeledRectangleListVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledRectangleListVisualizationObjectView"/> class.
        /// </summary>
        public LabeledRectangleListVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
