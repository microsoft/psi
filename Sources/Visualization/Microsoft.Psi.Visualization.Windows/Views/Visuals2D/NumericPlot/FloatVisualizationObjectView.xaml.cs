// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for FloatVisualizationObjectView.xaml.
    /// </summary>
    public partial class FloatVisualizationObjectView : PlotVisualizationObjectView<FloatVisualizationObject, float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatVisualizationObjectView"/> class.
        /// </summary>
        public FloatVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
