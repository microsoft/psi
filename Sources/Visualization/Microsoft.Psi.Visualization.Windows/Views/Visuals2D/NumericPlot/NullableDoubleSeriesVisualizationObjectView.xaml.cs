// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableDoubleSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableDoubleSeriesVisualizationObjectView : NullableDoubleSeriesVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableDoubleSeriesVisualizationObjectView"/> class.
        /// </summary>
        public NullableDoubleSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
