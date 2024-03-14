// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Defines an interface for visualization object canvas item views.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public interface IVisualizationObjectCanvasItemView<TItem>
    {
        /// <summary>
        /// Gets the list of UI elements forming this item view.
        /// </summary>
        public ObservableCollection<UIElement> UIElements { get; }

        /// <summary>
        /// Configures.
        /// </summary>
        /// <param name="canvasView">The canvas view.</param>
        /// <param name="visualizationObject">The stream visualization object.</param>
        public void Configure(
            IStreamVisualizationObjectCanvasView canvasView,
            VisualizationObject visualizationObject);

        /// <summary>
        /// Updates the view.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="canvasView">The canvas view.</param>
        public abstract void UpdateView(TItem item, IStreamVisualizationObjectCanvasView canvasView);
    }
}
