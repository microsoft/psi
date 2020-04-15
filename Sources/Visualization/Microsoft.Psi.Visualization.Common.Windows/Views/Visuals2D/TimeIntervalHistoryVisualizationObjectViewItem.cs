// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a time interval history view item.
    /// </summary>
    internal class TimeIntervalHistoryVisualizationObjectViewItem
    {
        private readonly TimeIntervalHistoryVisualizationObjectView parent;

        private readonly Grid label;

        private readonly Path figure;

        private readonly PathFigure timeIntervalFigure;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalHistoryVisualizationObjectViewItem"/> class.
        /// </summary>
        /// <param name="parent">The parent view.</param>
        internal TimeIntervalHistoryVisualizationObjectViewItem(TimeIntervalHistoryVisualizationObjectView parent)
        {
            this.parent = parent;

            this.timeIntervalFigure = new PathFigure
            {
                StartPoint = new Point(0, 0),
                IsClosed = true,
                IsFilled = true,
            };
            this.timeIntervalFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
            this.timeIntervalFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
            this.timeIntervalFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
            this.timeIntervalFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
            this.timeIntervalFigure.Segments.Add(new LineSegment(new Point(0, 0), true));

            var timeIntervalPathGeometry = new PathGeometry() { Transform = this.parent.TransformGroup };
            timeIntervalPathGeometry.Figures.Add(this.timeIntervalFigure);

            this.figure = new Path() { Data = timeIntervalPathGeometry };

            this.parent.Canvas.Children.Add(this.figure);

            this.label = new Grid
            {
                RenderTransform = new TranslateTransform(),
            };

            var textBlock = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(3),
            };

            this.label.Children.Add(textBlock);

            this.parent.Canvas.Children.Add(this.label);

            this.SetupBindings();
        }

        /// <summary>
        /// Update the view item from the given data.
        /// </summary>
        /// <param name="data">The data to update the item from.</param>
        internal void Update(TimeIntervalHistoryVisualizationObject.TimeIntervalVisualizationObjectData data)
        {
            // determine the correspondance of 5 pixels in the time space
            var offset = 10.0 / this.parent.ScaleTransform.ScaleX;
            var verticalSpace = this.parent.VisualizationObject.LineWidth * 4 / this.parent.ScaleTransform.ScaleY;

            var start = (data.TimeInterval.Left - this.parent.Navigator.DataRange.StartTime).TotalSeconds;
            var end = (data.TimeInterval.Right - this.parent.Navigator.DataRange.StartTime).TotalSeconds;
            var startFull = data.TimeInterval.LeftEndpoint.Inclusive ? start : Math.Min(start + offset, end);
            var endFull = data.TimeInterval.RightEndpoint.Inclusive ? end : Math.Max(end - offset, start);

            var lo = (double)(data.TrackNumber + verticalSpace) / this.parent.VisualizationObject.TrackCount;
            var hi = (double)(data.TrackNumber + 1 - verticalSpace) / this.parent.VisualizationObject.TrackCount;
            var mid = (double)(data.TrackNumber + 0.5) / this.parent.VisualizationObject.TrackCount;

            this.timeIntervalFigure.StartPoint = new Point(start, mid);
            (this.timeIntervalFigure.Segments[0] as LineSegment).Point = new Point(startFull, lo);
            (this.timeIntervalFigure.Segments[1] as LineSegment).Point = new Point(endFull, lo);
            (this.timeIntervalFigure.Segments[2] as LineSegment).Point = new Point(end, mid);
            (this.timeIntervalFigure.Segments[3] as LineSegment).Point = new Point(endFull, hi);
            (this.timeIntervalFigure.Segments[4] as LineSegment).Point = new Point(startFull, hi);

            // update the label
            var navigatorViewDuration = this.parent.Navigator.ViewRange.Duration.TotalSeconds;
            var labelStart = Math.Min(navigatorViewDuration, Math.Max((data.TimeInterval.Left - this.parent.Navigator.ViewRange.StartTime).TotalSeconds, 0));
            var labelEnd = Math.Max(0, Math.Min((data.TimeInterval.Right - this.parent.Navigator.ViewRange.StartTime).TotalSeconds, navigatorViewDuration));

            (this.label.Children[0] as TextBlock).Text = data.Text;
            this.label.Width = (labelEnd - labelStart) * this.parent.Canvas.ActualWidth / this.parent.Navigator.ViewRange.Duration.TotalSeconds;
            this.label.Height = (hi - lo) * this.parent.Canvas.ActualHeight;

            (this.label.RenderTransform as TranslateTransform).X = labelStart * this.parent.Canvas.ActualWidth / this.parent.Navigator.ViewRange.Duration.TotalSeconds;
            (this.label.RenderTransform as TranslateTransform).Y = lo * this.parent.Canvas.ActualHeight;
        }

        /// <summary>
        /// Removes the item from the parent canvas.
        /// </summary>
        internal void RemoveFromCanvas()
        {
            this.parent.Canvas.Children.Remove(this.figure);
            this.parent.Canvas.Children.Remove(this.label);
        }

        /// <summary>
        /// Creates the bindings.
        /// </summary>
        internal void SetupBindings()
        {
            // Create bindings for lines
            var binding = new Binding(nameof(this.parent.VisualizationObject) + "." + nameof(this.parent.VisualizationObject.LineColor))
            {
                Source = this.parent.VisualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(this.figure, Shape.StrokeProperty, binding);

            binding = new Binding(nameof(this.parent.VisualizationObject) + "." + nameof(this.parent.VisualizationObject.LineWidth))
            {
                Source = this.parent.VisualizationObject,
            };
            BindingOperations.SetBinding(this.figure, Shape.StrokeThicknessProperty, binding);

            binding = new Binding(nameof(this.parent.VisualizationObject) + "." + nameof(this.parent.VisualizationObject.FillColor))
            {
                Source = this.parent.VisualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(this.figure, Shape.FillProperty, binding);
        }
    }
}
