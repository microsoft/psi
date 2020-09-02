// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableFloatVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableFloatVisualizationObjectView : NullableFloatVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableFloatVisualizationObjectView"/> class.
        /// </summary>
        public NullableFloatVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
