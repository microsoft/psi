// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
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
    public partial class TimeIntervalAnnotationVisualizationObjectView : TimelineCanvasVisualizationObjectView<TimeIntervalAnnotationVisualizationObject, TimeIntervalAnnotation>
    {
        private List<TimeIntervalAnnotationVisualizationObjectViewItem> items = new List<TimeIntervalAnnotationVisualizationObjectViewItem>();

        /// <summary>
        /// The collection of brushes used in rendering.
        /// </summary>
        private Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationVisualizationObjectView"/> class.
        /// </summary>
        public TimeIntervalAnnotationVisualizationObjectView()
        {
            this.InitializeComponent();
            this.Canvas = this._DynamicCanvas;
        }

        /// <summary>
        /// Returns a brush with the requested System.Drawing.Color from the brushes cache.
        /// </summary>
        /// <param name="systemDrawingColor">The color of the brush to return.</param>
        /// <returns>A brush of the requested color.</returns>
        internal Brush GetBrush(System.Drawing.Color systemDrawingColor)
        {
            Color color = Color.FromArgb(systemDrawingColor.A, systemDrawingColor.R, systemDrawingColor.G, systemDrawingColor.B);

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
                oldVisualizationObject.TimeIntervalAnnotationEdit -= this.VisualizationObject_TimeIntervalAnnotationEdit;
            }

            if (e.NewValue is TimeIntervalAnnotationVisualizationObject newVisualizationObject)
            {
                newVisualizationObject.TimeIntervalAnnotationEdit += this.VisualizationObject_TimeIntervalAnnotationEdit;
            }

            base.OnDataContextChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnVisualizationObjectPropertyChanged(sender, e);
            if (e.PropertyName == nameof(this.VisualizationObject.DisplayData) ||
                e.PropertyName == nameof(this.VisualizationObject.LineWidth) ||
                e.PropertyName == nameof(this.VisualizationObject.Padding) ||
                e.PropertyName == nameof(this.VisualizationObject.FontSize))
            {
                this.OnDisplayDataChanged();
            }
        }

        /// <summary>
        /// Implements a response to display data changing.
        /// </summary>
        protected virtual void OnDisplayDataChanged()
        {
            this.Rerender();
        }

        /// <inheritdoc/>
        protected override void OnTransformsChanged()
        {
            this.Rerender();
        }

        private static Color ToMediaColor(System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private void VisualizationObject_TimeIntervalAnnotationEdit(object sender, TimeIntervalAnnotationEditEventArgs e)
        {
            // If we were already editing an unrestricted annotation, stop.
            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Collapsed;

            // Check if there's an annotation to edit.
            if (e.DisplayData != null)
            {
                // Check if a finite or an unrestricted annotation value is being edited
                if (e.DisplayData.Definition.SchemaDefinitions[e.TrackId].Schema.IsFiniteAnnotationSchema)
                {
                    this.EditFiniteAnnotationValue(e.DisplayData, e.TrackId);
                }
                else
                {
                    this.EditUnrestrictedAnnotationValue(e.DisplayData, e.TrackId);
                }
            }
        }

        private void EditFiniteAnnotationValue(TimeIntervalAnnotationDisplayData displayData, int trackId)
        {
            // Get the schema definition
            AnnotationSchemaDefinition schemaDefinition = displayData.Definition.SchemaDefinitions[trackId];

            // Get the collection of possible values
            Type schemaType = schemaDefinition.Schema.GetType();
            MethodInfo valuesProperty = schemaType.GetProperty("Values").GetGetMethod();
            IEnumerable values = (IEnumerable)valuesProperty.Invoke(schemaDefinition.Schema, new object[] { });

            // Create a new context menu
            ContextMenu contextMenu = new ContextMenu();

            // Create a menuitem for each value, with a command to update the value on the annotation.
            foreach (object value in values)
            {
                var metadata = this.GetAnnotationValueMetadata(value, schemaDefinition.Schema);
                contextMenu.Items.Add(MenuItemHelper.CreateAnnotationMenuItem(
                    value.ToString(),
                    metadata.BorderColor,
                    metadata.FillColor,
                    new PsiCommand(() => this.VisualizationObject.SetAnnotationValue(displayData.Annotation, schemaDefinition.Name, value))));
            }

            // Add a handler so that the timeline visualization panel continues to receive mouse move messages
            // while the context menu is displayed, and remove the handler once the context menu closes.
            MouseEventHandler mouseMoveHandler = new MouseEventHandler(this.FindTimelineVisualizationPanelView().ContextMenuMouseMove);
            contextMenu.AddHandler(MouseMoveEvent, mouseMoveHandler, true);
            contextMenu.Closed += (sender, e) => contextMenu.RemoveHandler(MouseMoveEvent, mouseMoveHandler);

            // Show the context menu
            contextMenu.IsOpen = true;
        }

        private void EditUnrestrictedAnnotationValue(TimeIntervalAnnotationDisplayData displayData, int trackId)
        {
            // Get the schema definition
            AnnotationSchemaDefinition schemaDefinition = displayData.Definition.SchemaDefinitions[trackId];

            // Get the current value
            object value = displayData.Annotation.Data.Values[schemaDefinition.Name];

            // Get the associated metadata
            AnnotationSchemaValueMetadata schemaMetadata = this.GetAnnotationValueMetadata(value, schemaDefinition.Schema);

            // Style the text box to match the schema of the annotation value
            this.EditUnrestrictedAnnotationTextBox.Foreground = new SolidColorBrush(ToMediaColor(schemaMetadata.TextColor));
            this.EditUnrestrictedAnnotationTextBox.Background = new SolidColorBrush(ToMediaColor(schemaMetadata.FillColor));
            this.EditUnrestrictedAnnotationTextBox.BorderBrush = new SolidColorBrush(ToMediaColor(schemaMetadata.BorderColor));
            this.EditUnrestrictedAnnotationTextBox.BorderThickness = new Thickness(schemaMetadata.BorderWidth);

            // The textbox's tag holds context information to allow us to update the value in the display object when
            // the text in the textbox changes. Note that we must set the correct tag before we set the text, otherwise
            // setting the text will cause it to be copied to the last annotation that we edited.
            this.EditUnrestrictedAnnotationTextBox.Tag = new UnrestrictedAnnotationValueContext(displayData, schemaDefinition.Name);
            this.EditUnrestrictedAnnotationTextBox.Text = value.ToString();

            // Set the textbox position to exactly cover the annotation value
            var navigatorViewDuration = this.Navigator.ViewRange.Duration.TotalSeconds;
            var labelStart = Math.Min(navigatorViewDuration, Math.Max((displayData.StartTime - this.Navigator.ViewRange.StartTime).TotalSeconds, 0));
            var labelEnd = Math.Max(0, Math.Min((displayData.EndTime - this.Navigator.ViewRange.StartTime).TotalSeconds, navigatorViewDuration));

            var verticalSpace = this.VisualizationObject.Padding / this.ScaleTransform.ScaleY;
            var lo = (double)(trackId + verticalSpace) / this.VisualizationObject.TrackCount;
            var hi = (double)(trackId + 1 - verticalSpace) / this.VisualizationObject.TrackCount;

            this.EditUnrestrictedAnnotationTextBox.Width = (labelEnd - labelStart) * this.Canvas.ActualWidth / this.Navigator.ViewRange.Duration.TotalSeconds;
            this.EditUnrestrictedAnnotationTextBox.Height = (hi - lo) * this.Canvas.ActualHeight;
            (this.EditUnrestrictedAnnotationTextBox.RenderTransform as TranslateTransform).X = labelStart * this.Canvas.ActualWidth / this.Navigator.ViewRange.Duration.TotalSeconds;
            (this.EditUnrestrictedAnnotationTextBox.RenderTransform as TranslateTransform).Y = lo * this.Canvas.ActualHeight;

            // Initially select the entire text of the textbox, then show the textbox and set keyboard focus to it.
            this.EditUnrestrictedAnnotationTextBox.SelectAll();
            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Visible;
            this.EditUnrestrictedAnnotationTextBox.Focus();
        }

        private void EditUnrestrictedAnnotationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UnrestrictedAnnotationValueContext context = this.EditUnrestrictedAnnotationTextBox.Tag as UnrestrictedAnnotationValueContext;
            if (context != null)
            {
                context.DisplayData.SetValue(context.ValueName, this.EditUnrestrictedAnnotationTextBox.Text);
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.EditUnrestrictedAnnotationTextBox.Visibility = Visibility.Collapsed;
        }

        private AnnotationSchemaValueMetadata GetAnnotationValueMetadata(object value, IAnnotationSchema annotationSchema)
        {
            MethodInfo getMetadataProperty = annotationSchema.GetType().GetMethod("GetMetadata");
            return (AnnotationSchemaValueMetadata)getMetadataProperty.Invoke(annotationSchema, new[] { value });
        }

        private TimelineVisualizationPanelView FindTimelineVisualizationPanelView()
        {
            DependencyObject timelinePanelView = this.VisualParent;
            while (!(timelinePanelView is TimelineVisualizationPanelView))
            {
                timelinePanelView = VisualTreeHelper.GetParent(timelinePanelView);
            }

            return timelinePanelView as TimelineVisualizationPanelView;
        }

        private void Rerender()
        {
            for (int i = 0; i < this.VisualizationObject.DisplayData.Count; i++)
            {
                TimeIntervalAnnotationVisualizationObjectViewItem item;

                if (i < this.items.Count)
                {
                    item = this.items[i];
                }
                else
                {
                    item = new TimeIntervalAnnotationVisualizationObjectViewItem(this, this.VisualizationObject.DisplayData[i]);
                    this.items.Add(item);
                }

                item.Update(this.VisualizationObject.DisplayData[i]);
            }

            // remove the remaining figures
            for (int i = this.VisualizationObject.DisplayData.Count; i < this.items.Count; i++)
            {
                var item = this.items[this.VisualizationObject.DisplayData.Count];
                item.RemoveFromCanvas();
                this.items.Remove(item);
            }
        }

        private class UnrestrictedAnnotationValueContext
        {
            public UnrestrictedAnnotationValueContext(TimeIntervalAnnotationDisplayData displayData, string valueName)
            {
                this.DisplayData = displayData;
                this.ValueName = valueName;
            }

            public TimeIntervalAnnotationDisplayData DisplayData { get; private set; }

            public string ValueName { get; private set; }
        }
    }
}