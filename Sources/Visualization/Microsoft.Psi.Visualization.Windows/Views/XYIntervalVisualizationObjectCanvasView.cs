// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides an abstract base class for a canvas-based view of a <see cref="XYIntervalVisualizationObject{TData}"/>.
    /// </summary>
    /// <typeparam name="TXYIntervalVisualizationObject">The type of the XY value visualization object.</typeparam>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    /// <typeparam name="TItemView">The type of the view to use for individual items.</typeparam>
    public abstract class XYIntervalVisualizationObjectCanvasView<TXYIntervalVisualizationObject, TData, TItemView> :
        StreamIntervalVisualizationObjectCanvasView<TXYIntervalVisualizationObject, TData>
        where TXYIntervalVisualizationObject : XYIntervalVisualizationObject<TData>, new()
        where TItemView : IVisualizationObjectCanvasItemView<TData>, new()
    {
        private readonly List<TItemView> itemViews = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="XYIntervalVisualizationObjectCanvasView{TXYVisualizationObject, TData, TItemView}"/> class.
        /// </summary>
        public XYIntervalVisualizationObjectCanvasView()
            : base()
        {
            this.Padding = new Thickness(0, 0, 0, 0);
        }

        /// <summary>
        /// Gets the XY visualization object.
        /// </summary>
        public TXYIntervalVisualizationObject XYIntervalVisualizationObject =>
            this.VisualizationObject as TXYIntervalVisualizationObject;

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.XYIntervalVisualizationObject.XAxis) ||
                e.PropertyName == nameof(this.XYIntervalVisualizationObject.YAxis))
            {
                if (this.UpdateTransforms())
                {
                    this.OnTransformsChanged();
                }
            }
        }

        /// <inheritdoc/>
        protected override bool UpdateTransforms()
        {
            double oldScaleX = this.ScaleTransform.ScaleX;
            double oldScaleY = this.ScaleTransform.ScaleY;
            double oldTranslateX = this.TranslateTransform.X;
            double oldTranslateY = this.TranslateTransform.Y;

            if (this.XYIntervalVisualizationObject != null)
            {
                double contentWidth = this.XYIntervalVisualizationObject.XAxis.Maximum - this.XYIntervalVisualizationObject.XAxis.Minimum;
                if (contentWidth <= 0)
                {
                    contentWidth = 1;
                }

                double contentHeight = this.XYIntervalVisualizationObject.YAxis.Maximum - this.XYIntervalVisualizationObject.YAxis.Minimum;
                if (contentHeight <= 0)
                {
                    contentHeight = 1;
                }

                this.ScaleTransform.ScaleX = this.ActualWidth / contentWidth;
                this.TranslateTransform.X = -this.XYIntervalVisualizationObject.XAxis.Minimum;

                this.ScaleTransform.ScaleY = this.ActualHeight / contentHeight;
                this.TranslateTransform.Y = -this.XYIntervalVisualizationObject.YAxis.Minimum;
            }

            return
                oldScaleX != this.ScaleTransform.ScaleX ||
                oldScaleY != this.ScaleTransform.ScaleY ||
                oldTranslateX != this.TranslateTransform.X ||
                oldTranslateY != this.TranslateTransform.Y;
        }

        /// <summary>
        /// Update the view.
        /// </summary>
        protected override void UpdateView()
        {
            int itemsIndex = 0;
            if (this.XYIntervalVisualizationObject.Data != null)
            {
                foreach (var item in this.XYIntervalVisualizationObject.Data)
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

                        viewItem.Configure(this, this.XYIntervalVisualizationObject);
                        this.itemViews.Add(viewItem);
                    }

                    viewItem.UpdateView(item.Data, this);
                    itemsIndex++;
                }
            }
            else if (this.XYIntervalVisualizationObject.SummaryData != null)
            {
                foreach (var item in this.XYIntervalVisualizationObject.SummaryData)
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

                        viewItem.Configure(this, this.XYIntervalVisualizationObject);
                        this.itemViews.Add(viewItem);
                    }

                    viewItem.UpdateView(item.Value, this);
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
