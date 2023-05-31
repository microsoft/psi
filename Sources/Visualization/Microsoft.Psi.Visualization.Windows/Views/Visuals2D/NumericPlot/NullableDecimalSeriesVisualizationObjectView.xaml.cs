// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableDecimalSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableDecimalSeriesVisualizationObjectView : NullableDecimalSeriesVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableDecimalSeriesVisualizationObjectView"/> class.
        /// </summary>
        public NullableDecimalSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
