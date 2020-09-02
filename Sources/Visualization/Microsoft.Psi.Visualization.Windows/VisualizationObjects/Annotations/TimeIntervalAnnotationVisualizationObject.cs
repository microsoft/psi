// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Controls;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Class implements a <see cref="TimeIntervalAnnotation"/>.
    /// </summary>
    [VisualizationObject("Time Interval Annotations")]
    public class TimeIntervalAnnotationVisualizationObject : TimelineVisualizationObject<TimeIntervalAnnotation>
    {
        private double padding = 0;
        private double lineWidth = 2;
        private double fontSize = 10;
        private string legendValue = string.Empty;

        private RelayCommand nextAnnotationCommand;
        private RelayCommand previousAnnotationCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseEventArgs> mouseMoveCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonUpCommand;
        private RelayCommand<MouseButtonEventArgs> mouseDoubleClickCommand;

        private TimeIntervalAnnotationDisplayData selectedDisplayObject = null;

        private TimeIntervalAnnotationDragInfo annotationDragInfo = null;

        private enum AnnotationEdge
        {
            None,
            Left,
            Right,
        }

        /// <summary>
        /// Gets the data to de displayed in the control.
        /// </summary>
        public int TrackCount { get; private set; }

        /// <summary>
        /// Gets or sets the line width.
        /// </summary>
        public double LineWidth
        {
            get { return this.lineWidth; }
            set { this.Set(nameof(this.LineWidth), ref this.lineWidth, value); }
        }

        /// <summary>
        /// Gets or sets the padding.
        /// </summary>
        public double Padding
        {
            get { return this.padding; }
            set { this.Set(nameof(this.Padding), ref this.padding, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get { return this.fontSize; }
            set { this.Set(nameof(this.FontSize), ref this.fontSize, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether annotation values can be changed.
        /// </summary>
        [DataMember]
        public bool EnableAnnotationValueEdit { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether annotation edges can be dragged.
        /// </summary>
        [DataMember]
        public bool EnableAnnotationDrag { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether annotations can be added or deleted.
        /// </summary>
        [DataMember]
        public bool EnableAddOrDeleteAnnotation { get; set; } = true;

        /// <inheritdoc/>
        [IgnoreDataMember]
        public override Color LegendColor => Colors.White;

        /// <summary>
        /// Gets the annotated event definition.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public AnnotationDefinition Definition { get; private set; }

        /// <summary>
        /// Gets the data to be displayed in the control.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public List<TimeIntervalAnnotationDisplayData> DisplayData { get; private set; } = new List<TimeIntervalAnnotationDisplayData>();

        /// <inheritdoc/>
        public override string LegendValue => this.legendValue;

        /// <inheritdoc/>
        public override bool RequiresSupplementalMetadata => true;

        /// <inheritdoc/>
        public override string IconSource
        {
            get
            {
                if (this.StreamSource != null)
                {
                    return IconSourcePath.Annotation;
                }
                else
                {
                    return IconSourcePath.AnnotationUnbound;
                }
            }
        }

        /// <summary>
        /// Gets the mouse move command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
        {
            get
            {
                if (this.mouseLeftButtonDownCommand == null)
                {
                    // Ensure playback is stopped before exiting
                    this.mouseLeftButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
                        (e) =>
                        {
                            this.DoMouseLeftButtonDown(e);
                        });
                }

                return this.mouseLeftButtonDownCommand;
            }
        }

        /// <summary>
        /// Gets the mouse move command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseEventArgs> MouseMoveCommand
        {
            get
            {
                if (this.mouseMoveCommand == null)
                {
                    // Ensure playback is stopped before exiting
                    this.mouseMoveCommand = new RelayCommand<MouseEventArgs>(
                        (e) =>
                        {
                            this.DoMouseMove(e);
                        });
                }

                return this.mouseMoveCommand;
            }
        }

        /// <summary>
        /// Gets the mouse left button up command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseLeftButtonUpCommand
        {
            get
            {
                if (this.mouseLeftButtonUpCommand == null)
                {
                    // Ensure playback is stopped before exiting
                    this.mouseLeftButtonUpCommand = new RelayCommand<MouseButtonEventArgs>(
                        (e) =>
                        {
                            this.DoMouseLeftButtonUp(e);
                        });
                }

                return this.mouseLeftButtonUpCommand;
            }
        }

        /// <summary>
        /// Gets the mouse double-click command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseDoubleClickCommand
        {
            get
            {
                if (this.mouseDoubleClickCommand == null)
                {
                    // Ensure playback is stopped before exiting
                    this.mouseDoubleClickCommand = new RelayCommand<MouseButtonEventArgs>(
                        (e) =>
                        {
                            this.DoMouseDoubleClick(e);
                        });
                }

                return this.mouseDoubleClickCommand;
            }
        }

        /// <summary>
        /// Gets the next annotated event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand NextAnnotationCommand
        {
            get
            {
                if (this.nextAnnotationCommand == null)
                {
                    this.nextAnnotationCommand = new RelayCommand(
                        () =>
                        {
                            var annotatedEvent = this.Data.Where(m => m.Data.Interval.Left >= this.Navigator.SelectionRange.EndTime).OrderBy(m => m.Data.Interval.Left).FirstOrDefault();
                            if (annotatedEvent == default(Message<TimeIntervalAnnotation>))
                            {
                                annotatedEvent = this.Data.OrderBy(m => m.Data.Interval.Left).FirstOrDefault();
                            }

                            if (annotatedEvent != default(Message<TimeIntervalAnnotation>))
                            {
                                this.Navigator.SelectionRange.SetRange(annotatedEvent.Data.Interval.Left, annotatedEvent.Data.Interval.Right);
                            }
                        });
                }

                return this.nextAnnotationCommand;
            }
        }

        /// <summary>
        /// Gets the previous annotated event command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PreviousAnnotationCommand
        {
            get
            {
                if (this.previousAnnotationCommand == null)
                {
                    this.previousAnnotationCommand = new RelayCommand(
                        () =>
                        {
                            var annotatedEvent = this.Data.Where(m => m.Data.Interval.Right <= this.Navigator.SelectionRange.StartTime).OrderBy(m => m.Data.Interval.Right).LastOrDefault();
                            if (annotatedEvent == default(Message<TimeIntervalAnnotation>))
                            {
                                annotatedEvent = this.Data.OrderBy(m => m.Data.Interval.Right).LastOrDefault();
                            }

                            if (annotatedEvent != default(Message<TimeIntervalAnnotation>))
                            {
                                this.Navigator.SelectionRange.SetRange(annotatedEvent.Data.Interval.Left, annotatedEvent.Data.Interval.Right);
                            }
                        });
                }

                return this.previousAnnotationCommand;
            }
        }

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimeIntervalAnnotationVisualizationObjectView));

        /// <inheritdoc/>
        public override IEnumerable<MenuItem> GetAdditionalContextMenuItems()
        {
            List<MenuItem> menuItems = new List<MenuItem>();

            // Add annotation edit menu items if we're above an annotation
            this.AddAnnotationEditMenuItems(menuItems);

            // Add the add annotation and delete annotation context menu items.
            menuItems.Add(MenuItemHelper.CreateMenuItem(null, "Add Annotation", this.GetAddAnnotationCommand()));
            menuItems.Add(MenuItemHelper.CreateMenuItem(null, "Delete Annotation", this.GetDeleteAnnotationCommand()));

            return menuItems;
        }

        /// <summary>
        /// Sets a value in an annotation.
        /// </summary>
        /// <param name="annotation">The annotation to update.</param>
        /// <param name="valueName">The anme of the value to set.</param>
        /// <param name="value">The new value.</param>
        public void SetAnnotationValue(Message<TimeIntervalAnnotation> annotation, string valueName, object value)
        {
            // Update the value in the annotation
            annotation.Data.Values[valueName] = value;

            // Create the update
            List<StreamUpdate<TimeIntervalAnnotation>> updates = new List<StreamUpdate<TimeIntervalAnnotation>>();
            updates.Add(new StreamUpdate<TimeIntervalAnnotation>(StreamUpdateType.Replace, annotation));

            // Send it to the cache
            DataManager.Instance.UpdateStream(this.StreamSource, updates);
        }

        /// <inheritdoc/>
        protected override void OnStreamBound()
        {
            base.OnStreamBound();
            this.Definition = DataManager.Instance.GetSupplementalMetadata<AnnotationDefinition>(this.StreamSource);
            this.GenerateLegendValue();
        }

        /// <inheritdoc/>
        protected override void OnStreamUnbound()
        {
            base.OnStreamUnbound();
            this.Definition = null;
        }

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.UpdateDisplayData();
            base.OnDataCollectionChanged(e);
        }

        private void AddAnnotationEditMenuItems(List<MenuItem> menuItems)
        {
            // All of the following must be true to edit an annotation:
            //
            // 1) We must be bound to a source
            // 2) Edit annotations values must be enabled.
            // 3) The cursor must be over an annotation.
            if (this.IsBound && this.EnableAnnotationValueEdit)
            {
                int index = this.GetAnnotationIndexByTime(this.Container.Navigator.Cursor);
                if (index >= 0)
                {
                    // Get the annotation to be edited
                    Message<TimeIntervalAnnotation> annotation = this.Data[index];

                    // Get the collection of schema definitions in the annotation
                    foreach (AnnotationSchemaDefinition schemaDefinition in this.Definition.SchemaDefinitions)
                    {
                        // Create a menuitem for the value
                        var valueMenuItem = MenuItemHelper.CreateMenuItem(IconSourcePath.Annotation, schemaDefinition.Name, null);

                        // If this is a finite schema, then get the list of possible values
                        if (schemaDefinition.Schema.IsFiniteAnnotationSchema)
                        {
                            // Get the collection of possible values
                            Type schemaType = schemaDefinition.Schema.GetType();
                            MethodInfo valuesProperty = schemaType.GetProperty("Values").GetGetMethod();
                            IEnumerable values = (IEnumerable)valuesProperty.Invoke(schemaDefinition.Schema, new object[] { });

                            // Create a menuitem for each value, with a command to update the value on the annotation.
                            foreach (object value in values)
                            {
                                var metadata = this.GetAnnotationValueMetadata(value, schemaDefinition.Schema);
                                valueMenuItem.Items.Add(MenuItemHelper.CreateAnnotationMenuItem(
                                    value.ToString(),
                                    metadata.BorderColor,
                                    metadata.FillColor,
                                    new PsiCommand(() => this.SetAnnotationValue(annotation, schemaDefinition.Name, value))));
                            }
                        }
                        else
                        {
                            valueMenuItem.Items.Add(MenuItemHelper.CreateMenuItem(
                                null,
                                annotation.Data.Values[schemaDefinition.Name].ToString(),
                                null));
                        }

                        menuItems.Add(valueMenuItem);
                    }
                }
            }
        }

        private ICommand GetAddAnnotationCommand()
        {
            // All of the following must be true to allow an annotation to be added:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) The cursor must be within the selection markers.
            // 4) Both of the selection markers must be visible in the current view.
            // 5) There must be no annotations between the selection markers.
            DateTime cursor = this.Container.Navigator.Cursor;
            TimeInterval selectionInterval = this.Container.Navigator.SelectionRange.AsTimeInterval;
            TimeInterval viewInterval = this.Container.Navigator.ViewRange.AsTimeInterval;

            if (this.IsBound &&
                this.EnableAddOrDeleteAnnotation &&
                selectionInterval.PointIsWithin(cursor) &&
                selectionInterval.IsSubsetOf(viewInterval) &&
                !this.AnnotationIntersectsWith(selectionInterval))
            {
                return new PsiCommand(() => this.AddAnnotation(selectionInterval));
            }

            return null;
        }

        private ICommand GetDeleteAnnotationCommand()
        {
            // All of the following must be true to delete an annotation:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) The cursor must be over an annotation.
            if (this.IsBound && this.EnableAddOrDeleteAnnotation)
            {
                int index = this.GetAnnotationIndexByTime(this.Container.Navigator.Cursor);
                if (index >= 0)
                {
                    return new PsiCommand(() => this.DeleteAnnotation(this.Data[index]));
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a new annotation.
        /// </summary>
        /// <param name="timeInterval">The interval over which the annotation should occur.</param>
        private void AddAnnotation(TimeInterval timeInterval)
        {
            // Create the annotation using the schema definition
            TimeIntervalAnnotation annotation = this.Definition.CreateTimeIntervalAnnotation(timeInterval);

            // Create a message for the annotation
            Message<TimeIntervalAnnotation> message = new Message<TimeIntervalAnnotation>(annotation, annotation.Interval.Right, annotation.Interval.Right, 0, 0);

            // Update the stream with the new annotation
            DataManager.Instance.UpdateStream(this.StreamSource, new StreamUpdate<TimeIntervalAnnotation>[] { new StreamUpdate<TimeIntervalAnnotation>(StreamUpdateType.Add, message) });
        }

        /// <summary>
        /// Deletes an existing annotation.
        /// </summary>
        private void DeleteAnnotation(Message<TimeIntervalAnnotation> annotation)
        {
            // If the annotation is currently selected, then deselect it
            if (this.selectedDisplayObject != null && this.selectedDisplayObject.Annotation == annotation)
            {
                this.SelectDisplayObject(null);
            }

            // Create the list of stream updates
            List<StreamUpdate<TimeIntervalAnnotation>> updates = new List<StreamUpdate<TimeIntervalAnnotation>>();

            // Add an update to delete the annotation
            updates.Add(new StreamUpdate<TimeIntervalAnnotation>(StreamUpdateType.Delete, annotation));

            // Update the stream
            DataManager.Instance.UpdateStream(this.StreamSource, updates);
        }

        private bool AnnotationIntersectsWith(TimeInterval timeInterval)
        {
            // If there's no annotations at all, we're done
            if ((this.Data == null) || (this.Data.Count <= 0))
            {
                return false;
            }

            // Find the nearest annotation to the left edge of the interval
            int index = IndexHelper.GetIndexForTime(timeInterval.Left, this.Data.Count, (idx) => this.Data[idx].Data.Interval.Right, SnappingBehavior.Nearest);

            // Check if the annotation intersects with the interval, then keep walking to the right until
            // we find an annotation within the interval or we go past the right hand side of the interval.
            while (index < this.Data.Count)
            {
                TimeIntervalAnnotation annotation = this.Data[index].Data;

                // Check if the annotation intersects with the interval
                if (timeInterval.IntersectsWith(annotation.Interval))
                {
                    return true;
                }

                // Check if the annotation is completely to the right of the interval
                if (timeInterval.Right <= annotation.Interval.Left)
                {
                    return false;
                }

                index++;
            }

            return false;
        }

        private void UpdateDisplayData()
        {
            // Rebuild the data
            this.RaisePropertyChanging(nameof(this.DisplayData));
            this.DisplayData.Clear();

            foreach (Message<TimeIntervalAnnotation> annotationMessage in this.Data)
            {
                this.DisplayData.Add(new TimeIntervalAnnotationDisplayData(this, annotationMessage, this.Definition));
            }

            this.RaisePropertyChanged(nameof(this.DisplayData));
        }

        private void DoMouseDoubleClick(MouseButtonEventArgs e)
        {
            // Get the timeline scroller
            TimelineScroller timelineScroller = this.FindTimelineScroller(e.Source);

            // Get the time at the mouse cursor
            DateTime cursorTime = this.GetTimeAtMousePointer(e.GetPosition(e.Source as IInputElement), timelineScroller, false);

            // Get the item (if any) that straddles this time
            int index = this.GetAnnotationIndexByTime(cursorTime);
            if (index > -1)
            {
                // Get the annotation that was hit
                Message<TimeIntervalAnnotation> annotation = this.Data[index];

                // Set the navigator selection to the bounds of the annotation
                TimeInterval annotationRange = annotation.Data.Interval;
                VisualizationContext.Instance.VisualizationContainer.Navigator.SelectionRange.SetRange(annotationRange);

                // If the shift key was down, then also zoom to the annotation (with 10% empty space to the left and right)
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    double bufferSeconds = annotationRange.Span.TotalSeconds * 0.1d;
                    VisualizationContext.Instance.VisualizationContainer.Navigator.ViewRange.SetRange(annotationRange.Left.AddSeconds(-bufferSeconds), annotationRange.Right.AddSeconds(bufferSeconds));
                }
            }
        }

        private void DoMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // Get the timeline scroller
            TimelineScroller timelineScroller = this.FindTimelineScroller(e.Source);

            // Get the time at the mouse cursor
            DateTime cursorTime = this.GetTimeAtMousePointer(e.GetPosition(e.Source as IInputElement), timelineScroller, false);

            // Get the item (if any) that straddles this time
            int index = this.GetAnnotationIndexByTime(cursorTime);
            if (index > -1)
            {
                // Get the annotation that was hit
                Message<TimeIntervalAnnotation> annotation = this.Data[index];

                Canvas canvas = this.FindCanvas(e.Source);

                // Check if the mouse is over an edge of the annotation
                AnnotationEdge annotationEdge = this.MouseOverAnnotationEdge(cursorTime, this.Data[index].Data, timelineScroller);
                if (annotationEdge != AnnotationEdge.None && this.EnableAnnotationDrag)
                {
                    // Get the previous and next annotations (if any) and check if they abut the annotation whose edge we're going to drag
                    Message<TimeIntervalAnnotation>? previousAnnotation = index > 0 ? this.Data[index - 1] : (Message<TimeIntervalAnnotation>?)null;
                    bool previousAnnotationAbuts = previousAnnotation != null && previousAnnotation.Value.Data.Interval.Right == annotation.Data.Interval.Left;
                    Message<TimeIntervalAnnotation>? nextAnnotation = index < this.Data.Count - 1 ? this.Data[index + 1] : (Message<TimeIntervalAnnotation>?)null;
                    bool nextAnnotationAbuts = nextAnnotation != null && nextAnnotation.Value.Data.Interval.Left == annotation.Data.Interval.Right;

                    // If the ALT key is down, then we will not try to move the annotation that abuts this one
                    bool moveNeighborAnnotation = !Keyboard.IsKeyDown(Key.LeftAlt) && !Keyboard.IsKeyDown(Key.RightAlt);

                    // Work out the minimum and maximum times we can drag the annotation's edge to
                    DateTime minTime;
                    DateTime maxTime;
                    if (annotationEdge == AnnotationEdge.Left)
                    {
                        maxTime = annotation.Data.Interval.Right;
                        if (previousAnnotation == null)
                        {
                            minTime = this.Navigator.ViewRange.StartTime;
                        }
                        else if (previousAnnotationAbuts && moveNeighborAnnotation)
                        {
                            minTime = previousAnnotation.Value.Data.Interval.Left;
                        }
                        else
                        {
                            minTime = previousAnnotation.Value.Data.Interval.Right;
                        }
                    }
                    else
                    {
                        minTime = annotation.Data.Interval.Left;
                        if (nextAnnotation == null)
                        {
                            maxTime = this.Navigator.ViewRange.EndTime;
                        }
                        else if (nextAnnotationAbuts && moveNeighborAnnotation)
                        {
                            maxTime = nextAnnotation.Value.Data.Interval.Right;
                        }
                        else
                        {
                            maxTime = nextAnnotation.Value.Data.Interval.Left;
                        }
                    }

                    // Create the drag data that specifies which annotation(s) to drag, and the minimum and maximum time we can drag to
                    if (annotationEdge == AnnotationEdge.Right)
                    {
                        this.annotationDragInfo = new TimeIntervalAnnotationDragInfo(annotation, moveNeighborAnnotation && nextAnnotationAbuts ? nextAnnotation : null, minTime, maxTime);
                    }
                    else
                    {
                        this.annotationDragInfo = new TimeIntervalAnnotationDragInfo(moveNeighborAnnotation && previousAnnotationAbuts ? previousAnnotation : null, annotation, minTime, maxTime);
                    }
                }
                else
                {
                    this.SelectDisplayObject(annotation.Data);
                }

                e.Handled = true;
            }
            else
            {
                this.SelectDisplayObject(null);
            }
        }

        private void DoMouseMove(MouseEventArgs e)
        {
            if (this.annotationDragInfo != null)
            {
                this.DragAnnotationEdge(e);
                e.Handled = true;
            }
            else
            {
                // Get the timeline scroller and the canvas
                TimelineScroller timelineScroller = this.FindTimelineScroller(e.Source);
                Canvas canvas = this.FindCanvas(e.Source);

                // Get the time at the mouse cursor
                DateTime cursorTime = this.GetTimeAtMousePointer(e.GetPosition(e.Source as IInputElement), timelineScroller, false);

                // Get the item (if any) that straddles this time
                if (this.EnableAnnotationDrag)
                {
                    int index = this.GetAnnotationIndexByTime(cursorTime);
                    if (index > -1)
                    {
                        AnnotationEdge annotationEdge = this.MouseOverAnnotationEdge(cursorTime, this.Data[index].Data, timelineScroller);
                        if (annotationEdge != AnnotationEdge.None)
                        {
                            canvas.Cursor = Cursors.SizeWE;
                        }
                        else
                        {
                            canvas.Cursor = Cursors.Arrow;
                        }
                    }
                    else
                    {
                        canvas.Cursor = Cursors.Arrow;
                    }
                }
            }
        }

        private void DoMouseLeftButtonUp(MouseEventArgs e)
        {
            if (this.annotationDragInfo != null)
            {
                if (this.annotationDragInfo.LeftAnnotationMessage.HasValue)
                {
                    this.UpdateAnnotationMessageTime(this.annotationDragInfo.LeftAnnotationMessage.Value);
                }

                if (this.annotationDragInfo.RightAnnotationMessage.HasValue)
                {
                    this.UpdateAnnotationMessageTime(this.annotationDragInfo.RightAnnotationMessage.Value);
                }

                this.annotationDragInfo = null;
            }

            Canvas canvas = this.FindCanvas(e.Source);
        }

        private void SelectDisplayObject(TimeIntervalAnnotation annotation)
        {
            TimeIntervalAnnotationDisplayData displayObject = this.DisplayData.FirstOrDefault(d => d.Annotation.Data.Interval.Right == annotation?.Interval.Right);
            if (displayObject != this.selectedDisplayObject)
            {
                if (this.selectedDisplayObject != null)
                {
                    this.selectedDisplayObject.IsSelected = false;
                    VisualizationContext.Instance.DisplayObjectProperties(null);
                }

                if (displayObject != null)
                {
                    displayObject.IsSelected = true;
                    VisualizationContext.Instance.DisplayObjectProperties(displayObject);
                }
                else
                {
                    VisualizationContext.Instance.DisplayObjectProperties(this);
                }

                this.selectedDisplayObject = displayObject;
            }
        }

        /// <summary>
        /// Updates the time and originating time of an annotation message to match the values
        /// contained in the annotation after dragging the annotation's start or end edge.
        /// </summary>
        /// <param name="message">The message to update.</param>
        private void UpdateAnnotationMessageTime(Message<TimeIntervalAnnotation> message)
        {
            // NOTE: We can't just do an update to the existing message because the message's OriginationTime is the collection
            // key, and we're changing that value.  So we need to delete the existing item and add the updated one
            List<StreamUpdate<TimeIntervalAnnotation>> updates = new List<StreamUpdate<TimeIntervalAnnotation>>();
            updates.Add(new StreamUpdate<TimeIntervalAnnotation>(StreamUpdateType.Delete, message));
            updates.Add(new StreamUpdate<TimeIntervalAnnotation>(StreamUpdateType.Add, new Message<TimeIntervalAnnotation>(message.Data, message.Data.Interval.Right, message.Data.Interval.Right, message.SourceId, message.SequenceId)));

            DataManager.Instance.UpdateStream(this.StreamSource, updates);
        }

        private int GetAnnotationIndexByTime(DateTime time)
        {
            if ((this.Data != null) && (this.Data.Count > 0))
            {
                return this.GetTimeIntervalItemIndexByTime(time, this.Data.Count, (idx) => this.Data[idx].Data.Interval.Left, (idx) => this.Data[idx].Data.Interval.Right);
            }

            return -1;
        }

        private int GetTimeIntervalItemIndexByTime(DateTime time, int count, Func<int, DateTime> startTimeAtIndex, Func<int, DateTime> endTimeAtIndex)
        {
            if (count < 1)
            {
                return -1;
            }

            int lo = 0;
            int hi = count - 1;
            while ((lo != hi - 1) && (lo != hi))
            {
                var val = (lo + hi) / 2;
                if (endTimeAtIndex(val) < time)
                {
                    lo = val;
                }
                else if (startTimeAtIndex(val) > time)
                {
                    hi = val;
                }
                else
                {
                    return val;
                }
            }

            // If lo and hi differ by 1, then either of those value could be straddled by the first or last
            // annotation. If lo and hi are both 0 then there's only 1 element so we should test it as well.
            if (hi - lo <= 1)
            {
                if ((endTimeAtIndex(hi) >= time) && (startTimeAtIndex(hi) <= time))
                {
                    return hi;
                }

                if ((endTimeAtIndex(lo) >= time) && (startTimeAtIndex(lo) <= time))
                {
                    return lo;
                }
            }

            return -1;
        }

        private AnnotationEdge MouseOverAnnotationEdge(DateTime cursorTime, TimeIntervalAnnotation annotation, TimelineScroller timelineScroller)
        {
            // Work out what time interval is expressed in 3 pixels at the current zoom
            TimeSpan hitTargetWidth = this.PixelsToTimespan(5.0d, timelineScroller);

            // Check if the mouse cursor is within 3 pixels of the left or right edge of the annoation
            if (Math.Abs((annotation.Interval.Left - cursorTime).Ticks) <= hitTargetWidth.Ticks)
            {
                return AnnotationEdge.Left;
            }

            if (Math.Abs((annotation.Interval.Right - cursorTime).Ticks) <= hitTargetWidth.Ticks)
            {
                return AnnotationEdge.Right;
            }

            return AnnotationEdge.None;
        }

        private void DragAnnotationEdge(MouseEventArgs e)
        {
            // Get the timeline scroller
            TimelineScroller timelineScroller = this.FindTimelineScroller(e.Source);

            // Get the time at the mouse cursor
            DateTime cursorTime = this.GetTimeAtMousePointer(e.GetPosition(e.Source as IInputElement), timelineScroller, true);

            // Make sure we stay within bounds
            if (cursorTime < this.annotationDragInfo.MinimumTime)
            {
                cursorTime = this.annotationDragInfo.MinimumTime;
            }

            if (cursorTime > this.annotationDragInfo.MaximumTime)
            {
                cursorTime = this.annotationDragInfo.MaximumTime;
            }

            // Set the new positions of the ends of the annotation(s)
            if (this.annotationDragInfo.LeftAnnotation != null)
            {
                this.annotationDragInfo.LeftAnnotation.Interval = new TimeInterval(this.annotationDragInfo.LeftAnnotation.Interval.Left, cursorTime);
            }

            if (this.annotationDragInfo.RightAnnotation != null)
            {
                this.annotationDragInfo.RightAnnotation.Interval = new TimeInterval(cursorTime, this.annotationDragInfo.RightAnnotation.Interval.Right);
            }

            this.UpdateDisplayData();

            VisualizationContext.Instance.VisualizationContainer.Navigator.Cursor = cursorTime;
        }

        private void DoDrop(DragEventArgs e)
        {
            e.Handled = true;
        }

        private DateTime GetTimeAtMousePointer(Point point, TimelineScroller root, bool useSnap)
        {
            double percent = point.X / root.ActualWidth;
            var viewRange = this.Navigator.ViewRange;
            DateTime time = viewRange.StartTime + TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));

            // If we're currently snapping to some Visualization Object, adjust the time to the timestamp of the nearest message
            DateTime? snappedTime = null;
            if (useSnap)
            {
                IStreamVisualizationObject snapToVisualizationObject = this.Container.SnapToVisualizationObject as IStreamVisualizationObject;
                if (snapToVisualizationObject != null)
                {
                    snappedTime = snapToVisualizationObject.GetSnappedTime(time, SnappingBehavior.Nearest);
                }
            }

            return snappedTime ?? time;
        }

        private TimeSpan PixelsToTimespan(double pixelCount, TimelineScroller rootWindow)
        {
            double percent = pixelCount / rootWindow.ActualWidth;
            var viewRange = this.Navigator.ViewRange;
            return TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));
        }

        private TimelineScroller FindTimelineScroller(object sourceElement)
        {
            // Walk up the visual tree until we either find the
            // Timeline Scroller or fall off the top of the tree
            DependencyObject target = sourceElement as DependencyObject;
            while (target != null && !(target is TimelineScroller))
            {
                target = VisualTreeHelper.GetParent(target);
            }

            return target as TimelineScroller;
        }

        private Canvas FindCanvas(object sourceElement)
        {
            // Walk up the visual tree until we either find the
            // Timeline Scroller or fall off the top of the tree
            DependencyObject target = sourceElement as DependencyObject;
            while (target != null && !(target is Canvas))
            {
                target = VisualTreeHelper.GetParent(target);
            }

            return target as Canvas;
        }

        private void GenerateLegendValue()
        {
            this.RaisePropertyChanging(nameof(this.TrackCount));

            // For now the legend value is simmply a list of all the schema names in the definition
            StringBuilder legend = new StringBuilder();
            List<string> schemaNames = this.Definition.SchemaDefinitions.Select(d => d.Name).ToList();
            foreach (string schemaName in schemaNames)
            {
                if (legend.Length > 0)
                {
                    legend.AppendLine();
                }

                legend.Append(schemaName);
            }

            this.legendValue = legend.ToString();
            this.TrackCount = schemaNames.Count;

            this.RaisePropertyChanged(nameof(this.TrackCount));
        }

        private AnnotationSchemaValueMetadata GetAnnotationValueMetadata(object value, IAnnotationSchema annotationSchema)
        {
            MethodInfo getMetadataProperty = annotationSchema.GetType().GetMethod("GetMetadata");
            return (AnnotationSchemaValueMetadata)getMetadataProperty.Invoke(annotationSchema, new[] { value });
        }
    }
}