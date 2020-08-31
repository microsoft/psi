// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a time interval annotation view item.
    /// </summary>
    internal class TimeIntervalAnnotationVisualizationObjectViewItem
    {
        private readonly TimeIntervalAnnotationVisualizationObjectView parent;
        private readonly List<Path> figures;
        private readonly List<Grid> labels;
        private readonly TimeIntervalAnnotationDisplayData annotationDisplayData;
        private bool isSelected = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationVisualizationObjectViewItem"/> class.
        /// </summary>
        /// <param name="parent">The parent view.</param>
        /// <param name="annotationDisplayData">The data for all annotations in this view item.</param>
        internal TimeIntervalAnnotationVisualizationObjectViewItem(TimeIntervalAnnotationVisualizationObjectView parent, TimeIntervalAnnotationDisplayData annotationDisplayData)
        {
            this.parent = parent;
            this.annotationDisplayData = annotationDisplayData;
            annotationDisplayData.PropertyChanged += this.AnnotationDisplayData_PropertyChanged;

            this.figures = new List<Path>();
            this.labels = new List<Grid>();

            foreach (AnnotationSchemaDefinition schemaDefinition in annotationDisplayData.Definition.SchemaDefinitions)
            {
                PathFigure annotationElementFigure = new PathFigure()
                {
                    StartPoint = new Point(0, 0),
                    IsClosed = true,
                    IsFilled = true,
                };

                annotationElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
                annotationElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
                annotationElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));

                PathGeometry pathGeometry = new PathGeometry() { Transform = this.parent.TransformGroup };
                pathGeometry.Figures.Add(annotationElementFigure);
                Path path = new Path() { Data = pathGeometry };
                this.figures.Add(path);

                Grid labelGrid = new Grid
                {
                    RenderTransform = new TranslateTransform(),
                    IsHitTestVisible = false,
                };

                TextBlock textBlock = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(3),
                    IsHitTestVisible = false,
                };

                labelGrid.Children.Add(textBlock);
                this.labels.Add(labelGrid);

                // Get the current value
                object value = annotationDisplayData.Annotation.Data.Values[schemaDefinition.Name];

                // Get the metadata associtaed with the value
                AnnotationSchemaValueMetadata schemaMetadata = this.GetAnnotationValueMetadata(value, schemaDefinition.Schema);

                // Set the colors etc
                path.Stroke = this.parent.GetBrush(schemaMetadata.BorderColor);
                path.StrokeThickness = schemaMetadata.BorderWidth;
                path.Fill = this.parent.GetBrush(schemaMetadata.FillColor);
                textBlock.Foreground = this.parent.GetBrush(schemaMetadata.TextColor);
            }

            foreach (Path figure in this.figures)
            {
                this.parent.Canvas.Children.Add(figure);
            }

            foreach (Grid label in this.labels)
            {
                this.parent.Canvas.Children.Add(label);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this annotation is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;

            set
            {
                if (this.isSelected != value)
                {
                    this.isSelected = value;

                    foreach (Path figure in this.figures)
                    {
                        if (this.isSelected)
                        {
                            figure.StrokeThickness *= 2.0d;
                        }
                        else
                        {
                            figure.StrokeThickness /= 2.0d;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the view item from the given data.
        /// </summary>
        /// <param name="annotationDisplayData">The data to update the item from.</param>
        internal void Update(TimeIntervalAnnotationDisplayData annotationDisplayData)
        {
            var verticalSpace = this.parent.VisualizationObject.Padding / this.parent.ScaleTransform.ScaleY;
            var start = (annotationDisplayData.StartTime - this.parent.Navigator.DataRange.StartTime).TotalSeconds;
            var end = (annotationDisplayData.EndTime - this.parent.Navigator.DataRange.StartTime).TotalSeconds;

            for (int index = 0; index < annotationDisplayData.Definition.SchemaDefinitions.Count; index++)
            {
                // Get the schema definition
                AnnotationSchemaDefinition schemaDefinition = annotationDisplayData.Definition.SchemaDefinitions[index];

                // Get the current value
                object value = annotationDisplayData.Annotation.Data.Values[schemaDefinition.Name];

                // Get the associated metadata
                AnnotationSchemaValueMetadata schemaMetadata = this.GetAnnotationValueMetadata(value, schemaDefinition.Schema);

                // Set the colors etc
                this.figures[index].Stroke = this.parent.GetBrush(schemaMetadata.BorderColor);
                this.figures[index].StrokeThickness = schemaMetadata.BorderWidth;
                this.figures[index].Fill = this.parent.GetBrush(schemaMetadata.FillColor);

                var lo = (double)(index + verticalSpace) / this.parent.VisualizationObject.TrackCount;
                var hi = (double)(index + 1 - verticalSpace) / this.parent.VisualizationObject.TrackCount;

                PathFigure annotationElementFigure = (this.figures[index].Data as PathGeometry).Figures[0];
                annotationElementFigure.StartPoint = new Point(start, lo);
                (annotationElementFigure.Segments[0] as LineSegment).Point = new Point(end, lo);
                (annotationElementFigure.Segments[1] as LineSegment).Point = new Point(end, hi);
                (annotationElementFigure.Segments[2] as LineSegment).Point = new Point(start, hi);

                // update the label
                var navigatorViewDuration = this.parent.Navigator.ViewRange.Duration.TotalSeconds;
                var labelStart = Math.Min(navigatorViewDuration, Math.Max((annotationDisplayData.StartTime - this.parent.Navigator.ViewRange.StartTime).TotalSeconds, 0));
                var labelEnd = Math.Max(0, Math.Min((annotationDisplayData.EndTime - this.parent.Navigator.ViewRange.StartTime).TotalSeconds, navigatorViewDuration));

                Grid labelGrid = this.labels[index];
                (labelGrid.Children[0] as TextBlock).Text = value?.ToString();
                (labelGrid.Children[0] as TextBlock).FontSize = this.parent.VisualizationObject.FontSize;
                if (schemaDefinition.Schema.IsFiniteAnnotationSchema)
                {
                    (labelGrid.Children[0] as TextBlock).Foreground = this.parent.GetBrush(schemaMetadata.TextColor);
                }

                labelGrid.Width = (labelEnd - labelStart) * this.parent.Canvas.ActualWidth / this.parent.Navigator.ViewRange.Duration.TotalSeconds;
                labelGrid.Height = (hi - lo) * this.parent.Canvas.ActualHeight;
                (labelGrid.RenderTransform as TranslateTransform).X = labelStart * this.parent.Canvas.ActualWidth / this.parent.Navigator.ViewRange.Duration.TotalSeconds;
                (labelGrid.RenderTransform as TranslateTransform).Y = lo * this.parent.Canvas.ActualHeight;
            }
        }

        /// <summary>
        /// Removes the item from the parent canvas.
        /// </summary>
        internal void RemoveFromCanvas()
        {
            foreach (Path figure in this.figures)
            {
                this.parent.Canvas.Children.Remove(figure);
            }

            foreach (Grid label in this.labels)
            {
                this.parent.Canvas.Children.Remove(label);
            }
        }

        private void AnnotationDisplayData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimeIntervalAnnotationDisplayData.IsSelected))
            {
                this.IsSelected = this.annotationDisplayData.IsSelected;
            }
        }

        private AnnotationSchemaValueMetadata GetAnnotationValueMetadata(object value, IAnnotationSchema annotationSchema)
        {
            MethodInfo getMetadataProperty = annotationSchema.GetType().GetMethod("GetMetadata");
            return (AnnotationSchemaValueMetadata)getMetadataProperty.Invoke(annotationSchema, new[] { value });
        }
    }
}