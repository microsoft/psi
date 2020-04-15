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
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Base class for PlotVisualizationObject views.
    /// </summary>
    /// <typeparam name="TPlotVisualizationObject">The type of timeline visualization object.</typeparam>
    /// <typeparam name="TData">The type of the data to plot.</typeparam>
    public class PlotVisualizationObjectView<TPlotVisualizationObject, TData> : TimelineCanvasVisualizationObjectView<TPlotVisualizationObject, TData>
        where TPlotVisualizationObject : PlotVisualizationObject<TData>, new()
    {
        private readonly List<Segment> segments = new List<Segment>();
        private Segment currentSegment;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotVisualizationObjectView{TTimelineVisualizationObject, TData}"/> class.
        /// </summary>
        public PlotVisualizationObjectView()
        {
        }

        /// <inheritdoc/>
        public override TPlotVisualizationObject VisualizationObject { get; protected set; }

        /// <inheritdoc/>
        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.OnDataContextChanged(sender, e);

            // add a handler for navigator data range changes
            this.Navigator.DataRange.RangeChanged += this.OnNavigatorDataRangeChanged;
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.VisualizationObject.IsConnected))
            {
                // if the IsConnected property is changing, that means the visualization object is being disconnected from the
                // panel, so detach all handlers
                this.Navigator.DataRange.RangeChanged -= this.OnNavigatorDataRangeChanged;
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.VisualizationObject.MarkerSize) ||
                e.PropertyName == nameof(this.VisualizationObject.MarkerStyle) ||
                e.PropertyName == nameof(this.VisualizationObject.MarkerColor))
            {
                this.ReRenderMarkers();
            }
            else if (e.PropertyName == nameof(this.VisualizationObject.YMax) || e.PropertyName == nameof(this.VisualizationObject.YMin))
            {
                if (this.UpdateTransforms())
                {
                    this.ReRenderMarkers();
                }
            }
            else if (e.PropertyName == nameof(this.VisualizationObject.InterpolationStyle))
            {
                if (this.VisualizationObject.Data != null)
                {
                    this.OnDataCollectionChanged(this.VisualizationObject.Data, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
                else if (this.VisualizationObject.SummaryData != null)
                {
                    this.OnSummaryDataCollectionChanged(this.VisualizationObject.SummaryData, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnNavigatorViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            if (this.Navigator.CursorMode == CursorMode.Live)
            {
                var oldSegments = this.segments.Where(s => s.EndTime < this.Navigator.ViewRange.StartTime).ToList();
                oldSegments.ForEach((s) => this.RemoveSegment(s));
            }

            base.OnNavigatorViewRangeChanged(sender, e);
        }

        /// <summary>
        /// Implements changes in response to navigator data range changed.
        /// </summary>
        /// <param name="sender">The sender of the change.</param>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnNavigatorDataRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            var oldSegments = this.segments.Where(s => s.EndTime < this.Navigator.DataRange.StartTime).ToList();
            oldSegments.ForEach((s) => this.RemoveSegment(s));

            if (this.UpdateTransforms())
            {
                this.OnTransformsChanged();
            }
        }

        /// <inheritdoc/>
        protected override void OnDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                bool transformChanged = this.UpdateTransforms();
                foreach (var item in e.NewItems)
                {
                    Message<TData> value = (Message<TData>)item;
                    this.AddPoint(value.OriginatingTime, this.VisualizationObject.GetDoubleValue(value.Data));
                }

                if (transformChanged)
                {
                    this.ReRenderMarkers();
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.ResetSegments();
                if (this.VisualizationObject.Data != null && this.VisualizationObject.Data.Count > 0)
                {
                    this.UpdateTransforms();
                    foreach (var point in this.VisualizationObject.Data)
                    {
                        this.AddPoint(point.OriginatingTime, this.VisualizationObject.GetDoubleValue(point.Data));
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"PlotVisualizationObjectView.OnDataCollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }

        /// <inheritdoc/>
        protected override void OnSummaryDataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    IntervalData<TData> range = (IntervalData<TData>)item;
                    this.AddRange(
                        range.OriginatingTime,
                        this.VisualizationObject.GetDoubleValue(range.Value),
                        this.VisualizationObject.GetDoubleValue(range.Minimum),
                        this.VisualizationObject.GetDoubleValue(range.Maximum));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.ResetSegments();
                if (this.VisualizationObject.SummaryData != null && this.VisualizationObject.SummaryData.Count > 0)
                {
                    this.UpdateTransforms();
                    foreach (var range in this.VisualizationObject.SummaryData)
                    {
                        this.AddRange(
                            range.OriginatingTime,
                            this.VisualizationObject.GetDoubleValue(range.Value),
                            this.VisualizationObject.GetDoubleValue(range.Minimum),
                            this.VisualizationObject.GetDoubleValue(range.Maximum));
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

        /// <inheritdoc/>
        protected override bool UpdateTransforms()
        {
            // Exit early if view is not parented
            if (this.VisualizationObject.Panel == null)
            {
                return false;
            }

            var baseUpdate = base.UpdateTransforms();

            // now do the update on the Y scale
            double oldScaleY = this.ScaleTransform.ScaleY;
            double oldTranslateY = this.TranslateTransform.Y;

            double maxY = this.VisualizationObject.YMax;
            double minY = this.VisualizationObject.YMin;
            if (maxY == minY)
            {
                maxY += 1;
                minY -= 1;
            }

            var deltaY = maxY - minY;
            this.ScaleTransform.ScaleY = -this.VisualizationObject.Panel.Height / ((deltaY != 0) ? deltaY * 1.2 : 1.0);
            this.TranslateTransform.Y = -maxY - (deltaY * 0.1);
            return baseUpdate || oldScaleY != this.ScaleTransform.ScaleY || oldTranslateY != this.TranslateTransform.Y;
        }

        /// <inheritdoc/>
        protected override void OnTransformsChanged()
        {
            this.ReRenderMarkers();
            base.OnTransformsChanged();
        }

        private void AddPoint(DateTime time, double value)
        {
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

            TimeSpan timeOffset = time - this.Navigator.DataRange.StartTime;
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
                TimeSpan timeOffset = time - this.Navigator.DataRange.StartTime;
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

            this.Canvas.Children.Add(this.currentSegment.LinePath);
            this.Canvas.Children.Add(this.currentSegment.MarkerPath);
            this.Canvas.Children.Add(this.currentSegment.RangePath);

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

            this.Canvas.Children.Remove(segment.LinePath);
            this.Canvas.Children.Remove(segment.MarkerPath);
            this.Canvas.Children.Remove(segment.RangePath);
        }

        private void ReRenderMarkers()
        {
            if (this.segments.Count == 0)
            {
                return;
            }

            this.segments.ForEach((s) => s.ClearMarkers());

            int currentSegment = 0;
            var dataPoints = (this.VisualizationObject.Data != null) ? this.VisualizationObject.Data.Select(m => new { m.OriginatingTime, m.Data }) : this.VisualizationObject.SummaryData.Select(m => new { m.OriginatingTime, Data = m.Value });
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

                TimeSpan time = value.OriginatingTime - this.Navigator.DataRange.StartTime;
                Point point = new Point(time.TotalSeconds, this.VisualizationObject.GetDoubleValue(value.Data));
                this.segments[currentSegment].RenderMarker(point);
            }
        }

        private void ResetSegments()
        {
            this.segments.Clear();
            this.currentSegment = null;
            this.Canvas.Children.Clear();
        }

        private class Segment
        {
            public const uint Capacity = 100;

            private PlotVisualizationObjectView<TPlotVisualizationObject, TData> parent;
            private PathGeometry lineGeometry;
            private PathFigure lineFigure;
            private PathGeometry markerGeometry;
            private PathGeometry rangeGeometry;
            private PathFigure rangeFigure;
            private bool previousPointIsValid = false;

            public Segment(PlotVisualizationObjectView<TPlotVisualizationObject, TData> parent)
            {
                this.parent = parent;

                this.lineFigure = new PathFigure();
                this.lineGeometry = new PathGeometry() { Transform = parent.TransformGroup };
                this.lineGeometry.Figures.Add(this.lineFigure);
                this.LinePath = new Path() { Data = this.lineGeometry };

                this.markerGeometry = new PathGeometry() { Transform = parent.TransformGroup };
                this.MarkerPath = new Path() { StrokeThickness = 1, Data = this.markerGeometry };
                this.ClearMarkers();

                this.rangeFigure = new PathFigure();
                this.rangeGeometry = new PathGeometry() { Transform = parent.TransformGroup };
                this.rangeGeometry.Figures.Add(this.rangeFigure);
                this.RangePath = new Path() { StrokeThickness = 1, Data = this.rangeGeometry };

                // Create bindings for lines
                var binding = new Binding(nameof(parent.VisualizationObject.Color))
                {
                    Source = parent.VisualizationObject,
                    Converter = new Converters.ColorConverter(),
                };
                BindingOperations.SetBinding(this.LinePath, Shape.StrokeProperty, binding);

                binding = new Binding(nameof(parent.VisualizationObject.LineWidth))
                {
                    Source = parent.VisualizationObject,
                };
                BindingOperations.SetBinding(this.LinePath, Shape.StrokeThicknessProperty, binding);

                // Create bindings for markers
                binding = new Binding(nameof(parent.VisualizationObject.MarkerColor))
                {
                    Source = parent.VisualizationObject,
                    Converter = new Converters.ColorConverter(),
                };
                BindingOperations.SetBinding(this.MarkerPath, Shape.StrokeProperty, binding);

                // Create bindings for ranges
                binding = new Binding(nameof(parent.VisualizationObject.RangeColor))
                {
                    Source = parent.VisualizationObject,
                    Converter = new Converters.ColorConverter(),
                };
                BindingOperations.SetBinding(this.RangePath, Shape.StrokeProperty, binding);

                binding = new Binding(nameof(parent.VisualizationObject.RangeWidth))
                {
                    Source = parent.VisualizationObject,
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
                if (double.IsNaN(point.Y) || double.IsInfinity(point.Y))
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

                InterpolationStyle interpolationStyle = this.parent.VisualizationObject.InterpolationStyle;

                // If we're doing step interpolation and there are existing points in the segment, then insert a horizontal joiner
                if (i >= 0 && interpolationStyle == InterpolationStyle.Step)
                {
                    this.lineFigure.Segments.Insert(i + 1, new LineSegment(new Point(point.X, ((LineSegment)this.lineFigure.Segments[i]).Point.Y), this.previousPointIsValid));
                    this.previousPointIsValid = true;
                    i++;
                }

                // Insert new point immediately after previous.
                this.lineFigure.Segments.Insert(i + 1, new LineSegment(point, this.previousPointIsValid && interpolationStyle != InterpolationStyle.None));
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
                // Don't render markers at discontinuities
                if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.None || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                {
                    return;
                }

                var markerSizeX = this.parent.VisualizationObject.MarkerSize / this.parent.ScaleTransform.ScaleX;
                var markerSizeY = this.parent.VisualizationObject.MarkerSize / this.parent.ScaleTransform.ScaleY;

                var markerFigure = new PathFigure();

                if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Plus)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y + (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y), true));
                }
                else if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Cross)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Asterisk)
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
                else if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Circle)
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
                else if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Square)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.DownTriangle)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y + (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y + (markerSizeY / 2)), true));
                }
                else if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.UpTriangle)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Diamond)
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

                if ((this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Circle) ||
                    (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Square) ||
                    (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.UpTriangle) ||
                    (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.DownTriangle) ||
                    (this.parent.VisualizationObject.MarkerStyle == MarkerStyle.Diamond))
                {
                    this.MarkerPath.Fill = new SolidColorBrush(this.parent.VisualizationObject.MarkerColor);
                }
                else
                {
                    this.MarkerPath.Fill = null;
                }
            }
        }
    }
}
