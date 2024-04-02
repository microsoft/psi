// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for LabeledPointIntervalVisualizationObjectView.xaml.
    /// </summary>
    public partial class LabeledPointIntervalVisualizationObjectView : LabeledPointIntervalVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledPointIntervalVisualizationObjectView"/> class.
        /// </summary>
        public LabeledPointIntervalVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
