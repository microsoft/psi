// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableFloatSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableFloatSeriesVisualizationObjectView : NullableFloatSeriesVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableFloatSeriesVisualizationObjectView"/> class.
        /// </summary>
        public NullableFloatSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
