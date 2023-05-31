// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Collections.Generic;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;

    /// <summary>
    /// Represents a visualization panel that instant visualizers can be rendered in.
    /// </summary>
    public class CanvasVisualizationPanel : InstantVisualizationPanel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CanvasVisualizationPanel"/> class.
        /// </summary>
        public CanvasVisualizationPanel()
        {
            this.Name = "Canvas Panel";
        }

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new () { VisualizationPanelType.Canvas };

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
            => XamlHelper.CreateTemplate(this.GetType(), typeof(CanvasVisualizationPanelView));
    }
}