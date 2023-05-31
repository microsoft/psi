// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for TimeIntervalAnnotationVisualizationObjectView.xaml.
    /// </summary>
    public partial class TimeIntervalAnnotationVisualizationObjectView : StreamIntervalVisualizationObjectTimelineCanvasView<TimeIntervalAnnotationVisualizationObject, TimeIntervalAnnotationSet>
    {
        private readonly Grid trackHighlight;
        private readonly List<TimeIntervalAnnotationTrackVisualizationObjectViewItem> trackViews = new ();
        private readonly List<TimeIntervalAnnotationVisualizationObjectViewItem> itemViews = new ();

        /// <summary>
        /// The collection of brushes used in rendering.
        /// </summary>
        private readonly Dictionary<Color, Brush> brushes = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationVisualizationObjectView"/> class.
        /// </summary>
        public TimeIntervalAnnotationVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;

            this.trackHighlight = new Grid
            {
                RenderTransform = new TranslateTransform(),
                IsHitTestVisible = false,
                Height = 0,
            };

            this.Canvas.Children.Add(this.trackHighlight);
        }

        /// <summary>
        /// Returns a brush with the requested System.Drawing.Color from the brushes cache.
        /// </summary>
        /// <param name="systemDrawingColor">The color of the brush to return.</param>
        /// <returns>A brush of the requested color.</returns>
        internal Brush GetBrush(System.Drawing.Color systemDrawingColor)
        {
            var color = Color.FromArgb(systemDrawingColor.A, systemDrawingColor.R, systemDrawingColor.G, systemDrawingColor.B);

            if (!this.brushes.ContainsKey(color))
            {
                this.brushes.Add(color, new SolidColorBrush(color));
            }

            return this.brushes[color];
        }

        /// <inheritdoc/>
        protected override void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is TimeIntervalAnnotationVisualizationObject oldVisualizationObject)
            {
                oldVisualizationObject.TimeIntervalAnnotationEdit -= this.OnVisualizationObjectTimeIntervalAnnotationEdit;
                oldVisualizationObject.TimeIntervalAnnotationDrag -= this.OnVisualizationObjectTimeIntervalAnnotationDrag;
            }

            if (e.NewValue is TimeIntervalAnnotationVisualizationObject newVisualizationObject)
            {
                newVisualizationObject.TimeIntervalAnnotationEdit += this.OnVisualizationObjectTimeIntervalAnnotationEdit;
                newVisualizationObject.TimeIntervalAnnotationDrag += this.OnVisualizationObjectTimeIntervalAnnotationDrag;
            }

            base.OnDataContextChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);
            if (e.PropertyName == nameof(this.StreamVisualizationObject.DisplayData) ||
                e.PropertyName == nameof(this.StreamVisualizationObject.LineWidth) ||
                e.PropertyName == nameof(this.StreamVisualizationObject.Padding) ||
                e.PropertyName == nameof(this.StreamVisualizationObject.FontSize) ||
                e.PropertyName == nameof(this.StreamVisualizationObject.ShowTracks) ||
                e.PropertyName == nameof(this.StreamVisualizationObject.ShowTracksSelection))
            {
                this.OnDisplayDataChanged();
            }
            else if (e.PropertyName == nameof(this.StreamVisualizationObject.TrackUnderMouseIndex))
            {
                this.UpdateTrackHighlight();
            }
        }

        /// <summary>
        /// Implements a response to display data changing.
        /// </summary>
        protected virtual void OnDisplayDataChanged() => this.UpdateView();

        /// <inheritdoc/>
        protected override void OnTransformsChanged()
        {
            base.OnTransformsChanged();

            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Collapsed;
        }

        /// <inheritdoc/>
        protected override void UpdateView()
        {
            // Update the track highlight
            this.UpdateTrackHighlight();

            // Go through the display data and create new items and tracks where
            // necessary
            for (int i = 0; i < this.StreamVisualizationObject.DisplayData.Count; i++)
            {
                if (i >= this.itemViews.Count)
                {
                    var item = new TimeIntervalAnnotationVisualizationObjectViewItem(this, this.StreamVisualizationObject.DisplayData[i]);
                    item.Update(this.StreamVisualizationObject.DisplayData[i], this.StreamVisualizationObject.DisplayData[i].TrackIndex, this.StreamIntervalVisualizationObject.TrackCount);
                    this.itemViews.Add(item);
                }
                else
                {
                    this.itemViews[i].Update(this.StreamVisualizationObject.DisplayData[i], this.StreamVisualizationObject.DisplayData[i].TrackIndex, this.StreamIntervalVisualizationObject.TrackCount);
                }
            }

            // remove the remaining figures
            for (int i = this.StreamVisualizationObject.DisplayData.Count; i < this.itemViews.Count; i++)
            {
                this.itemViews[i].RemoveFromCanvas();
            }

            this.itemViews.RemoveRange(this.StreamVisualizationObject.DisplayData.Count, this.itemViews.Count - this.StreamVisualizationObject.DisplayData.Count);

            // Go through the track views and create new ones where necessary
            for (int i = 0; i < this.StreamVisualizationObject.TrackCount; i++)
            {
                if (i >= this.trackViews.Count)
                {
                    this.trackViews.Add(
                        new TimeIntervalAnnotationTrackVisualizationObjectViewItem(this, i, this.StreamIntervalVisualizationObject.TrackCount, this.StreamIntervalVisualizationObject.GetTrackByIndex(i)));
                }
                else
                {
                    this.trackViews[i].Update(i, this.StreamIntervalVisualizationObject.TrackCount, this.StreamIntervalVisualizationObject.GetTrackByIndex(i));
                }
            }

            // remove the remaining figures
            for (int i = this.StreamVisualizationObject.TrackCount; i < this.trackViews.Count; i++)
            {
                this.trackViews[i].RemoveFromCanvas();
            }

            this.trackViews.RemoveRange(this.StreamVisualizationObject.TrackCount, this.trackViews.Count - this.StreamVisualizationObject.TrackCount);
        }

        private static Color ToMediaColor(System.Drawing.Color color)
            => Color.FromArgb(color.A, color.R, color.G, color.B);

        private void UpdateTrackHighlight()
        {
            if (this.DataContext is TimeIntervalAnnotationVisualizationObject annotationVisualizationObject &&
                annotationVisualizationObject.IsBound &&
                annotationVisualizationObject.TrackCount > 0)
            {
                this.trackHighlight.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30));
                this.trackHighlight.Width = this.Canvas.ActualWidth;
                this.trackHighlight.Height = this.Canvas.ActualHeight / annotationVisualizationObject.TrackCount;
                (this.trackHighlight.RenderTransform as TranslateTransform).X = 0;
                (this.trackHighlight.RenderTransform as TranslateTransform).Y = annotationVisualizationObject.TrackUnderMouseIndex * this.Canvas.ActualHeight / annotationVisualizationObject.TrackCount;
            }
        }

        private void OnVisualizationObjectTimeIntervalAnnotationEdit(object sender, TimeIntervalAnnotationEditEventArgs e)
        {
            // If we were already editing an unrestricted annotation, stop.
            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Collapsed;

            // Check if there's an annotation to edit.
            if (e.DisplayData != null)
            {
                // Check if a finite or an unrestricted annotation value is being edited
                if (e.DisplayData.AnnotationSchema.AttributeSchemas[e.AttributeIndex].ValueSchema is IEnumerableAnnotationValueSchema)
                {
                    this.EditEnumerableAnnotationValue(e.DisplayData, e.AttributeIndex);
                }
                else
                {
                    this.EditAnnotationValue(e.DisplayData, e.AttributeIndex);
                }
            }
        }

        private void OnVisualizationObjectTimeIntervalAnnotationDrag(object sender, EventArgs e)
        {
            // If we dragging an annotation, hide the editor
            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Collapsed;
        }

        private void EditEnumerableAnnotationValue(TimeIntervalAnnotationDisplayData displayData, int trackId)
        {
            // Get the attribute schema
            var attributeSchema = displayData.AnnotationSchema.AttributeSchemas[trackId];

            // Get the collection of possible values
            var attributeSchemaType = attributeSchema.ValueSchema as IEnumerableAnnotationValueSchema;

            // Create a new context menu
            var contextMenu = new ContextMenu();

            // Create a menuitem for each value, with a command to update the value on the annotation.
            foreach (var finiteAnnotationValue in attributeSchemaType.GetPossibleAnnotationValues())
            {
                contextMenu.Items.Add(MenuItemHelper.CreateAnnotationMenuItem(
                    finiteAnnotationValue.ValueAsString,
                    System.Drawing.Color.LightGray,
                    finiteAnnotationValue.FillColor,
                    new PsiCommand(() => displayData.SetAttributeValue(attributeSchema.Name, finiteAnnotationValue))));
            }

            // Add a handler so that the timeline visualization panel continues to receive mouse move messages
            // while the context menu is displayed, and remove the handler once the context menu closes.
            var mouseMoveHandler = new MouseEventHandler(this.FindTimelineVisualizationPanelView().ContextMenuMouseMove);
            contextMenu.AddHandler(MouseMoveEvent, mouseMoveHandler, true);
            contextMenu.Closed += (sender, e) => contextMenu.RemoveHandler(MouseMoveEvent, mouseMoveHandler);

            // Show the context menu
            contextMenu.IsOpen = true;
        }

        private void EditAnnotationValue(TimeIntervalAnnotationDisplayData displayData, int attributeIndex)
        {
            // Get the attribute schema
            var attributeSchema = displayData.AnnotationSchema.AttributeSchemas[attributeIndex];

            // Get the current value
            var attributeValue = displayData.Annotation.AttributeValues[attributeSchema.Name];

            // Style the text box to match the schema of the annotation value
            this.EditUnrestrictedAnnotationTextBox.Foreground = new SolidColorBrush(ToMediaColor(attributeValue.TextColor));
            this.EditUnrestrictedAnnotationTextBox.Background = new SolidColorBrush(ToMediaColor(attributeValue.FillColor));

            // The textbox's tag holds context information to allow us to update the value in the display object when
            // the text in the textbox changes. Note that we must set the correct tag before we set the text, otherwise
            // setting the text will cause it to be copied to the last annotation that we edited.
            this.EditUnrestrictedAnnotationTextBox.Tag = new UnrestrictedAnnotationValueContext(displayData, attributeSchema.Name);
            this.EditUnrestrictedAnnotationTextBox.Text = attributeValue.ValueAsString;

            // Set the textbox position to exactly cover the annotation value
            var navigatorViewDuration = this.Navigator.ViewRange.Duration.TotalSeconds;
            var labelStart = Math.Min(navigatorViewDuration, Math.Max((displayData.StartTime - this.Navigator.ViewRange.StartTime).TotalSeconds, 0));
            var labelEnd = Math.Max(0, Math.Min((displayData.EndTime - this.Navigator.ViewRange.StartTime).TotalSeconds, navigatorViewDuration));

            var verticalSpace = this.StreamVisualizationObject.Padding / this.ScaleTransform.ScaleY;
            var attributeCount = this.StreamVisualizationObject.AttributeCount;
            var totalTrackCount = attributeCount * this.StreamIntervalVisualizationObject.TrackCount;
            var lo = (double)(displayData.TrackIndex * attributeCount + attributeIndex + verticalSpace) / totalTrackCount;
            var hi = (double)(displayData.TrackIndex * attributeCount + attributeIndex + 1 - verticalSpace) / totalTrackCount;

            this.EditUnrestrictedAnnotationTextBox.Width = (labelEnd - labelStart) * this.Canvas.ActualWidth / this.Navigator.ViewRange.Duration.TotalSeconds;
            this.EditUnrestrictedAnnotationTextBox.Height = (hi - lo) * this.Canvas.ActualHeight;
            (this.EditUnrestrictedAnnotationTextBox.RenderTransform as TranslateTransform).X = labelStart * this.Canvas.ActualWidth / this.Navigator.ViewRange.Duration.TotalSeconds;
            (this.EditUnrestrictedAnnotationTextBox.RenderTransform as TranslateTransform).Y = lo * this.Canvas.ActualHeight;

            // Initially select the entire text of the textbox, then show the textbox and set keyboard focus to it.
            this.EditUnrestrictedAnnotationTextBox.SelectAll();
            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Visible;
            this.EditUnrestrictedAnnotationTextBox.Focus();
        }

        private void OnEditUnrestrictedAnnotationTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.EditUnrestrictedAnnotationTextBox.Tag is UnrestrictedAnnotationValueContext context)
            {
                var attributeSchema = context.DisplayData.AnnotationSchema.GetAttributeSchema(context.AttributeName);
                var annotationValue = attributeSchema.ValueSchema.CreateAnnotationValue(this.EditUnrestrictedAnnotationTextBox.Text);
                context.DisplayData.SetAttributeValue(context.AttributeName, annotationValue);
            }
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Collapsed;
        }

        private TimelineVisualizationPanelView FindTimelineVisualizationPanelView()
        {
            var timelinePanelView = this.VisualParent;
            while (timelinePanelView is not TimelineVisualizationPanelView)
            {
                timelinePanelView = VisualTreeHelper.GetParent(timelinePanelView);
            }

            return timelinePanelView as TimelineVisualizationPanelView;
        }

        private class UnrestrictedAnnotationValueContext
        {
            public UnrestrictedAnnotationValueContext(TimeIntervalAnnotationDisplayData displayData, string attributeName)
            {
                this.DisplayData = displayData;
                this.AttributeName = attributeName;
            }

            public TimeIntervalAnnotationDisplayData DisplayData { get; private set; }

            public string AttributeName { get; private set; }
        }
    }
}