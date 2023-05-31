// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for LongVisualizationObjectView.xaml.
    /// </summary>
    public partial class LongVisualizationObjectView : PlotVisualizationObjectView<LongVisualizationObject, long>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LongVisualizationObjectView"/> class.
        /// </summary>
        public LongVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
