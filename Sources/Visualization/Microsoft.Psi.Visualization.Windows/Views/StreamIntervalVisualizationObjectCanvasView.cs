// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Provide an abstract base class for canvas-based views of stream interval visualization objects.
    /// </summary>
    /// <typeparam name="TStreamIntervalVisualizationObject">The type of stream interval visualization object.</typeparam>
    /// <typeparam name="TData">The type of data.</typeparam>
    public abstract class StreamIntervalVisualizationObjectCanvasView<TStreamIntervalVisualizationObject, TData>
        : StreamVisualizationObjectCanvasView<TStreamIntervalVisualizationObject, TData>
        where TStreamIntervalVisualizationObject : StreamIntervalVisualizationObject<TData>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamIntervalVisualizationObjectCanvasView{TVisualizationObject, TData}"/> class.
        /// </summary>
        public StreamIntervalVisualizationObjectCanvasView()
        {
        }

        /// <summary>
        /// Gets the timeline visualization object.
        /// </summary>
        public TStreamIntervalVisualizationObject StreamIntervalVisualizationObject =>
            this.VisualizationObject as TStreamIntervalVisualizationObject;

        /// <inheritdoc/>
        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            // if we have data, setup handlers for data changes
            this.OnDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (this.StreamIntervalVisualizationObject.Data != null)
            {
                this.StreamIntervalVisualizationObject.Data.DetailedCollectionChanged += this.OnDataCollectionChanged;
            }

            // add a handler for summary data changes
            this.OnSummaryDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (this.StreamIntervalVisualizationObject.SummaryData != null)
            {
                this.StreamIntervalVisualizationObject.SummaryData.DetailedCollectionChanged += this.OnSummaryDataCollectionChanged;
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.StreamIntervalVisualizationObject.IsConnected))
            {
                // if the IsConnected property is changing, that means the visualization object is being disconnected from the
                // panel, so detach all handlers
                if (this.StreamIntervalVisualizationObject.Data != null)
                {
                    this.StreamIntervalVisualizationObject.Data.DetailedCollectionChanged -= this.OnDataCollectionChanged;
                }

                if (this.StreamIntervalVisualizationObject.SummaryData != null)
                {
                    this.StreamIntervalVisualizationObject.SummaryData.DetailedCollectionChanged -= this.OnSummaryDataCollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.StreamIntervalVisualizationObject.Data))
            {
                if (this.StreamIntervalVisualizationObject.Data != null)
                {
                    this.StreamIntervalVisualizationObject.Data.DetailedCollectionChanged -= this.OnDataCollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.StreamIntervalVisualizationObject.SummaryData))
            {
                if (this.StreamIntervalVisualizationObject.SummaryData != null)
                {
                    this.StreamIntervalVisualizationObject.SummaryData.DetailedCollectionChanged -= this.OnSummaryDataCollectionChanged;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.StreamIntervalVisualizationObject.Data))
            {
                this.OnDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.StreamIntervalVisualizationObject.Data != null)
                {
                    this.StreamIntervalVisualizationObject.Data.DetailedCollectionChanged += this.OnDataCollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.StreamIntervalVisualizationObject.SummaryData))
            {
                this.OnSummaryDataCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.StreamIntervalVisualizationObject.SummaryData != null)
                {
                    this.StreamIntervalVisualizationObject.SummaryData.DetailedCollectionChanged += this.OnSummaryDataCollectionChanged;
                }
            }
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
        /// Implements changes in response to summary data collection changed.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnSummaryDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
        }
    }
}
