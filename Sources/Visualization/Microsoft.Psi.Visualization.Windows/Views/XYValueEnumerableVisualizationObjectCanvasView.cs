// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Windows;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides a base abstract class for a canvas-based view of a <see cref="XYValueEnumerableVisualizationObject{TItem, TEnumerable}"/>.
    /// </summary>
    /// <typeparam name="TXYValueEnumerableVisualizationObject">The type of the XY-value enumerable visualization object.</typeparam>
    /// <typeparam name="TItem">The type of the items in the XY-value enumeration.</typeparam>
    /// <typeparam name="TEnumerable">The type of the enumeration of XY-values.</typeparam>
    /// <typeparam name="TItemView">The type of the view to use for individual items.</typeparam>
    public abstract class XYValueEnumerableVisualizationObjectCanvasView<TXYValueEnumerableVisualizationObject, TItem, TEnumerable, TItemView> :
        XYValueVisualizationObjectCanvasView<TXYValueEnumerableVisualizationObject, TEnumerable>
        where TEnumerable : IEnumerable<TItem>
        where TXYValueEnumerableVisualizationObject : XYValueEnumerableVisualizationObject<TItem, TEnumerable>, new()
        where TItemView : VisualizationObjectCanvasItemView<TXYValueEnumerableVisualizationObject, TItem, TEnumerable>, new()
    {
        private readonly List<TItemView> itemViews = new List<TItemView>();

        /// <summary>
        /// Initializes a new instance of the <see cref="XYValueEnumerableVisualizationObjectCanvasView{TXYVisualizationObject, TItem, TEnumerable, TViewItem}"/> class.
        /// </summary>
        public XYValueEnumerableVisualizationObjectCanvasView()
        {
        }

        /// <summary>
        /// Gets the XY-value enumeration visualization object.
        /// </summary>
        public TXYValueEnumerableVisualizationObject XYValueEnumerableVisualizationObject =>
            this.VisualizationObject as TXYValueEnumerableVisualizationObject;

        /// <summary>
        /// Update the view.
        /// </summary>
        protected override void UpdateView()
        {
            int itemsIndex = 0;
            if (this.StreamVisualizationObject.HasCurrentValue)
            {
                foreach (var item in this.StreamVisualizationObject.CurrentData)
                {
                    TItemView viewItem;
                    if (itemsIndex < this.itemViews.Count)
                    {
                        viewItem = this.itemViews[itemsIndex];
                    }
                    else
                    {
                        viewItem = new TItemView();

                        // add the UI elements to the canvas
                        foreach (var element in viewItem.UIElements)
                        {
                            this.Canvas.Children.Add(element);
                        }

                        // setup handling of collection change events
                        viewItem.UIElements.CollectionChanged += this.OnItemUIElementsCollectionChanged;

                        viewItem.Configure(this, this.XYValueEnumerableVisualizationObject);
                        this.itemViews.Add(viewItem);
                    }

                    viewItem.UpdateView(item, this, this.XYValueEnumerableVisualizationObject);
                    itemsIndex++;
                }
            }

            // remove the remaining figures
            for (int i = itemsIndex; i < this.itemViews.Count; i++)
            {
                while (this.itemViews.Count > itemsIndex)
                {
                    var item = this.itemViews[itemsIndex];
                    item.UIElements.CollectionChanged -= this.OnItemUIElementsCollectionChanged;
                    foreach (var element in item.UIElements)
                    {
                        this.Canvas.Children.Remove(element);
                    }

                    this.itemViews.Remove(item);
                }
            }
        }

        private void OnItemUIElementsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    this.Canvas.Children.Add(item as UIElement);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    this.Canvas.Children.Remove(item as UIElement);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}