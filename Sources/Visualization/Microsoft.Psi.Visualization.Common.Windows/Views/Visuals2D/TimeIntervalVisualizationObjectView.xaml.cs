// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for TimeIntervalVisualizationObjectView.xaml.
    /// </summary>
    public partial class TimeIntervalVisualizationObjectView : UserControl
    {
        private Path linePath;
        private PathGeometry lineGeometry;
        private PathFigure lineFigure;

        private Path thresholdPath;
        private PathGeometry thresholdGeometry;
        private PathFigure thresholdFigure;

        private Brush brush = null;
        private Brush thresholdBrush = null;
        private ScaleTransform scaleTransform = new ScaleTransform();
        private TranslateTransform translateTransform = new TranslateTransform();
        private TransformGroup transformGroup = new TransformGroup();
        private Navigator navigator;

        private DateTime? startTime;

        private List<Tuple<DateTime, DateTime>> datapoints = new List<Tuple<DateTime, DateTime>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalVisualizationObjectView"/> class.
        /// </summary>
        public TimeIntervalVisualizationObjectView()
        {
            this.InitializeComponent();
            this.transformGroup.Children.Add(this.translateTransform);
            this.transformGroup.Children.Add(this.scaleTransform);

            this.DataContextChanged += this.TimeIntervalVisualizationObject_DataContextChanged;
            this.Unloaded += this.TimeIntervalVisualizationObjectView_Unloaded;
            this.SizeChanged += this.TimeIntervalVisualizationObjectView_SizeChanged;
        }

        /// <summary>
        /// Gets the time interval visualization object.
        /// </summary>
        public TimeIntervalVisualizationObject TimeIntervalVisualizationObject { get; private set; }

        private void AddPoint(Tuple<DateTime, DateTime> timeInterval)
        {
            if (!this.startTime.HasValue)
            {
                this.startTime = timeInterval.Item1;
                this.CalculateScaleTransform();
            }

            TimeSpan left = timeInterval.Item1 - this.startTime.Value;
            TimeSpan right = timeInterval.Item2 - this.startTime.Value;

            var trackBottom = (this.TimeIntervalVisualizationObject.TrackIndex + 1) / (double)this.TimeIntervalVisualizationObject.TrackCount;
            var trackWidth = 1 / (double)this.TimeIntervalVisualizationObject.TrackCount;

            if ((timeInterval.Item2 - timeInterval.Item1).TotalMilliseconds > this.TimeIntervalVisualizationObject.Threshold)
            {
                this.thresholdFigure.Segments.Add(new LineSegment(new Point(left.TotalSeconds, 1 - (trackBottom - (0.1 * trackWidth))), false));
                this.thresholdFigure.Segments.Add(new LineSegment(new Point(left.TotalSeconds, 1 - (trackBottom - (0.35 * trackWidth))), true));
                this.thresholdFigure.Segments.Add(new LineSegment(new Point(right.TotalSeconds, 1 - (trackBottom - (0.65 * trackWidth))), true));
                this.thresholdFigure.Segments.Add(new LineSegment(new Point(right.TotalSeconds, 1 - (trackBottom - (0.9 * trackWidth))), true));
            }
            else
            {
                this.lineFigure.Segments.Add(new LineSegment(new Point(left.TotalSeconds, 1 - (trackBottom - (0.1 * trackWidth))), false));
                this.lineFigure.Segments.Add(new LineSegment(new Point(left.TotalSeconds, 1 - (trackBottom - (0.35 * trackWidth))), true));
                this.lineFigure.Segments.Add(new LineSegment(new Point(right.TotalSeconds, 1 - (trackBottom - (0.65 * trackWidth))), true));
                this.lineFigure.Segments.Add(new LineSegment(new Point(right.TotalSeconds, 1 - (trackBottom - (0.9 * trackWidth))), true));
            }
        }

        private void TimeIntervalVisualizationObject_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.TimeIntervalVisualizationObject = (TimeIntervalVisualizationObject)this.DataContext;
            this.TimeIntervalVisualizationObject.PropertyChanging += this.TimeIntervalVisualizationObject_PropertyChanging;
            this.TimeIntervalVisualizationObject.PropertyChanged += this.TimeIntervalVisualizationObject_PropertyChanged;

            this.navigator = this.TimeIntervalVisualizationObject.Navigator;
            this.navigator.ViewRange.RangeChanged += this.Navigator_ViewRangeChanged;

            this.ResetPath();
            if (this.TimeIntervalVisualizationObject.Data != null)
            {
                this.Data_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                this.TimeIntervalVisualizationObject.Data.DetailedCollectionChanged += this.Data_CollectionChanged;
            }

            if (this.TimeIntervalVisualizationObject.SummaryData != null)
            {
                this.IntervalData_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                this.TimeIntervalVisualizationObject.SummaryData.DetailedCollectionChanged += this.IntervalData_CollectionChanged;
            }
        }

        private void UpdateThreshold()
        {
            this.lineFigure.Segments.Clear();
            this.thresholdFigure.Segments.Clear();
            foreach (var timeInterval in this.datapoints)
            {
                this.AddPoint(timeInterval);
            }
        }

        private void TimeIntervalVisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.TimeIntervalVisualizationObject.Data))
            {
                this.Data_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.TimeIntervalVisualizationObject.Data != null)
                {
                    this.TimeIntervalVisualizationObject.Data.DetailedCollectionChanged += this.Data_CollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.TimeIntervalVisualizationObject.SummaryData))
            {
                this.IntervalData_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.TimeIntervalVisualizationObject.SummaryData != null)
                {
                    this.TimeIntervalVisualizationObject.SummaryData.DetailedCollectionChanged += this.IntervalData_CollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.TimeIntervalVisualizationObject.Threshold))
            {
                this.UpdateThreshold();
            }

            if (e.PropertyName == nameof(this.TimeIntervalVisualizationObject.Color))
            {
                this.brush = null;
                this.ResetPath();
                this.UpdateThreshold();
            }

            if (e.PropertyName == nameof(this.TimeIntervalVisualizationObject.ThresholdColor))
            {
                this.thresholdBrush = null;
                this.ResetPath();
                this.UpdateThreshold();
            }
        }

        private void TimeIntervalVisualizationObject_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(this.TimeIntervalVisualizationObject.Data))
            {
                // Unsubscribe our handler from data collection notifications so that we do not
                // continue to receive notifications from collections that are no longer in view.
                if (this.TimeIntervalVisualizationObject.Data != null)
                {
                    this.TimeIntervalVisualizationObject.Data.DetailedCollectionChanged -= this.Data_CollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.TimeIntervalVisualizationObject.SummaryData))
            {
                if (this.TimeIntervalVisualizationObject.SummaryData != null)
                {
                    this.TimeIntervalVisualizationObject.SummaryData.DetailedCollectionChanged -= this.IntervalData_CollectionChanged;
                }
            }
        }

        private void TimeIntervalVisualizationObjectView_Unloaded(object sender, RoutedEventArgs e)
        {
            this.navigator.ViewRange.RangeChanged -= this.Navigator_ViewRangeChanged;
            if (this.TimeIntervalVisualizationObject.Data != null)
            {
                this.TimeIntervalVisualizationObject.Data.DetailedCollectionChanged -= this.Data_CollectionChanged;
            }
        }

        private void TimeIntervalVisualizationObjectView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.CalculateScaleTransform();
        }

        private void Navigator_ViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.CalculateScaleTransform();
        }

        private void ResetPath()
        {
            // Remove previous paths
            this.DynamicCanvas.Children.Clear();

            if (this.brush == null)
            {
                this.brush = new SolidColorBrush(this.TimeIntervalVisualizationObject.Color);
            }

            if (this.thresholdBrush == null)
            {
                this.thresholdBrush = new SolidColorBrush(this.TimeIntervalVisualizationObject.ThresholdColor);
            }

            this.linePath = new Path() { Stroke = this.brush, StrokeThickness = 1 };
            this.lineGeometry = new PathGeometry();
            this.lineFigure = new PathFigure();
            this.lineGeometry.Figures.Add(this.lineFigure);
            this.linePath.Data = this.lineGeometry;
            this.lineGeometry.Transform = this.transformGroup;
            this.DynamicCanvas.Children.Add(this.linePath);

            this.thresholdPath = new Path() { Stroke = this.thresholdBrush, StrokeThickness = 1 };
            this.thresholdGeometry = new PathGeometry();
            this.thresholdFigure = new PathFigure();
            this.thresholdGeometry.Figures.Add(this.thresholdFigure);
            this.thresholdPath.Data = this.thresholdGeometry;
            this.thresholdGeometry.Transform = this.transformGroup;
            this.DynamicCanvas.Children.Add(this.thresholdPath);

            this.CalculateScaleTransform();
        }

        private void CalculateScaleTransform()
        {
            if (!this.startTime.HasValue)
            {
                return;
            }

            if (this.TimeIntervalVisualizationObject == null || this.TimeIntervalVisualizationObject.Panel == null)
            {
                return;
            }

            var timeSpan = this.TimeIntervalVisualizationObject.Navigator.ViewRange.Duration;

            var deltaY = 1 - 0;
            this.scaleTransform.ScaleX = this.ActualWidth / timeSpan.TotalSeconds;
            this.scaleTransform.ScaleY = -this.TimeIntervalVisualizationObject.Panel.Height / ((deltaY != 0) ? deltaY : 1.0);
            this.translateTransform.Y = -1;
            this.translateTransform.X = -(this.TimeIntervalVisualizationObject.Navigator.ViewRange.StartTime - this.startTime.Value).TotalSeconds;
        }

        private void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                this.CalculateScaleTransform();
                foreach (var item in e.NewItems)
                {
                    var datapoint = ((Message<Tuple<DateTime, DateTime>>)item).Data;
                    this.datapoints.Add(datapoint);
                    this.AddPoint(datapoint);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.ResetPath();
                if (this.TimeIntervalVisualizationObject.Data != null)
                {
                    foreach (var message in this.TimeIntervalVisualizationObject.Data)
                    {
                        var datapoint = message.Data;
                        this.datapoints.Add(datapoint);
                        this.AddPoint(datapoint);
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"TimeIntervalVisualizationObjectView.Data_CollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }

        private void IntervalData_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                this.CalculateScaleTransform();
                var index = e.NewStartingIndex;
                foreach (var item in e.NewItems)
                {
                    var datapoint = ((IntervalData<Tuple<DateTime, DateTime>>)item).Value;
                    this.datapoints.Add(datapoint);
                    this.AddPoint(datapoint);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.ResetPath();
                if (this.TimeIntervalVisualizationObject.SummaryData != null)
                {
                    foreach (var interval in this.TimeIntervalVisualizationObject.SummaryData)
                    {
                        var datapoint = interval.Value;
                        this.datapoints.Add(datapoint);
                        this.AddPoint(datapoint);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                // Currently, the only time we get hit with a replace is on the boundary intervals of a
                // range of new intervals being added, and those will be included in the Add notification,
                // so we handle them anyway. The old value being replaced should already have been removed
                // when its containing segment was removed.
            }
            else
            {
                throw new NotImplementedException($"TimeIntervalVisualizationObjectView.IntervalData_CollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }
    }
}
