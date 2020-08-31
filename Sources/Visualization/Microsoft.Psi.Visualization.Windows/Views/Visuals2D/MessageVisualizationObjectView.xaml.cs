// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for MessageVisualizationObjectView.xaml.
    /// </summary>
    public partial class MessageVisualizationObjectView : PlotVisualizationObjectView<MessageVisualizationObject, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageVisualizationObjectView"/> class.
        /// </summary>
        public MessageVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }
    }
}
