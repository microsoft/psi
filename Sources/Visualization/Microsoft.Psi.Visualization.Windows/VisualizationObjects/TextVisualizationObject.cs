// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for strings.
    /// </summary>
    [VisualizationObject("Text")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class TextVisualizationObject : StreamValueVisualizationObject<string>
    {
        private Thickness margin = new (5, 0, 0, 0);

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TextVisualizationObjectView));

        /// <summary>
        /// Gets or sets the margin.
        /// </summary>
        [DataMember]
        [DisplayName("Margin")]
        [Description("The left, top, right and bottom margin in pixels.")]
        public Thickness Margin
        {
            get { return this.margin; }
            set { this.Set(nameof(this.Margin), ref this.margin, value); }
        }
    }
}
