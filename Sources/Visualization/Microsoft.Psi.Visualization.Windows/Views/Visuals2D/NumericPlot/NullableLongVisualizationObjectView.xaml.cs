// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    /// <summary>
    /// Interaction logic for NullableLongVisualizationObjectView.xaml.
    /// </summary>
    public partial class NullableLongVisualizationObjectView : NullableLongVisualizationObjectViewBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableLongVisualizationObjectView"/> class.
        /// </summary>
        public NullableLongVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
