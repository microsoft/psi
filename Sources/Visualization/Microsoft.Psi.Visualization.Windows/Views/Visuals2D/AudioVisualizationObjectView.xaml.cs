// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for AudioVisualizationObjectView.xaml.
    /// </summary>
    public partial class AudioVisualizationObjectView : PlotVisualizationObjectView<AudioVisualizationObject, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioVisualizationObjectView"/> class.
        /// </summary>
        public AudioVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }

        /// <inheritdoc/>
        public override void AppendContextMenuItems(List<MenuItem> menuItems)
        {
            if (this.DataContext is AudioVisualizationObject audioVisualizationObject && audioVisualizationObject.IsBound)
            {
                menuItems.Add(MenuItemHelper.CreateMenuItem(audioVisualizationObject.ContextMenuIconSource, audioVisualizationObject.EnableAudioCommandText, audioVisualizationObject.EnableAudioCommand));
            }

            base.AppendContextMenuItems(menuItems);
        }
    }
}
