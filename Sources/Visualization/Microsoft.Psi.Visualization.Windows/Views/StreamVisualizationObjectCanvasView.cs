// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provides an abstract base class for a canvas-based view of a <see cref="StreamVisualizationObject{TData}"/>.
    /// </summary>
    /// <typeparam name="TStreamVisualizationObject">The type of stream visualization object.</typeparam>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    /// <remarks>
    /// Canvas-based views contain a single Canvas object. The view contains a scale and a translate transform
    /// and various UI elements can be added to the list of children for the canvas programmatically.
    ///
    /// This base abstract class defines virtual methods for responding to resizing events,
    /// updating the set of scaling and translation transforms and updating the view.
    /// </remarks>
    public abstract class StreamVisualizationObjectCanvasView<TStreamVisualizationObject, TData> : VisualizationObjectView
        where TStreamVisualizationObject : StreamVisualizationObject<TData>, new()
    {
        private readonly ScaleTransform scaleTransform = new ();
        private readonly TranslateTransform translateTransform = new ();
        private readonly TransformGroup transformGroup = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamVisualizationObjectCanvasView{TVisualizationObject, TData}"/> class.
        /// </summary>
        public StreamVisualizationObjectCanvasView()
        {
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
        /// Gets the stream visualization object.
        /// </summary>
        public virtual TStreamVisualizationObject StreamVisualizationObject => this.VisualizationObject as TStreamVisualizationObject;

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.StreamVisualizationObject.CurrentValue))
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
            this.UpdateView();
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
        /// Implements changes in response to current value changes.
        /// </summary>
        protected virtual void OnCurrentValueChanged()
        {
        }

        /// <summary>
        /// Update the view.
        /// </summary>
        protected abstract void UpdateView();
    }
}
