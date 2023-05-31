// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableDoubleVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableDoubleVisualizationObjectView : NullableDoubleVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableDoubleVisualizationObjectView"/> class.
        /// </summary>
        public NullableDoubleVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
