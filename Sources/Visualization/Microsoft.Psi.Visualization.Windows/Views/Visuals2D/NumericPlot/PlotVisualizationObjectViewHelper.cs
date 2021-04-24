// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization;

    /// <summary>
    /// Implements a helper class that provides support for constructing plot visualization object views.
    /// </summary>
    /// <typeparam name="TKey">The type of the series key.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <remarks>
    /// This class abstracts common functionality for, and is used via aggregation by
    /// <see cref="PlotVisualizationObjectView{TPlotVisualizationObject, TData}"/> and
    /// <see cref="PlotSeriesVisualizationObjectView{TPlotVisualizationObject, TKey, TData}"/>.
    /// The constructor receives the corresponding visualization object view as a parent,
    /// abstracted as a <see cref="IPlotVisualizationObjectView{TKey, TData}"/> object.
    /// </remarks>
    public class PlotVisualizationObjectViewHelper<TKey, TData>
    {
        private readonly IPlotVisualizationObjectView<TKey, TData> plotVisualizationObjectView;
        private readonly Dictionary<TKey, List<Segment>> segments = new Dictionary<TKey, List<Segment>>();
        private readonly Dictionary<TKey, Segment> currentSegment = new Dictionary<TKey, Segment>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotVisualizationObjectViewHelper{TKey, TData}"/> class.
        /// </summary>
        /// <param name="plotVisualizationObjectView">The plot visualization object view.</param>
        public PlotVisualizationObjectViewHelper(IPlotVisualizationObjectView<TKey, TData> plotVisualizationObjectView)
        {
            this.plotVisualizationObjectView = plotVisualizationObjectView;
        }

        /// <summary>
        /// Adds a point to a specified series.
        /// </summary>
        /// <param name="keySeries">The series key.</param>
        /// <param name="time">The time for the point.</param>
        /// <param name="value">The value for the point.</param>
        public void AddPoint(TKey keySeries, DateTime time, double value)
        {
            if (!this.currentSegment.ContainsKey(keySeries))
            {
                this.currentSegment.Add(keySeries, null);
            }

            // Find the segment within which this point should lie
            if (this.currentSegment[keySeries] != null && (time < this.currentSegment[keySeries].StartTime || time >= this.currentSegment[keySeries].EndTime))
            {
                this.currentSegment[keySeries] = this.segments[keySeries].FindLast(s => (time >= s.StartTime && time < s.EndTime));
            }

            Segment previousSegment = null;
            if (this.currentSegment[keySeries] == null || this.currentSegment[keySeries].PointCount >= Segment.Capacity)
            {
                int i = this.AddSegment(keySeries, time);
                if (i > 0)
                {
                    previousSegment = this.segments[keySeries][i - 1];
                }
            }

            var timeOffset = time - this.plotVisualizationObjectView.Navigator.DataRange.StartTime;
            var point = new Point(timeOffset.TotalSeconds, value);

            // Add single connecting line between two segments
            previousSegment?.AddLine(point);

            this.currentSegment[keySeries].AddPoint(time, point);
        }

        /// <summary>
        /// Adds a range to a specified series.
        /// </summary>
        /// <param name="keySeries">The series key.</param>
        /// <param name="time">The time for the range.</param>
        /// <param name="value">The range value.</param>
        /// <param name="min">The range minimum.</param>
        /// <param name="max">The range maximum.</param>
        public void AddRange(TKey keySeries, DateTime time, double value, double min, double max)
        {
            // Plot the representative value of the data
            this.AddPoint(keySeries, time, value);

            // Only plot the range if there is one
            if (max > min)
            {
                var timeOffset = time - this.plotVisualizationObjectView.Navigator.DataRange.StartTime;
                var point1 = new Point(timeOffset.TotalSeconds, min);
                var point2 = new Point(timeOffset.TotalSeconds, max);

                // Draw a vertical bar representing the min/max range over the interval
                this.currentSegment[keySeries].PlotRange(point1, point2);
            }
        }

        /// <summary>
        /// Adds a segment to a specified series.
        /// </summary>
        /// <param name="keySeries">The series key.</param>
        /// <param name="startTime">The segment start time.</param>
        /// <returns>The segment index.</returns>
        public int AddSegment(TKey keySeries, DateTime startTime)
        {
            if (!this.segments.ContainsKey(keySeries))
            {
                this.segments.Add(keySeries, new List<Segment>());
            }

            this.currentSegment[keySeries] = new Segment(this.plotVisualizationObjectView) { StartTime = startTime };
            this.plotVisualizationObjectView.CreateBindings(
                keySeries,
                this.currentSegment[keySeries].LinePath,
                this.currentSegment[keySeries].MarkerPath,
                this.currentSegment[keySeries].RangePath);

            // Maintain time-based sort order of segments. Find the predecessor segment index.
            int predecessor = this.segments[keySeries].Count - 1;
            while (predecessor >= 0 && this.segments[keySeries][predecessor].StartTime > startTime)
            {
                --predecessor;
            }

            // Current index is one after predecessor. If there is no predecessor, iPred will
            // be -1 and iCurrent will be 0. If predecessor is last element of the list,
            // iCurrent will be equal to Count. So just insert new segment at iCurrent.
            int current = predecessor + 1;
            this.segments[keySeries].Insert(current, this.currentSegment[keySeries]);

            // Ensure that segment times do not overlap
            int next = current + 1;
            if (next < this.segments[keySeries].Count)
            {
                // Constrain end time of current segment to be no greater than the start time of the next segment.
                this.currentSegment[keySeries].EndTime = this.segments[keySeries][next].StartTime.Value;
            }

            if (predecessor >= 0)
            {
                // Predecessor end time should be no greater than new segment start time. It is possible for the
                // previous segment to have an end time which is greater if we are starting a new segment due to
                // the previous one being full with a point that falls somewhere in between the previous segment's
                // start and end times. By right we should remove the LineSegments from the previous segment which
                // occur after the new segment's start time, and re-insert them into the new segment, but since
                // they have already been plotted, it seems innocuous to just leave them in the old segment.
                this.segments[keySeries][predecessor].EndTime = startTime;
            }

            this.plotVisualizationObjectView.Canvas.Children.Add(this.currentSegment[keySeries].LinePath);
            this.plotVisualizationObjectView.Canvas.Children.Add(this.currentSegment[keySeries].MarkerPath);
            this.plotVisualizationObjectView.Canvas.Children.Add(this.currentSegment[keySeries].RangePath);

            // Return index of newly inserted segment so caller can more easily get to the adjacent segments.
            return current;
        }

        /// <summary>
        /// Removes segments specified by a predicate.
        /// </summary>
        /// <param name="predicate">A predicate that indicates which segments to remove.</param>
        public void RemoveSegments(Func<Segment, bool> predicate)
        {
            foreach (var key in this.segments.Keys)
            {
                var keySegments = this.segments[key];
                var oldKeySegments = keySegments.Where(predicate).ToList();
                oldKeySegments.ForEach((s) => this.RemoveSegment(key, s));
            }
        }

        /// <summary>
        /// Removes a segment from a specified series.
        /// </summary>
        /// <param name="seriesKey">The series key.</param>
        /// <param name="segment">The segment to remove.</param>
        public void RemoveSegment(TKey seriesKey, Segment segment)
        {
            // Remove segment elements from the canvas
            this.plotVisualizationObjectView.Canvas.Children.Remove(segment.LinePath);
            this.plotVisualizationObjectView.Canvas.Children.Remove(segment.MarkerPath);
            this.plotVisualizationObjectView.Canvas.Children.Remove(segment.RangePath);

            // Remove the segment
            this.segments[seriesKey].Remove(segment);

            // If no other segments are left
            if (!this.segments[seriesKey].Any())
            {
                // Remove the series key
                this.segments.Remove(seriesKey);
                this.currentSegment.Remove(seriesKey);
            }
            else
            {
                // O/w set the current segment for this series key to null if
                // we just removed the current segment.
                if (this.currentSegment[seriesKey] == segment)
                {
                    this.currentSegment[seriesKey] = null;
                }
            }
        }

        /// <summary>
        /// Re-renders the markers.
        /// </summary>
        public void ReRenderMarkers()
        {
            if (this.segments.Count == 0)
            {
                return;
            }

            foreach (var key in this.segments.Keys)
            {
                this.segments[key].ForEach(s => s.ClearMarkers());

                int currentSegment = 0;

                foreach (var value in this.plotVisualizationObjectView.GetDataPoints(key))
                {
                    while (currentSegment < this.segments[key].Count &&
                        this.segments[key][currentSegment].EndTime < value.OriginatingTime)
                    {
                        currentSegment++;
                    }

                    // If we have moved past end of our last segment (this can happen if we
                    // are re-rendering ticks just after new points have been added, but not plotted)
                    if (currentSegment >= this.segments[key].Count)
                    {
                        break;
                    }

                    var time = value.OriginatingTime - this.plotVisualizationObjectView.Navigator.DataRange.StartTime;
                    var point = new Point(
                        time.TotalSeconds,
                        value.Available ? this.plotVisualizationObjectView.GetNumericValue(value.Value) : double.NaN);
                    this.segments[key][currentSegment].RenderMarker(point);
                }
            }
        }

        /// <summary>
        /// Clears the plot.
        /// </summary>
        public void Clear()
        {
            this.segments.Clear();
            this.currentSegment.Clear();
            this.plotVisualizationObjectView.Canvas.Children.Clear();
        }

        /// <summary>
        /// Implements a plot segment.
        /// </summary>
        public class Segment
        {
            /// <summary>
            /// Gets the maximum segment capacity.
            /// </summary>
            public const uint Capacity = 100;

            private readonly IPlotVisualizationObjectView<TKey, TData> parent;
            private readonly PathGeometry lineGeometry;
            private readonly PathFigure lineFigure;
            private readonly PathGeometry markerGeometry;
            private readonly PathGeometry rangeGeometry;
            private readonly PathFigure rangeFigure;
            private bool previousPointIsValid = false;

            /// <summary>
            /// Initializes a new instance of the <see cref="Segment"/> class.
            /// </summary>
            /// <param name="parent">The parent.</param>
            public Segment(IPlotVisualizationObjectView<TKey, TData> parent)
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

                this.previousPointIsValid = false;
            }

            /// <summary>
            /// Gets the marker path.
            /// </summary>
            public Path MarkerPath { get; private set; }

            /// <summary>
            /// Gets the line path.
            /// </summary>
            public Path LinePath { get; private set; }

            /// <summary>
            /// Gets the range path.
            /// </summary>
            public Path RangePath { get; private set; }

            /// <summary>
            /// Gets the point count.
            /// </summary>
            public int PointCount { get; private set; }

            /// <summary>
            /// Gets or sets the start time.
            /// </summary>
            public DateTime? StartTime { get; set; }

            /// <summary>
            /// Gets or sets the end time.
            /// </summary>
            public DateTime EndTime { get; set; } = DateTime.MaxValue; // Start out at MaxValue so that an initial empty segment doesn't get removed from a data range change

            /// <summary>
            /// Adds a point to the segment.
            /// </summary>
            /// <param name="time">The time for the point.</param>
            /// <param name="point">The point to add.</param>
            public void AddPoint(DateTime time, Point point)
            {
                if (!this.StartTime.HasValue)
                {
                    this.StartTime = time;
                }

                // Don't set EndTime here. Segment end time is now set by its position within the sorted list of segments.
                this.AddLine(point);
                this.RenderMarker(point);
            }

            /// <summary>
            /// Plots a line in the segment from the last point to a specified point.
            /// </summary>
            /// <param name="point">The new point.</param>
            public void AddLine(Point point)
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

                InterpolationStyle interpolationStyle = this.parent.InterpolationStyle;

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

            /// <summary>
            /// Plots a range between two points.
            /// </summary>
            /// <param name="point1">The first point.</param>
            /// <param name="point2">The second point.</param>
            public void PlotRange(Point point1, Point point2)
            {
                if (point1 != point2)
                {
                    this.rangeFigure.Segments.Add(new LineSegment(point1, false));
                    this.rangeFigure.Segments.Add(new LineSegment(point2, true));
                }
            }

            /// <summary>
            /// Renders a marker at a specified point.
            /// </summary>
            /// <param name="point">The point to add a marker to.</param>
            public void RenderMarker(Point point)
            {
                // Don't render markers at discontinuities
                if (this.parent.MarkerStyle == MarkerStyle.None || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
                {
                    return;
                }

                var markerSizeX = this.parent.MarkerSize / this.parent.ScaleTransform.ScaleX;
                var markerSizeY = this.parent.MarkerSize / this.parent.ScaleTransform.ScaleY;

                var markerFigure = new PathFigure();

                if (this.parent.MarkerStyle == MarkerStyle.Plus)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y + (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y), true));
                }
                else if (this.parent.MarkerStyle == MarkerStyle.Cross)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), false));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.MarkerStyle == MarkerStyle.Asterisk)
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
                else if (this.parent.MarkerStyle == MarkerStyle.Circle)
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
                else if (this.parent.MarkerStyle == MarkerStyle.Square)
                {
                    markerFigure.StartPoint = new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.MarkerStyle == MarkerStyle.DownTriangle)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y + (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y - (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y + (markerSizeY / 2)), true));
                }
                else if (this.parent.MarkerStyle == MarkerStyle.UpTriangle)
                {
                    markerFigure.StartPoint = new Point(point.X, point.Y - (markerSizeY / 2));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X - (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X + (markerSizeX / 2), point.Y + (markerSizeY / 2)), true));
                    markerFigure.Segments.Add(new LineSegment(new Point(point.X, point.Y - (markerSizeY / 2)), true));
                }
                else if (this.parent.MarkerStyle == MarkerStyle.Diamond)
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

            /// <summary>
            /// Clears the markers.
            /// </summary>
            public void ClearMarkers()
            {
                this.markerGeometry.Figures.Clear();
            }
        }
    }
}
