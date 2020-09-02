// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for DoubleVisualizationObjectView.xaml.
    /// </summary>
    public partial class DoubleVisualizationObjectView : PlotVisualizationObjectView<DoubleVisualizationObject, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleVisualizationObjectView"/> class.
        /// </summary>
        public DoubleVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
