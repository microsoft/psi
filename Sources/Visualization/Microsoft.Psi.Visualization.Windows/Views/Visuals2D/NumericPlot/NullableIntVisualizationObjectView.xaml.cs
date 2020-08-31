// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableIntVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableIntVisualizationObjectView : NullableIntVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableIntVisualizationObjectView"/> class.
        /// </summary>
        public NullableIntVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
