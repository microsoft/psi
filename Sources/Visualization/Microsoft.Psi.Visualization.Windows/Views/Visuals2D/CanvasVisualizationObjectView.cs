// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Base class for 2D canvas-based visualization object views.
    /// </summary>
    /// <typeparam name="TStreamVisualizationObject">The type of visualization object configuration.</typeparam>
    /// <typeparam name="TData">The type of data.</typeparam>
    public class CanvasVisualizationObjectView<TStreamVisualizationObject, TData> : UserControl
        where TStreamVisualizationObject : StreamVisualizationObject<TData>, new()
    {
        private readonly ScaleTransform scaleTransform = new ScaleTransform();
        private readonly TranslateTransform translateTransform = new TranslateTransform();
        private readonly TransformGroup transformGroup = new TransformGroup();

        /// <summary>
        /// Initializes a new instance of the <see cref="CanvasVisualizationObjectView{TVisualizationObject, TData}"/> class.
        /// </summary>
        public CanvasVisualizationObjectView()
        {
            this.DataContextChanged += this.OnDataContextChanged;
            this.SizeChanged += this.OnSizeChanged;

            this.transformGroup.Children.Add(this.translateTransform);
            this.transformGroup.Children.Add(this.scaleTransform);
        }

        /// <summary>
        /// Gets or sets the dynamic canvas element.
        /// </summary>
        public Canvas Canvas { get; protected set; }

        /// <summary>
        /// Gets the transform group.
        /// </summary>
        public TransformGroup TransformGroup => this.transformGroup;

        /// <summary>
        /// Gets the scale transform.
        /// </summary>
        public ScaleTransform ScaleTransform => this.scaleTransform;

        /// <summary>
        /// Gets the translate transform.
        /// </summary>
        public TranslateTransform TranslateTransform => this.translateTransform;

        /// <summary>
        /// Gets or sets the stream visualization object.
        /// </summary>
        public virtual TStreamVisualizationObject VisualizationObject { get; protected set; }

        /// <summary>
        /// Implements changes in response to data context changing.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.VisualizationObject = this.DataContext as TStreamVisualizationObject;

            if (this.VisualizationObject == null)
            {
                throw new InvalidCastException("The data context is not a stream visualization object.");
            }

            // check that the visualization object is connected
            if (!this.VisualizationObject.IsConnected)
            {
                throw new Exception("Visualization object should be connected by the time the view is attached.");
            }

            // setup handlers for properties changing
            this.VisualizationObject.PropertyChanging += this.OnVisualizationObjectPropertyChanging;
            this.VisualizationObject.PropertyChanged += this.OnVisualizationObjectPropertyChanged;

            // if we have data, setup handlers for data changes
            this.OnDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (this.VisualizationObject.Data != null)
            {
                this.VisualizationObject.Data.DetailedCollectionChanged += this.OnDataCollectionChanged;
            }
        }

        /// <summary>
        /// Implements changes in response to visualization object property changing.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnVisualizationObjectPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(this.VisualizationObject.IsConnected))
            {
                // if the IsConnected property is changing, that means the visualization object is being disconnected from the
                // panel, so detach all handlers
                this.VisualizationObject.PropertyChanging -= this.OnVisualizationObjectPropertyChanging;
                this.VisualizationObject.PropertyChanged -= this.OnVisualizationObjectPropertyChanged;
                if (this.VisualizationObject.Data != null)
                {
                    this.VisualizationObject.Data.DetailedCollectionChanged -= this.OnDataCollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.VisualizationObject.Data))
            {
                // If the data is about to change, detach the handlers
                if (this.VisualizationObject.Data != null)
                {
                    this.VisualizationObject.Data.DetailedCollectionChanged -= this.OnDataCollectionChanged;
                }
            }
        }

        /// <summary>
        /// Implements changes in response to data context changed.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.VisualizationObject.Data))
            {
                this.OnDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.VisualizationObject.Data != null)
                {
                    this.VisualizationObject.Data.DetailedCollectionChanged += this.OnDataCollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.VisualizationObject.CurrentValue))
            {
                this.OnCurrentValueChanged();
            }
        }

        /// <summary>
        /// Implements changes in response to the size of the control changing.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.UpdateTransforms())
            {
                this.OnTransformsChanged();
            }
        }

        /// <summary>
        /// Implements changes in response to a change in the transforms.
        /// </summary>
        protected virtual void OnTransformsChanged()
        {
        }

        /// <summary>
        /// Updates the transforms.
        /// </summary>
        /// <returns>Returns true if the transforms have been modified.</returns>
        protected virtual bool UpdateTransforms()
        {
            double oldScaleX = this.ScaleTransform.ScaleX;
            double oldScaleY = this.ScaleTransform.ScaleY;

            this.ScaleTransform.ScaleX = this.Canvas.ActualWidth;
            this.ScaleTransform.ScaleY = this.Canvas.ActualHeight;

            return oldScaleX != this.ScaleTransform.ScaleX || oldScaleY != this.ScaleTransform.ScaleY;
        }

        /// <summary>
        /// Implements changes in response to data collection changed.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }

        /// <summary>
        /// Implements changes in response to current value changes.
        /// </summary>
        protected virtual void OnCurrentValueChanged()
        {
        }
    }
}
