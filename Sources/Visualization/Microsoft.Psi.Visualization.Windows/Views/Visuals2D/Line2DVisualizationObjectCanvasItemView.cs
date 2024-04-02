// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Collections.Generic;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a view item for the Line2D visualization object view.
    /// </summary>
    public class Line2DVisualizationObjectCanvasItemView : VisualizationObjectCanvasItemView<Line2D?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Line2DVisualizationObjectCanvasItemView"/> class.
        /// </summary>
        public Line2DVisualizationObjectCanvasItemView()
        {
        }

        /// <summary>
        /// Gets the line.
        /// </summary>
        public Path Line { get; } = new Path() { Data = new LineGeometry() };

        /// <inheritdoc/>
        public override void Configure(IStreamVisualizationObjectCanvasView canvasView, VisualizationObject visualizationObject)
        {
            this.Line.Data.Transform = canvasView.TransformGroup;

            // Create bindings for lines
            var binding = new Binding(nameof(LabeledRectangleListVisualizationObject.Color))
            {
                Source = visualizationObject,
                Converter = new Converters.ColorConverter(),
            };
            BindingOperations.SetBinding(this.Line, Shape.StrokeProperty, binding);

            binding = new Binding(nameof(LabeledRectangleListVisualizationObject.LineWidth))
            {
                Source = visualizationObject,
            };
            BindingOperations.SetBinding(this.Line, Shape.StrokeThicknessProperty, binding);
        }

        /// <inheritdoc/>
        public override void UpdateView(Line2D? item, IStreamVisualizationObjectCanvasView canvasView)
        {
            if (item is null)
            {
                if (this.UIElements.Contains(this.Line))
                {
                    this.UIElements.Remove(this.Line);
                }

                return;
            }

            if (!this.UIElements.Contains(this.Line))
            {
                this.UIElements.Add(this.Line);
            }

            var lineGeometry = this.Line.Data as LineGeometry;
            lineGeometry.StartPoint = new System.Windows.Point(item.Value.StartPoint.X, item.Value.StartPoint.Y);
            lineGeometry.EndPoint = new System.Windows.Point(item.Value.EndPoint.X, item.Value.EndPoint.Y);
        }
    }
}