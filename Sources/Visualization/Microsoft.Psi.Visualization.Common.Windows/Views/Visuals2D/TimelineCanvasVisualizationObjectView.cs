// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Base class for 2D timeline canvas-based visualization object views.
    /// </summary>
    /// <typeparam name="TTimelineVisualizationObject">The type of timeline visualization object.</typeparam>
    /// <typeparam name="TData">The type of data.</typeparam>
    public class TimelineCanvasVisualizationObjectView<TTimelineVisualizationObject, TData> : CanvasVisualizationObjectView<TTimelineVisualizationObject, TData>
        where TTimelineVisualizationObject : TimelineVisualizationObject<TData>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineCanvasVisualizationObjectView{TVisualizationObject, TData}"/> class.
        /// </summary>
        public TimelineCanvasVisualizationObjectView()
        {
        }

        /// <summary>
        /// Gets the navigator for the visualization object.
        /// </summary>
        public Navigator Navigator => this.VisualizationObject.Navigator;

        /// <inheritdoc/>
        public override TTimelineVisualizationObject VisualizationObject { get; protected set; }

        /// <inheritdoc/>
        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            // add a handler for view range changing on the navigator
            this.Navigator.ViewRange.RangeChanged += this.OnNavigatorViewRangeChanged;

            // add a handler for summary data changes
            this.OnSummaryDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (this.VisualizationObject.SummaryData != null)
            {
                this.VisualizationObject.SummaryData.DetailedCollectionChanged += this.OnSummaryDataCollectionChanged;
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.VisualizationObject.IsConnected))
            {
                // if the IsConnected property is changing, that means the visualization object is being disconnected from the
                // panel, so detach all handlers
                this.Navigator.ViewRange.RangeChanged -= this.OnNavigatorViewRangeChanged;
                if (this.VisualizationObject.SummaryData != null)
                {
                    this.VisualizationObject.SummaryData.DetailedCollectionChanged -= this.OnSummaryDataCollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.VisualizationObject.SummaryData))
            {
                if (this.VisualizationObject.SummaryData != null)
                {
                    this.VisualizationObject.SummaryData.DetailedCollectionChanged -= this.OnSummaryDataCollectionChanged;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.VisualizationObject.SummaryData))
            {
                this.OnSummaryDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.VisualizationObject.SummaryData != null)
                {
                    this.VisualizationObject.SummaryData.DetailedCollectionChanged += this.OnSummaryDataCollectionChanged;
                }
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

        /// <summary>
        /// Implements changes in response to summary data collection changed.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSummaryDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}
