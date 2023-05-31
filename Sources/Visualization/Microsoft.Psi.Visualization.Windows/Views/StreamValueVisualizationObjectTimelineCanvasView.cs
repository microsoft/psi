// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provide an abstract base class for timeline-canvas based views of a <see cref="StreamValueVisualizationObject{TData}"/>.
    /// </summary>
    /// <typeparam name="TStreamValueVisualizationObject">The type of stream value visualization object.</typeparam>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    public abstract class StreamValueVisualizationObjectTimelineCanvasView<TStreamValueVisualizationObject, TData> :
        StreamValueVisualizationObjectCanvasView<TStreamValueVisualizationObject, TData>
        where TStreamValueVisualizationObject : StreamValueVisualizationObject<TData>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamValueVisualizationObjectTimelineCanvasView{TStreamValueVisualizationObject, TData}"/> class.
        /// </summary>
        public StreamValueVisualizationObjectTimelineCanvasView()
        {
        }

        /// <inheritdoc/>
        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            // add a handler for view range changing on the navigator
            this.Navigator.ViewRange.RangeChanged += this.OnNavigatorViewRangeChanged;
            this.Navigator.DataRange.RangeChanged += this.OnNavigatorDataRangeChanged;
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.StreamValueVisualizationObject.IsConnected))
            {
                // if the IsConnected property is changing, that means the visualization object is being disconnected from the
                // panel, so detach all handlers
                this.Navigator.ViewRange.RangeChanged -= this.OnNavigatorViewRangeChanged;
                this.Navigator.DataRange.RangeChanged -= this.OnNavigatorDataRangeChanged;
            }
        }

        /// <summary>
        /// Implements changes in response to navigator view range changed.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnNavigatorViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            if (this.UpdateTransforms())
            {
                this.OnTransformsChanged();
            }
        }

        /// <summary>
        /// Implements changes in response to navigator data range changed.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnNavigatorDataRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            if (this.UpdateTransforms())
            {
                this.OnTransformsChanged();
            }
        }

        /// <inheritdoc/>
        protected override bool UpdateTransforms()
        {
            double oldScaleX = this.ScaleTransform.ScaleX;
            double oldScaleY = this.ScaleTransform.ScaleY;
            double oldTranslateX = this.TranslateTransform.X;

            var timeSpan = this.Navigator.ViewRange.Duration;
            this.ScaleTransform.ScaleX = this.Canvas.ActualWidth / timeSpan.TotalSeconds;
            this.ScaleTransform.ScaleY = this.Canvas.ActualHeight;
            this.TranslateTransform.X = -(this.Navigator.ViewRange.StartTime - this.Navigator.DataRange.StartTime).TotalSeconds;

            return oldScaleX != this.ScaleTransform.ScaleX || oldScaleY != this.ScaleTransform.ScaleY || oldTranslateX != this.TranslateTransform.X;
        }
    }
}
