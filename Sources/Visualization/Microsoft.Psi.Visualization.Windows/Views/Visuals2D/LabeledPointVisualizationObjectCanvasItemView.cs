// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a labeled point canvas item view.
    /// </summary>
    public class LabeledPointVisualizationObjectCanvasItemView
        : IVisualizationObjectCanvasItemView<Tuple<Point, string, string>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledPointVisualizationObjectCanvasItemView"/> class.
        /// </summary>
        public LabeledPointVisualizationObjectCanvasItemView()
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

        /// <inheritdoc/>
        public ObservableCollection<UIElement> UIElements { get; } = new ObservableCollection<UIElement>();

        /// <summary>
        /// Gets the rectangle.
        /// </summary>
        public Path Point { get; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        public Grid Label { get; }

        /// <inheritdoc/>
        public void Configure(IStreamVisualizationObjectCanvasView canvasView, VisualizationObject visualizationObject)
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
        public void UpdateView(Tuple<Point, string, string> item, IStreamVisualizationObjectCanvasView canvasView)
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
