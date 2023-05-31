// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableDecimalVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableDecimalVisualizationObjectView : NullableDecimalVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableDecimalVisualizationObjectView"/> class.
        /// </summary>
        public NullableDecimalVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
