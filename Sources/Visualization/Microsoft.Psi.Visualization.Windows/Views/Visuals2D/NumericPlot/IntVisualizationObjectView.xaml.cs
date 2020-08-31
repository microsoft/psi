// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for IntVisualizationObjectView.xaml.
    /// </summary>
    public partial class IntVisualizationObjectView : PlotVisualizationObjectView<IntVisualizationObject, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntVisualizationObjectView"/> class.
        /// </summary>
        public IntVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
