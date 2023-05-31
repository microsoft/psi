// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for BoolVisualizationObjectView.xaml.
    /// </summary>
    public partial class BoolVisualizationObjectView : PlotVisualizationObjectView<BoolVisualizationObject, bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoolVisualizationObjectView"/> class.
        /// </summary>
        public BoolVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
