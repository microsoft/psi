// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableBoolSeriesVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableBoolSeriesVisualizationObjectView : NullableBoolSeriesVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableBoolSeriesVisualizationObjectView"/> class.
        /// </summary>
        public NullableBoolSeriesVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
