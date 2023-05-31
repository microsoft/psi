// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for LabeledPointListVisualizationObjectView.xaml.
    /// </summary>
    public partial class LabeledPointListVisualizationObjectView : LabeledPointListVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledPointListVisualizationObjectView"/> class.
        /// </summary>
        public LabeledPointListVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
