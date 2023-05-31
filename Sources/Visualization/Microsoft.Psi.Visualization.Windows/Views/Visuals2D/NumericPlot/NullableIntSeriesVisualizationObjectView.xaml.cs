// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableIntSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableIntSeriesVisualizationObjectView : NullableIntSeriesVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableIntSeriesVisualizationObjectView"/> class.
        /// </summary>
        public NullableIntSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
