// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that 2D visualizers can be rendered in.
    /// </summary>
    public class CanvasVisualizationPanel : VisualizationPanel
    {
        private int relativeWidth = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="CanvasVisualizationPanel"/> class.
        /// </summary>
        public CanvasVisualizationPanel()
        {
            this.Name = "Canvas Panel";
        }

        /// <summary>
        /// Gets or sets the name of the relative width for the panel.
        /// </summary>
        [DataMember]
        [PropertyOrder(7)]
        [Description("The relative width for the panel.")]
        public int RelativeWidth
        {
            get { return this.relativeWidth; }
            set { this.Set(nameof(this.RelativeWidth), ref this.relativeWidth, value); }
        }

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new () { VisualizationPanelType.Canvas };

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
            => XamlHelper.CreateTemplate(this.GetType(), typeof(CanvasVisualizationPanelView));
    }
}