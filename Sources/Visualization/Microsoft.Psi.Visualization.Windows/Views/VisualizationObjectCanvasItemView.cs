// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides a base class for individual items views for a stream visualization object view.
    /// </summary>
    /// <typeparam name="TStreamVisualizationObject">The type of the stream visualization object.</typeparam>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <typeparam name="TEnumerable">The type of the enumeration of items.</typeparam>
    public abstract class VisualizationObjectCanvasItemView<TStreamVisualizationObject, TItem, TEnumerable>
        where TEnumerable : IEnumerable<TItem>
        where TStreamVisualizationObject : StreamVisualizationObject<TEnumerable>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObjectCanvasItemView{TStreamVisualizationObject, TItem, TEnumerable}"/> class.
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
        public abstract void Configure(
            StreamVisualizationObjectCanvasView<TStreamVisualizationObject, TEnumerable> canvasView,
            TStreamVisualizationObject visualizationObject);

        /// <summary>
        /// Updaets the view.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="canvasView">The canvas view.</param>
        /// <param name="visualizationObject">The stream visualization object.</param>
        public abstract void UpdateView(
            TItem item,
            StreamVisualizationObjectCanvasView<TStreamVisualizationObject, TEnumerable> canvasView,
            TStreamVisualizationObject visualizationObject);
    }
}