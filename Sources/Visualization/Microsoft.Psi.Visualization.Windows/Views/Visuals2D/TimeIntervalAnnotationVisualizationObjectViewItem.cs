// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a time interval annotation view item.
    /// </summary>
    internal class TimeIntervalAnnotationVisualizationObjectViewItem
    {
        private readonly TimeIntervalAnnotationVisualizationObjectView parent;
        private readonly List<Path> figures;
        private readonly List<Grid> labels;
        private readonly Path borderPath;
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

            foreach (var attributeSchema in annotationDisplayData.AnnotationSchema.AttributeSchemas)
            {
                var annotationElementFigure = new PathFigure()
                {
                    StartPoint = new Point(0, 0),
                    IsClosed = true,
                    IsFilled = true,
                };

                annotationElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
                annotationElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
                annotationElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));

                var pathGeometry = new PathGeometry() { Transform = this.parent.TransformGroup };
                pathGeometry.Figures.Add(annotationElementFigure);
                var path = new Path() { Data = pathGeometry };
                this.figures.Add(path);

                var labelGrid = new Grid
                {
                    RenderTransform = new TranslateTransform(),
                    IsHitTestVisible = false,
                };

                var textBlock = new TextBlock()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(3),
                    IsHitTestVisible = false,
                };

                labelGrid.Children.Add(textBlock);
                this.labels.Add(labelGrid);

                // Set the colors etc
                var annotationValue = annotationDisplayData.Annotation.AttributeValues[attributeSchema.Name];
                path.Fill = this.parent.GetBrush(annotationValue.FillColor);
                textBlock.Foreground = this.parent.GetBrush(annotationValue.TextColor);
            }

            var borderElementFigure = new PathFigure()
            {
                StartPoint = new Point(0, 0),
                IsClosed = true,
                IsFilled = true,
            };

            borderElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
            borderElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));
            borderElementFigure.Segments.Add(new LineSegment(new Point(0, 0), true));

            var borderPathGeometry = new PathGeometry() { Transform = this.parent.TransformGroup };
            borderPathGeometry.Figures.Add(borderElementFigure);

            this.borderPath = new Path()
            {
                Data = borderPathGeometry,
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 2,
            };

            // Insert the figure and labels in the parent canvas, but at the beginning, starting
            // from index 1. At index 0 we have the track highlight child. Inserting from index
            // 1 ensures that the track label items remain towards the tail of the list of canvas
            // children, making sure they stay on top in z-order.
            int index = 2;
            foreach (var figure in this.figures)
            {
                this.parent.Canvas.Children.Insert(index++, figure);
            }

            foreach (var label in this.labels)
            {
                this.parent.Canvas.Children.Insert(index++, label);
            }

            this.parent.Canvas.Children.Insert(index++, this.borderPath);
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
        /// <param name="trackIndex">The track index to display the item on.</param>
        /// <param name="trackCount">The track count.</param>
        internal void Update(TimeIntervalAnnotationDisplayData annotationDisplayData, int trackIndex, int trackCount)
        {
            // First, check if the annotation display data is in view
            var notInView = annotationDisplayData.EndTime < this.parent.Navigator.ViewRange.StartTime ||
                annotationDisplayData.StartTime > this.parent.Navigator.ViewRange.EndTime;

            // If not in view
            if (notInView)
            {
                // Collapse its elements
                for (int attributeIndex = 0; attributeIndex < annotationDisplayData.AnnotationSchema.AttributeSchemas.Count; attributeIndex++)
                {
                    this.figures[attributeIndex].Visibility = Visibility.Collapsed;
                    this.labels[attributeIndex].Visibility = Visibility.Collapsed;
                    this.borderPath.Visibility = Visibility.Collapsed;
                }

                // And return
                return;
            }

            var verticalSpace = this.parent.StreamVisualizationObject.Padding / this.parent.ScaleTransform.ScaleY;
            var start = (annotationDisplayData.StartTime - this.parent.Navigator.DataRange.StartTime).TotalSeconds;
            var end = (annotationDisplayData.EndTime - this.parent.Navigator.DataRange.StartTime).TotalSeconds;

            // update the label
            var navigatorViewDuration = this.parent.Navigator.ViewRange.Duration.TotalSeconds;
            var labelStart = Math.Min(navigatorViewDuration, Math.Max((annotationDisplayData.StartTime - this.parent.Navigator.ViewRange.StartTime).TotalSeconds, 0));
            var labelEnd = Math.Max(0, Math.Min((annotationDisplayData.EndTime - this.parent.Navigator.ViewRange.StartTime).TotalSeconds, navigatorViewDuration));

            var attributeCount = this.parent.StreamVisualizationObject.AttributeCount;
            var totalTrackCount = attributeCount * trackCount;

            for (int attributeIndex = 0; attributeIndex < annotationDisplayData.AnnotationSchema.AttributeSchemas.Count; attributeIndex++)
            {
                // Set visibility for the figure
                if (this.figures[attributeIndex].Visibility != Visibility.Visible)
                {
                    this.figures[attributeIndex].Visibility = Visibility.Visible;
                }

                // Set visibility for the label
                if (this.labels[attributeIndex].Visibility != Visibility.Visible)
                {
                    this.labels[attributeIndex].Visibility = Visibility.Visible;
                }

                // Get the attribute schema
                var attributeSchema = annotationDisplayData.AnnotationSchema.AttributeSchemas[attributeIndex];

                // Get the current value
                var annotationValue = annotationDisplayData.Annotation.AttributeValues[attributeSchema.Name];

                // Set the colors etc
                if (this.figures[attributeIndex].Fill != this.parent.GetBrush(annotationValue.FillColor))
                {
                    this.figures[attributeIndex].Fill = this.parent.GetBrush(annotationValue.FillColor);
                }

                var lo = (double)(trackIndex * attributeCount + attributeIndex + verticalSpace) / totalTrackCount;
                var hi = (double)(trackIndex * attributeCount + attributeIndex + 1 - verticalSpace) / totalTrackCount;

                var annotationElementFigure = (this.figures[attributeIndex].Data as PathGeometry).Figures[0];
                if (annotationElementFigure.StartPoint.X != start ||
                    annotationElementFigure.StartPoint.Y != end ||
                    (annotationElementFigure.Segments[0] as LineSegment).Point.Y != lo ||
                    (annotationElementFigure.Segments[1] as LineSegment).Point.Y != hi)
                {
                    annotationElementFigure.StartPoint = new Point(start, lo);
                    (annotationElementFigure.Segments[0] as LineSegment).Point = new Point(end, lo);
                    (annotationElementFigure.Segments[1] as LineSegment).Point = new Point(end, hi);
                    (annotationElementFigure.Segments[2] as LineSegment).Point = new Point(start, hi);
                }

                var labelGrid = this.labels[attributeIndex];
                var labelWidth = (int)((labelEnd - labelStart) * this.parent.Canvas.ActualWidth / this.parent.Navigator.ViewRange.Duration.TotalSeconds);
                var labelHeight = (int)((hi - lo) * this.parent.Canvas.ActualHeight);

                // Measure how large the label should be, and if we don't have enough space,
                // activate a tooltip
                var t = new TextBlock()
                {
                    IsHitTestVisible = false,
                    Text = annotationValue.ValueAsString,
                };
                t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (t.DesiredSize.Width > labelWidth || t.DesiredSize.Height > labelHeight)
                {
                    this.figures[attributeIndex].ToolTip = annotationValue.ValueAsString;
                }
                else
                {
                    this.figures[attributeIndex].ToolTip = null;
                }

                // If the label is in view and large enough
                if (labelEnd > 0 && labelStart < navigatorViewDuration && labelWidth > 20)
                {
                    labelGrid.Visibility = Visibility.Visible;
                    if (labelGrid.Width != labelWidth)
                    {
                        labelGrid.Width = labelWidth;
                    }

                    if (labelGrid.Height != labelHeight)
                    {
                        labelGrid.Height = labelHeight;
                    }

                    var textBlock = labelGrid.Children[0] as TextBlock;
                    if (textBlock.Text != annotationValue.ValueAsString)
                    {
                        textBlock.Text = annotationValue.ValueAsString;
                    }

                    if (textBlock.FontSize != this.parent.StreamVisualizationObject.FontSize)
                    {
                        textBlock.FontSize = this.parent.StreamVisualizationObject.FontSize;
                    }

                    if (textBlock.Foreground != this.parent.GetBrush(annotationValue.TextColor))
                    {
                        textBlock.Foreground = this.parent.GetBrush(annotationValue.TextColor);
                    }

                    var transformX = (int)(labelStart * this.parent.Canvas.ActualWidth / this.parent.Navigator.ViewRange.Duration.TotalSeconds);
                    var transformY = (int)(lo * this.parent.Canvas.ActualHeight);
                    var translateTranform = labelGrid.RenderTransform as TranslateTransform;

                    if (translateTranform.X != transformX)
                    {
                        translateTranform.X = transformX;
                    }

                    if (translateTranform.Y != transformY)
                    {
                        translateTranform.Y = transformY;
                    }
                }
                else
                {
                    labelGrid.Visibility = Visibility.Collapsed;
                }
            }

            // Set visibility for the border
            if (this.borderPath.Visibility != Visibility.Visible)
            {
                this.borderPath.Visibility = Visibility.Visible;
            }

            var borderLo = (double)(trackIndex * attributeCount + 0 + verticalSpace) / totalTrackCount;
            var borderHi = (double)(trackIndex * attributeCount + attributeCount - verticalSpace) / totalTrackCount;

            var borderFigure = (this.borderPath.Data as PathGeometry).Figures[0];

            if (borderFigure.StartPoint.X != start ||
                borderFigure.StartPoint.Y != borderLo ||
                (borderFigure.Segments[0] as LineSegment).Point.X != end ||
                (borderFigure.Segments[1] as LineSegment).Point.Y != borderHi)
            {
                borderFigure.StartPoint = new Point(start, borderLo);
                (borderFigure.Segments[0] as LineSegment).Point = new Point(end, borderLo);
                (borderFigure.Segments[1] as LineSegment).Point = new Point(end, borderHi);
                (borderFigure.Segments[2] as LineSegment).Point = new Point(start, borderHi);
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

            this.parent.Canvas.Children.Remove(this.borderPath);
        }

        private void AnnotationDisplayData_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimeIntervalAnnotationDisplayData.IsSelected))
            {
                this.IsSelected = this.annotationDisplayData.IsSelected;
            }
        }
    }
}