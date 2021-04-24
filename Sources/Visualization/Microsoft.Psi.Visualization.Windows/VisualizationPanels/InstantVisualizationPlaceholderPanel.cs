// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;

    /// <summary>
    /// Represents a placeholder for another instant visualization panel.
    /// </summary>
    public class InstantVisualizationPlaceholderPanel : VisualizationPanel
    {
        private readonly InstantVisualizationContainer instantVisualizationContainer;
        private int relativeWidth = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationPlaceholderPanel"/> class.
        /// </summary>
        /// <param name="instantVisualizationContainer">The parent instant visualization container.</param>
        public InstantVisualizationPlaceholderPanel(InstantVisualizationContainer instantVisualizationContainer)
        {
            this.instantVisualizationContainer = instantVisualizationContainer;
            this.Name = "-Empty-";
        }

        /// <summary>
        /// Gets the remove cell command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand RemoveCellCommand => new RelayCommand(() => this.instantVisualizationContainer.CreateRemoveCellCommand(this));

        /// <summary>
        /// Gets or sets the name of the relative width for the panel.
        /// </summary>
        [DataMember]
        [Description("The relative width for the panel.")]
        public int RelativeWidth
        {
            get { return this.relativeWidth; }
            set { this.Set(nameof(this.RelativeWidth), ref this.relativeWidth, value); }
        }

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new List<VisualizationPanelType>() { VisualizationPanelType.XY, VisualizationPanelType.XYZ };

        /// <inheritdoc/>
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(InstantVisualizationPlaceholderPanelView));
        }
    }
}
