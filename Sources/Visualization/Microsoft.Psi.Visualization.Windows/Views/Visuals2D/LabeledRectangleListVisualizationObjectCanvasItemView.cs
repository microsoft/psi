// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a view item for the scatter rectangle visualization object view.
    /// </summary>
    public class LabeledRectangleListVisualizationObjectCanvasItemView :
        VisualizationObjectCanvasItemView<LabeledRectangleListVisualizationObject, Tuple<System.Drawing.Rectangle, string, string>, List<Tuple<System.Drawing.Rectangle, string, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledRectangleListVisualizationObjectCanvasItemView"/> class.
        /// </summary>
        public LabeledRectangleListVisualizationObjectCanvasItemView()
        {
            this.RectangleFigure = new PathFigure
            {
                StartPoint = new Point(0, 0),
                IsClosed = true,
                IsFilled = true,
            };
            this.RectangleFigure.Segments.Add(new LineSegment(new Point(100, 0), true));
            this.RectangleFigure.Segments.Add(new LineSegment(new Point(100, 100), true));
            this.RectangleFigure.Segments.Add(new LineSegment(new Point(0, 100), true));
            this.RectangleFigure.Segments.Add(new LineSegment(new Point(0, 0), true));

            var rectanglePathGeometry = new PathGeometry();
            rectanglePathGeometry.Figures.Add(this.RectangleFigure);

            this.Rectangle = new Path() { Data = rectanglePathGeometry, Stroke = new SolidColorBrush(Colors.Red) };

            this.Label = new Grid { RenderTransform = new TranslateTransform() };
            this.Label.Children.Add(
                new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(3),
                });

            this.UIElements.Add(this.Rectangle);
            this.UIElements.Add(this.Label);
        }

        /// <summary>
        /// Gets the rectangle figure.
        /// </summary>
        public PathFigure RectangleFigure { get; }

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Path Rectangle { get; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        public Grid Label { get; }

        /// <inheritdoc/>
        public override void Configure(
            StreamVisualizationObjectCanvasView<LabeledRectangleListVisualizationObject, List<Tuple<System.Drawing.Rectangle, string, string>>> canvasView,
            LabeledRectangleListVisualizationObject visualizationObject)
        {
            this.Rectangle.Data.Transform = canvasView.TransformGroup;

            // Create bindings for lines
            var binding = new Binding(nameof(LabeledRectangleListVisualizationObject.Color))
            {
                Source = visualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(this.Label, Grid.BackgroundProperty, binding);

            binding = new Binding(nameof(LabeledRectangleListVisualizationObject.Color))
            {
                Source = visualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(this.Rectangle, Shape.StrokeProperty, binding);

            binding = new Binding(nameof(LabeledRectangleListVisualizationObject.LineWidth))
            {
                Source = visualizationObject,
            };
            BindingOperations.SetBinding(this.Rectangle, Shape.StrokeThicknessProperty, binding);

            binding = new Binding(nameof(LabeledRectangleListVisualizationObject.ShowLabel))
            {
                Source = visualizationObject,
                Converter = new Converters.BoolToVisibilityConverter(),
            };
            BindingOperations.SetBinding(this.Label, Grid.VisibilityProperty, binding);
        }

        /// <inheritdoc/>
        public override void UpdateView(
            Tuple<System.Drawing.Rectangle, string, string> item,
            StreamVisualizationObjectCanvasView<LabeledRectangleListVisualizationObject, List<Tuple<System.Drawing.Rectangle, string, string>>> canvasView,
            LabeledRectangleListVisualizationObject visualizationObject)
        {
            var rectangle = item.Item1;
            var label = item.Item2;
            var tooltip = item.Item3;

            this.RectangleFigure.StartPoint = new System.Windows.Point(rectangle.Left, rectangle.Top);
            (this.RectangleFigure.Segments[0] as LineSegment).Point = new System.Windows.Point(rectangle.Left + rectangle.Width, rectangle.Top);
            (this.RectangleFigure.Segments[1] as LineSegment).Point = new System.Windows.Point(rectangle.Left + rectangle.Width, rectangle.Top + rectangle.Height);
            (this.RectangleFigure.Segments[2] as LineSegment).Point = new System.Windows.Point(rectangle.Left, rectangle.Top + rectangle.Height);
            (this.RectangleFigure.Segments[3] as LineSegment).Point = new System.Windows.Point(rectangle.Left, rectangle.Top);

            // update the label
            (this.Label.Children[0] as TextBlock).Text = label;
            this.Label.Width = Math.Max(0, rectangle.Width * canvasView.ScaleTransform.ScaleX);
            this.Label.Height = 30;
            this.Label.ToolTip = tooltip;

            // update the render transform for the label
            (this.Label.RenderTransform as TranslateTransform).X = (rectangle.Left + canvasView.TranslateTransform.X) * canvasView.ScaleTransform.ScaleX;
            (this.Label.RenderTransform as TranslateTransform).Y = (rectangle.Bottom + canvasView.TranslateTransform.Y) * canvasView.ScaleTransform.ScaleY;
        }
    }
}