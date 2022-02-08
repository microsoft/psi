// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableLongSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableLongSeriesVisualizationObjectView : NullableLongSeriesVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableLongSeriesVisualizationObjectView"/> class.
        /// </summary>
        public NullableLongSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
