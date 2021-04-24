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
    /// Implements a canvas view item for the labeled point list visualization object view.
    /// </summary>
    public class LabeledPointListVisualizationObjectCanvasItemView :
        VisualizationObjectCanvasItemView<LabeledPointListVisualizationObject, Tuple<Point, string, string>, List<Tuple<Point, string, string>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledPointListVisualizationObjectCanvasItemView"/> class.
        /// </summary>
        public LabeledPointListVisualizationObjectCanvasItemView()
        {
            var pointGeometry = new EllipseGeometry();

            this.Point = new Path() { Data = pointGeometry, Stroke = new SolidColorBrush(Colors.Red) };

            this.Label = new Grid { RenderTransform = new TranslateTransform() };
            this.Label.Children.Add(
                new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(3),
                });

            this.UIElements.Add(this.Point);
            this.UIElements.Add(this.Label);
        }

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Path Point { get; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        public Grid Label { get; }

        /// <inheritdoc/>
        public override void Configure(
            StreamVisualizationObjectCanvasView<LabeledPointListVisualizationObject, List<Tuple<Point, string, string>>> canvasView,
            LabeledPointListVisualizationObject visualizationObject)
        {
            this.Point.Data.Transform = canvasView.TransformGroup;

            // Create bindings for lines
            var binding = new Binding(nameof(LabeledPointListVisualizationObject.LabelColor))
            {
                Source = visualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(this.Label, Grid.BackgroundProperty, binding);

            binding = new Binding(nameof(LabeledPointListVisualizationObject.FillColor))
            {
                Source = visualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(this.Point, Shape.StrokeProperty, binding);
            BindingOperations.SetBinding(this.Point, Shape.FillProperty, binding);

            binding = new Binding(nameof(LabeledPointListVisualizationObject.Radius))
            {
                Source = visualizationObject,
            };
            BindingOperations.SetBinding(this.Point.Data, EllipseGeometry.RadiusXProperty, binding);
            BindingOperations.SetBinding(this.Point.Data, EllipseGeometry.RadiusYProperty, binding);
        }

        /// <inheritdoc/>
        public override void UpdateView(
            Tuple<Point, string, string> item,
            StreamVisualizationObjectCanvasView<LabeledPointListVisualizationObject, List<Tuple<Point, string, string>>> canvasView,
            LabeledPointListVisualizationObject visualizationObject)
        {
            var point = item.Item1;
            var label = item.Item2;
            var tooltip = item.Item3;

            (this.Point.Data as EllipseGeometry).Center = point;

            // update the label
            (this.Label.Children[0] as TextBlock).Text = label;
            this.Label.Width = 100 * canvasView.ScaleTransform.ScaleX;
            this.Label.Height = 30;
            this.Label.ToolTip = tooltip;
            this.Label.Visibility = (label == default) ? Visibility.Collapsed : Visibility.Visible;

            // update the render transform for the label
            (this.Label.RenderTransform as TranslateTransform).X = (point.X + canvasView.TranslateTransform.X) * canvasView.ScaleTransform.ScaleX;
            (this.Label.RenderTransform as TranslateTransform).Y = (point.Y + canvasView.TranslateTransform.Y) * canvasView.ScaleTransform.ScaleY;
        }
    }
}