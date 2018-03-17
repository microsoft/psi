// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Base class for PlotVisualizationObject views
    /// </summary>
    /// <typeparam name="TConfig">The type of visualization object configuration.</typeparam>
    public class PlotVisualizationObjectView<TConfig> : UserControl
        where TConfig : PlotVisualizationObjectConfiguration, new()
    {
        private bool segmentsInvalidated;
        private List<Segment> segments = new List<Segment>();
        private Segment currentSegment;
        private ScaleTransform scaleTransform = new ScaleTransform();
        private TranslateTransform translateTransform = new TranslateTransform();
        private Navigator navigator;
        private DateTime? startTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotVisualizationObjectView{TConfig}"/> class.
        /// </summary>
        public PlotVisualizationObjectView()
        {
            this.DataContextChanged += this.PlotVisualizationObjectView_DataContextChanged;
            this.Unloaded += this.PlotVisualizationObjectView_Unloaded;
            this.SizeChanged += this.PlotVisualizationObjectView_SizeChanged;
        }

        /// <summary>
        /// Gets or sets the dynamic canvas element.
        /// </summary>
        public Canvas DynamicCanvas { get; protected set; }

        /// <summary>
        /// Gets the the plot visualization object.
        /// </summary>
        public PlotVisualizationObject<TConfig> PlotVisualizationObject { get; private set; }

        private void PlotVisualizationObjectView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.PlotVisualizationObject = (PlotVisualizationObject<TConfig>)this.DataContext;
            this.PlotVisualizationObject.PropertyChanging += this.PlotVisualizationObject_PropertyChanging;
            this.PlotVisualizationObject.PropertyChanged += this.PlotVisualizationObject_PropertyChanged;
            this.PlotVisualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;

            this.navigator = this.PlotVisualizationObject.Navigator;
            this.PlotVisualizationObject.Navigator.ViewRange.RangeChanged += this.Navigator_ViewRangeChanged;
            this.PlotVisualizationObject.Navigator.DataRange.RangeChanged += this.Navigator_DataRangeChanged;

            this.ResetSegments();
            if (this.PlotVisualizationObject.Data != null)
            {
                this.Data_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                this.PlotVisualizationObject.Data.DetailedCollectionChanged += this.Data_CollectionChanged;
            }

            if (this.PlotVisualizationObject.SummaryData != null)
            {
                this.SummaryData_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                this.PlotVisualizationObject.SummaryData.DetailedCollectionChanged += this.SummaryData_CollectionChanged;
            }
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlotVisualizationObjectConfiguration.MarkerSize) ||
                e.PropertyName == nameof(PlotVisualizationObjectConfiguration.MarkerStyle) ||
                e.PropertyName == nameof(PlotVisualizationObjectConfiguration.MarkerColor))
            {
                this.ReRenderMarkers();
            }
            else if (e.PropertyName == nameof(PlotVisualizationObjectConfiguration.YMax) || e.PropertyName == nameof(PlotVisualizationObjectConfiguration.YMin))
            {
                if (this.CalculateYTransform())
                {
                    this.ReRenderMarkers();
                }
            }
        }

        private void PlotVisualizationObject_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(this.PlotVisualizationObject.Data))
            {
                // Unsubscribe our handler from data collection notifications so that we do not
                // continue to receive notifications from collections that are no longer in view.
                if (this.PlotVisualizationObject.Data != null)
                {
                    this.PlotVisualizationObject.Data.DetailedCollectionChanged -= this.Data_CollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.PlotVisualizationObject.SummaryData))
            {
                if (this.PlotVisualizationObject.SummaryData != null)
                {
                    this.PlotVisualizationObject.SummaryData.DetailedCollectionChanged -= this.SummaryData_CollectionChanged;
                }
            }
        }

        private void PlotVisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.PlotVisualizationObject.Data))
            {
                this.Data_CollectionChanged(this.PlotVisualizationObject.Data, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.PlotVisualizationObject.Data != null)
                {
                    this.PlotVisualizationObject.Data.DetailedCollectionChanged += this.Data_CollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.PlotVisualizationObject.SummaryData))
            {
                this.SummaryData_CollectionChanged(this.PlotVisualizationObject.SummaryData, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.PlotVisualizationObject.SummaryData != null)
                {
                    this.PlotVisualizationObject.SummaryData.DetailedCollectionChanged += this.SummaryData_CollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.PlotVisualizationObject.Configuration))
            {
                this.PlotVisualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
                this.CalculateYTransform();
                this.ReRenderMarkers();
            }
        }

        private void PlotVisualizationObjectView_Unloaded(object sender, RoutedEventArgs e)
        {
            this.navigator.ViewRange.RangeChanged -= this.Navigator_ViewRangeChanged;
            this.navigator.DataRange.RangeChanged -= this.Navigator_DataRangeChanged;
            if (this.PlotVisualizationObject.Data != null)
            {
                this.PlotVisualizationObject.Data.DetailedCollectionChanged -= this.Data_CollectionChanged;
            }

            if (this.PlotVisualizationObject.SummaryData != null)
            {
                this.PlotVisualizationObject.SummaryData.DetailedCollectionChanged -= this.SummaryData_CollectionChanged;
            }
        }

        private void PlotVisualizationObjectView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.CalculateXTransform() || this.CalculateYTransform())
            {
                this.ReRenderMarkers();
            }
        }

        private void Navigator_ViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            if (this.navigator.NavigationMode == NavigationMode.Live)
            {
                var oldSegments = this.segments.Where(s => s.EndTime < this.navigator.ViewRange.StartTime).ToList();
                oldSegments.ForEach((s) => this.RemoveSegment(s));
            }

            if (this.CalculateXTransform())
            {
                this.ReRenderMarkers();
            }
        }

        private void Navigator_DataRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            var oldSegments = this.segments.Where(s => s.EndTime < this.navigator.DataRange.StartTime).ToList();
            oldSegments.ForEach((s) => this.RemoveSegment(s));
        }

        private void AddPoint(DateTime time, double value)
        {
            if (this.segmentsInvalidated)
            {
                this.ResetSegments();
            }

            if (!this.startTime.HasValue)
            {
                this.startTime = time;
                this.CalculateXTransform();
            }

            // Find the segment within which this point should lie
            if (this.currentSegment != null && (time < this.currentSegment.StartTime || time >= this.currentSegment.EndTime))
            {
                this.currentSegment = this.segments.FindLast(s => (time >= s.StartTime && time < s.EndTime));
            }

            Segment previousSegment = null;
            if (this.currentSegment == null || this.currentSegment.PointCount >= Segment.Capacity)
            {
                int i = this.AddSegment(time);
                if (i > 0)
                {
                    previousSegment = this.segments[i - 1];
                }
            }

            TimeSpan timeOffset = time - this.startTime.Value;
            Point point = new Point(timeOffset.TotalSeconds, value);

            // Add single connecting line between two segments
            if (previousSegment != null)
            {
                previousSegment.PlotLine(point);
            }

            this.currentSegment.AddPoint(time, point);
        }

        private void AddRange(DateTime time, double value, double min, double max)
        {
            // Plot the representative value of the data
            this.AddPoint(time, value);

            // Only plot the range if there is one
            if (max > min)
            {
                TimeSpan timeOffset = time - this.startTime.Value;
                Point point1 = new Point(timeOffset.TotalSeconds, min);
                Point point2 = new Point(timeOffset.TotalSeconds, max);

                // Draw a vertical bar representing the min/max range over the interval
                this.currentSegment.PlotRange(point1, point2);
            }
        }

        private int AddSegment(DateTime startTime)
        {
            this.currentSegment = new Segment(this) { StartTime = startTime };

            // Maintain time-based sort order of segments. Find the predecessor segment index.
            int predecessor = this.segments.Count - 1;
            while (predecessor >= 0 && this.segments[predecessor].StartTime > startTime)
            {
                --predecessor;
            }

            // Current index is one after predecessor. If there is no predecessor, iPred will
            // be -1 and iCurrent will be 0. If predecessor is last element of the list,
            // iCurrent will be equal to Count. So just insert new segment at iCurrent.
            int current = predecessor + 1;
            this.segments.Insert(current, this.currentSegment);

            // Ensure that segment times do not overlap
            int next = current + 1;
            if (next < this.segments.Count)
            {
                // Constrain end time of current segment to be no greater than the start time of the next segment.
                this.currentSegment.EndTime = this.segments[next].StartTime.Value;
            }

            if (predecessor >= 0)
            {
                // Predecessor end time should be no greater than new segment start time. It is possible for the
                // previous segment to have an end time which is greater if we are starting a new segment due to
                // the previous one being full with a point that falls somewhere in between the previous segment's
                // start and end times. By right we should remove the LineSegments from the previous segment which
                // occur after the new segment's start time, and re-insert them into the new segment, but since
                // they have already been plotted, it seems innocuous to just leave them in the old segment.
                this.segments[predecessor].EndTime = startTime;
            }

            this.DynamicCanvas.Children.Add(this.currentSegment.LinePath);
            this.DynamicCanvas.Children.Add(this.currentSegment.MarkerPath);
            this.DynamicCanvas.Children.Add(this.currentSegment.RangePath);

            // Return index of newly inserted segment so caller can more easily get to the adjacent segments.
            return current;
        }

        private void RemoveSegment(Segment segment)
        {
            this.segments.Remove(segment);
            if (this.currentSegment == segment)
            {
                this.currentSegment = null;
            }

            this.DynamicCanvas.Children.Remove(segment.LinePath);
            this.DynamicCanvas.Children.Remove(segment.MarkerPath);
            this.DynamicCanvas.Children.Remove(segment.RangePath);
        }

        private void ReRenderMarkers()
        {
            if (this.segments.Count == 0)
            {
                return;
            }

            this.segments.ForEach((s) => s.ClearMarkers());

            int currentSegment = 0;
            var dataPoints = (this.PlotVisualizationObject.Data != null) ? this.PlotVisualizationObject.Data.Select(m => new { m.OriginatingTime, m.Data }) : this.PlotVisualizationObject.SummaryData.Select(m => new { m.OriginatingTime, Data = m.Value });
            foreach (var value in dataPoints)
            {
                while (currentSegment < this.segments.Count &&
                    this.segments[currentSegment].EndTime < value.OriginatingTime)
                {
                    currentSegment++;
                }

                // Have we moved past end of our last segment?
                // This can happen if we are re-rendering ticks just after new points have been added, but not plotted
                if (currentSegment >= this.segments.Count)
                {
                    break;
                }

                TimeSpan time = value.OriginatingTime - this.startTime.Value;
                Point point = new Point(time.TotalSeconds, value.Data);
                this.segments[currentSegment].RenderMarker(point);
            }
        }

        private bool CalculateXTransform()
        {
            if (!this.startTime.HasValue)
            {
                return false;
            }

            double oldScaleX = this.scaleTransform.ScaleX;
            double oldTranslateX = this.translateTransform.X;

            var timeSpan = this.navigator.ViewRange.Duration;
            this.scaleTransform.ScaleX = this.ActualWidth / timeSpan.TotalSeconds;
            this.translateTransform.X = -(this.navigator.ViewRange.StartTime - this.startTime.Value).TotalSeconds;

            return oldScaleX != this.scaleTransform.ScaleX || oldTranslateX != this.translateTransform.X;
        }

        private bool CalculateYTransform()
        {
            // Exit early if view is not parented
            if (this.PlotVisualizationObject.Panel == null)
            {
                return false;
            }

            double oldScaleY = this.scaleTransform.ScaleY;
            double oldTranslateY = this.translateTransform.Y;

            double maxY = this.PlotVisualizationObject.Configuration.YMax;
            double minY = this.PlotVisualizationObject.Configuration.YMin;
            if (maxY == minY)
            {
                maxY += 1;
                minY -= 1;
            }

            var deltaY = maxY - minY;
            this.scaleTransform.ScaleY = -this.PlotVisualizationObject.Panel.Height / ((deltaY != 0) ? deltaY * 1.2 : 1.0);
            this.translateTransform.Y = -maxY - (deltaY * 0.1);
            return oldScaleY != this.scaleTransform.ScaleY || oldTranslateY != this.translateTransform.Y;
        }

        private void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                bool transformChanged = this.CalculateYTransform();
                foreach (var item in e.NewItems)
                {
                    Message<double> value = (Message<double>)item;
                    this.AddPoint(value.OriginatingTime, value.Data);
                }

                if (transformChanged)
                {
                    this.ReRenderMarkers();
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.InvalidateSegments();
                if (this.PlotVisualizationObject.Data != null && this.PlotVisualizationObject.Data.Count > 0)
                {
                    this.CalculateXTransform();
                    this.CalculateYTransform();
                    foreach (var point in this.PlotVisualizationObject.Data)
                    {
                        this.AddPoint(point.OriginatingTime, point.Data);
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"PlotVisualizationObjectView.Data_CollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }

        private void SummaryData_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    IntervalData<double> range = (IntervalData<double>)item;
                    this.AddRange(range.OriginatingTime, range.Value, range.Minimum, range.Maximum);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.InvalidateSegments();
                if (this.PlotVisualizationObject.SummaryData != null && this.PlotVisualizationObject.SummaryData.Count > 0)
                {
                    this.CalculateXTransform();
                    this.CalculateYTransform();
                    foreach (var range in this.PlotVisualizationObject.SummaryData)
                    {
                        this.AddRange(range.OriginatingTime, range.Value, range.Minimum, range.Maximum);
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
                throw new NotImplementedException($"PlotVisualizationObjectView.SummaryData_CollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }

        private void ResetSegments()
        {
            // Reset startTime so that it won't get too far away from newer data, which caused rendering transform issues in the past
            this.startTime = null;

            this.segments.Clear();
            this.currentSegment = null;
            this.DynamicCanvas.Children.Clear();
            this.segmentsInvalidated = false;
        }

        private void InvalidateSegments()
        {
            this.segmentsInvalidated = true;
        }

        private class Segment
        {
            public const uint Capacity = 100;

            private PlotVisualizationObjectView<TConfig> parent;
            private TransformGroup transformGroup;
            private ScaleTransform scaleTransform;
            private PathGeometry lineGeometry;
            private PathFigure lineFigure;
            private PathGeometry markerGeometry;
            private PathGeometry rangeGeometry;
            private PathFigure rangeFigure;
            private bool previousPointIsValid = false;

            public Segment(PlotVisualizationObjectView<TConfig> parent)
            {
                this.parent = parent;

                this.transformGroup = new TransformGroup();
                this.scaleTransform = parent.scaleTransform;
                this.transformGroup.Children.Add(parent.translateTransform);
                this.transformGroup.Children.Add(parent.scaleTransform);

                this.lineFigure = new PathFigure();
                this.lineGeometry = new PathGeometry() { Transform = this.transformGroup };
                this.lineGeometry.Figures.Add(this.lineFigure);
                this.LinePath = new Path() { Data = this.lineGeometry };

                this.markerGeometry = new PathGeometry() { Transform = this.transformGroup };
                this.MarkerPath = new Path() { StrokeThickness = 1, Data = this.markerGeometry };
                this.ClearMarkers();

                this.rangeFigure = new PathFigure();
                this.rangeGeometry = new PathGeometry() { Transform = this.transformGroup };
                this.rangeGeometry.Figures.Add(this.rangeFigure);
                this.RangePath = new Path() { StrokeThickness = 1, Data = this.rangeGeometry };

                // Create bindings for lines
                var binding = new Binding(nameof(parent.PlotVisualizationObject.Configuration) + "." + nameof(PlotVisualizationObjectConfiguration.LineColor))
                {
                    Source = parent.PlotVisualizationObject,
                    Converter = new Converters.ColorConverter()
                };
                BindingOperations.SetBinding(this.LinePath, Shape.StrokeProperty, binding);

                binding = new Binding(nameof(parent.PlotVisualizationObject.Configuration) + "." + nameof(PlotVisualizationObjectConfiguration.LineWidth))
                {
                    Source = parent.PlotVisualizationObject
                };
                BindingOperations.SetBinding(this.LinePath, Shape.StrokeThicknessProperty, binding);

                // Create bindings for markers
                binding = new Binding(nameof(parent.PlotVisualizationObject.Configuration) + "." + nameof(PlotVisualizationObjectConfiguration.MarkerColor))
                {
                    Source = parent.PlotVisualizationObject,
                    Converter = new Converters.ColorConverter()
                };
                BindingOperations.SetBinding(this.MarkerPath, Shape.StrokeProperty, binding);

                // Create bindings for ranges
                binding = new Binding(nameof(parent.PlotVisualizationObject.Configuration) + "." + nameof(PlotVisualizationObjectConfiguration.RangeColor))
                {
                    Source = parent.PlotVisualizationObject,
                    Converter = new Converters.ColorConverter()
                };
                BindingOperations.SetBinding(this.RangePath, Shape.StrokeProperty, binding);

                binding = new Binding(nameof(parent.PlotVisualizationObject.Configuration) + "." + nameof(PlotVisualizationObjectConfiguration.RangeWidth))
                {
                    Source = parent.PlotVisualizationObject
                };
                BindingOperations.SetBinding(this.RangePath, Shape.StrokeThicknessProperty, binding);

                this.previousPointIsValid = false;
            }

            public Path MarkerPath { get; private set; }

            public Path LinePath { get; private set; }

            public Path RangePath { get; private set; }

            public int PointCount { get; private set; }

            public DateTime? StartTime { get; set; }

            public DateTime EndTime { get; set; } = DateTime.MaxValue; // Start out at MaxValue so that an intial empty segment doesn't get removed from a data range change

            public void AddPoint(DateTime time, Point point)
            {
                if (!this.StartTime.HasValue)
                {
                    this.StartTime = time;
                }

                // Don't set EndTime here. Segment end time is now set by its position within the sorted list of segments.
                this.PlotLine(point);
                this.RenderMarker(point);
            }

            public void PlotLine(Point point)
            {
                if (double.IsNaN(point.Y))
                {
                    this.previousPointIsValid = false;
                    return;
                }

                // Insert new point in sorted order based on its x-axis (time) value. Most of the time we
                // will be appending in increasing time order, so start at the end and walk backwards.
                int i = this.lineFigure.Segments.Count - 1;
                while (i >= 0 && ((LineSegment)this.lineFigure.Segments[i]).Point.X > point.X)
                {
                    --i;
                }

                // Insert new point immediately after previous
                this.lineFigure.Segments.Insert(i + 1, new LineSegment(point, this.previousPointIsValid));
                this.previousPointIsValid = true;
                this.PointCount++;
            }

            public void PlotRange(Point p1, Point p2)
            {
                if (p1 != p2)
                {
                    this.rangeFigure.Segments.Add(new LineSegment(p1, false));
                    this.rangeFigure.Segments.Add(new LineSegment(p2, true));
                }
            }

            public void RenderMarker(Point point)
            {
                var markerSizeX = this.parent.PlotVisualizationObject.Configuration.MarkerSize / this.scaleTransform.ScaleX;
                var markerSizeY = this.parent.PlotVisualizationObject.Configuration.MarkerSize / this.scaleTransform.ScaleY;

                var markerFigure = new PathFigure();

                if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Plus)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y + (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y), true));
                }
                else if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Cross)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Asterisk)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y + (markerSizeY / 2)), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y), true));
                }
                else if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Circle)
                {
                    var startPoint = new Point(point.X - (markerSizeX / 2), point.Y);
                    var endPoint = new Point(point.X + (markerSizeX / 2), point.Y);
                    markerFigure.StartPoint = startPoint;
                    markerFigure.Segments.Add(new ArcSegment(
                        endPoint,
                        new Size(Math.Abs(markerSizeX) / 2, Math.Abs(markerSizeY) / 2),
                        0,
                        true,
                        SweepDirection.Clockwise,
                        true));
                    markerFigure.Segments.Add(new ArcSegment(
                        startPoint,
                        new Size(Math.Abs(markerSizeX) / 2, Math.Abs(markerSizeY) / 2),
                        0,
                        true,
                        SweepDirection.Clockwise,
                        true));
                }
                else if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Square)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.DownTriangle)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y + (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y + (markerSizeY / 2)), true));
                }
                else if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.UpTriangle)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Diamond)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y);
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y), true));
                }

                if (markerFigure != null)
                {
                    this.markerGeometry.Figures.Add(markerFigure);
                }
            }

            public void ClearMarkers()
            {
                this.markerGeometry.Figures.Clear();

                if ((this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Circle) ||
                    (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Square) ||
                    (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.UpTriangle) ||
                    (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.DownTriangle) ||
                    (this.parent.PlotVisualizationObject.Configuration.MarkerStyle == Common.MarkerStyle.Diamond))
                {
                    this.MarkerPath.Fill = new SolidColorBrush(this.parent.PlotVisualizationObject.Configuration.MarkerColor);
                }
                else
                {
                    this.MarkerPath.Fill = null;
                }
            }
        }
    }
}
