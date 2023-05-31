// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableBoolVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableBoolVisualizationObjectView : NullableBoolVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableBoolVisualizationObjectView"/> class.
        /// </summary>
        public NullableBoolVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
