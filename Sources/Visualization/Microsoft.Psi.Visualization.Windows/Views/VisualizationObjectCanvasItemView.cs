// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.ObjectModel;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides a base class for individual canvas items views.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public abstract class VisualizationObjectCanvasItemView<TItem>
        : IVisualizationObjectCanvasItemView<TItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObjectCanvasItemView{TItem}"/> class.
        /// </summary>
        public VisualizationObjectCanvasItemView()
        {
        }

        /// <summary>
        /// Gets the list of UI elements forming this view item.
        /// </summary>
        public ObservableCollection<UIElement> UIElements { get; } = new ObservableCollection<UIElement>();

        /// <summary>
        /// Configures.
        /// </summary>
        /// <param name="canvasView">The canvas view.</param>
        /// <param name="visualizationObject">The stream visualization object.</param>
        public abstract void Configure(IStreamVisualizationObjectCanvasView canvasView, VisualizationObject visualizationObject);

        /// <summary>
        /// Updaets the view.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="canvasView">The canvas view.</param>
        public abstract void UpdateView(TItem item, IStreamVisualizationObjectCanvasView canvasView);
    }
}