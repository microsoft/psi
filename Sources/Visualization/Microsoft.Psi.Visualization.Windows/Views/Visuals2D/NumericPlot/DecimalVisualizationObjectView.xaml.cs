// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for DecimalVisualizationObjectView.xaml.
    /// </summary>
    public partial class DecimalVisualizationObjectView : PlotVisualizationObjectView<DecimalVisualizationObject, decimal>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalVisualizationObjectView"/> class.
        /// </summary>
        public DecimalVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
