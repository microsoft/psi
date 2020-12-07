// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
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
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Class implements a <see cref="TimeIntervalAnnotation"/>.
    /// </summary>
    [VisualizationObject("Time Interval Annotations")]
    public class TimeIntervalAnnotationVisualizationObject : TimelineVisualizationObject<TimeIntervalAnnotation>
    {
        private const string ErrorStreamNotBound = "The visualization object is not currently bound to a stream.";
        private const string ErrorEditingDisabled = "Annotation add/delete is currently disabled in the visualization object properties.";
        private const string ErrorSelectionMarkersUnset = "Both the start and end selection markers must be set.\r\n\r\nYou can set the start and end selection markers with SHIFT + Left mouse button and SHIFT + Right mouse button.";
        private const string ErrorOverlappingAnnotations = "Time interval annotations may not overlap.";

        private double padding = 0;
        private double lineWidth = 2;
        private double fontSize = 10;
        private string legendValue = string.Empty;

        private RelayCommand nextAnnotationCommand;
        private RelayCommand previousAnnotationCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private RelayCommand<MouseEventArgs> mouseMoveCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonUpCommand;
        private RelayCommand<MouseButtonEventArgs> mouseDoubleClickCommand;

        private TimeIntervalAnnotationDisplayData selectedDisplayObject = null;

        private TimeIntervalAnnotationDragInfo annotationDragInfo = null;

        /// <summary>
        /// Event that fires when an annotation value should be edited in the view.
        /// </summary>
        public event EventHandler<TimeIntervalAnnotationEditEventArgs> TimeIntervalAnnotationEdit;

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
        /// Gets the mouse left button down command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
        {
            get
            {
                if (this.mouseLeftButtonDownCommand == null)
                {
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
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownCommand
        {
            get
            {
                if (this.mouseRightButtonDownCommand == null)
                {
                    this.mouseRightButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
                        (e) =>
                        {
                            this.DoMouseRightButtonDown(e);
                        });
                }

                return this.mouseRightButtonDownCommand;
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

            // Add the add annotation context menu item
            menuItems.Add(MenuItemHelper.CreateMenuItem(null, "Add Annotation", this.GetAddAnnotationCommand()));

            // If the mouse is above an existing annotation, add the delete annotation context menu item.
            ICommand deleteCommand = this.GetDeleteAnnotationCommand();
            if (deleteCommand != null)
            {
                menuItems.Add(MenuItemHelper.CreateMenuItem(null, "Delete Annotation", deleteCommand));
            }

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

        private ICommand GetAddAnnotationCommand()
        {
            // All of the following must be true to allow an annotation to be added:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) Both selection markers must be set.
            // 4) There must be no annotations between the selection markers.
            DateTime cursor = this.Container.Navigator.Cursor;
            TimeInterval selectionInterval = this.Container.Navigator.SelectionRange.AsTimeInterval;

            if (!this.IsBound)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorStreamNotBound);
            }

            if (!this.EnableAddOrDeleteAnnotation)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorEditingDisabled);
            }

            if ((selectionInterval.Left <= DateTime.MinValue) || (selectionInterval.Right >= DateTime.MaxValue))
            {
                return this.CreateEditAnnotationErrorCommand(ErrorSelectionMarkersUnset);
            }

            if (this.AnnotationIntersectsWith(selectionInterval))
            {
                return this.CreateEditAnnotationErrorCommand(ErrorOverlappingAnnotations);
            }

            return new PsiCommand(() => this.AddAnnotation(selectionInterval));
        }

        private ICommand GetDeleteAnnotationCommand()
        {
            // All of the following must be true to delete an annotation:
            //
            // 1) We must be bound to a source
            // 2) Add/Delete annotations must be enabled.
            // 3) The mouse cursor must be above an existing annotation.
            TimeInterval selectionInterval = this.Container.Navigator.SelectionRange.AsTimeInterval;

            if (!this.IsBound)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorStreamNotBound);
            }

            if (!this.EnableAddOrDeleteAnnotation)
            {
                return this.CreateEditAnnotationErrorCommand(ErrorEditingDisabled);
            }

            int index = this.GetAnnotationIndexByTime(this.Container.Navigator.Cursor);
            if (index >= 0)
            {
                return new PsiCommand(() => this.DeleteAnnotation(this.Data[index]));
            }

            return null;
        }

        private PsiCommand CreateEditAnnotationErrorCommand(string errorMessage)
        {
            return new PsiCommand(() => new MessageBoxWindow(Application.Current.MainWindow, "Error Editing Annotation", errorMessage, "Close", null).ShowDialog());
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

            // Display the properties of the new annotation
            this.SelectDisplayObject(annotation);
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

                // Check if the annotation is completely to the left of the interval
                // NOTE: By default time intervals are inclusive of their endpoints, so abutting time intervals will
                // test as intersecting. Use a non-inclusive time interval so that we can let annotations abut.
                if (timeInterval.IntersectsWith(new TimeInterval(annotation.Interval.Left, false, annotation.Interval.Right, false)))
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
            // Get the time at the mouse cursor
            DateTime cursorTime = (this.Panel as TimelineVisualizationPanel).GetTimeAtMousePointer(e, false);

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
            TimelineVisualizationPanel timelinePanel = this.Panel as TimelineVisualizationPanel;

            // Get the time at the mouse cursor
            DateTime cursorTime = timelinePanel.GetTimeAtMousePointer(e, false);

            Message<TimeIntervalAnnotation> annotation = default;
            AnnotationEdge annotationEdge = AnnotationEdge.None;

            // Get the item (if any) that straddles this time
            int index = this.GetAnnotationIndexByTime(cursorTime);
            if (index > -1)
            {
                // Get the annotation that was hit
                annotation = this.Data[index];

                // Check if the mouse is over an edge of the annotation
                annotationEdge = this.MouseOverAnnotationEdge(cursorTime, this.Data[index].Data, timelinePanel.GetTimelineScroller(e.Source));
            }

            // If the shift key is down, the user is dropping the start selection marker. If there is no VO currently being snapped
            // to and the mouse is over an annotation edge, then manually set the selection marker right on the edge. Otherwise
            // let the event bubble up to the timeline visualization panel which will set the selection marker in the usual fashion.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if ((VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject == null) && (annotationEdge != AnnotationEdge.None))
                {
                    DateTime selectionMarkerTime = annotationEdge == AnnotationEdge.Left ? annotation.Data.Interval.Left : annotation.Data.Interval.Right;
                    this.Navigator.SelectionRange.SetRange(selectionMarkerTime, this.Navigator.SelectionRange.EndTime >= selectionMarkerTime ? this.Navigator.SelectionRange.EndTime : DateTime.MaxValue);
                    e.Handled = true;
                }
                else
                {
                    return;
                }
            }

            // If we're over an annotation
            if (annotation != default)
            {
                if (annotationEdge == AnnotationEdge.None)
                {
                    // We're over an annotation, but not an annotation edge, display the annotation's properties
                    this.SelectDisplayObject(annotation.Data);

                    // Begin annotation edit if it's enabled
                    if (this.EnableAnnotationValueEdit)
                    {
                        // Work out the track number to be edited based on the mouse position
                        TimelineScroller timelineScroller = timelinePanel.GetTimelineScroller(e.Source);
                        Point point = e.GetPosition(timelineScroller);
                        int trackId = (int)(point.Y / timelineScroller.ActualHeight * (double)annotation.Data.Values.Count);

                        // Find the display data object corresponding to the annotation and fire an edit event to the view
                        TimeIntervalAnnotationDisplayData displayObject = this.DisplayData.FirstOrDefault(d => d.Annotation.Data.Interval.Right == annotation.Data.Interval.Right);
                        this.TimeIntervalAnnotationEdit?.Invoke(this, new TimeIntervalAnnotationEditEventArgs(displayObject, trackId));
                    }
                }

                // Check if we're over an edge and annotation drag is enabled.
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
            }
            else
            {
                // We're not over any annotation, cancel any current edit operation in the view and display the VO's properties
                this.TimeIntervalAnnotationEdit?.Invoke(this, new TimeIntervalAnnotationEditEventArgs(null, 0));
                this.SelectDisplayObject(null);
            }
        }

        private void DoMouseRightButtonDown(MouseButtonEventArgs e)
        {
            TimelineVisualizationPanel timelinePanel = this.Panel as TimelineVisualizationPanel;

            // Get the time at the mouse cursor
            DateTime cursorTime = timelinePanel.GetTimeAtMousePointer(e, false);

            Message<TimeIntervalAnnotation> annotation = default;
            AnnotationEdge annotationEdge = AnnotationEdge.None;

            // Get the item (if any) that straddles this time
            int index = this.GetAnnotationIndexByTime(cursorTime);
            if (index > -1)
            {
                // Get the annotation that was hit
                annotation = this.Data[index];

                // Check if the mouse is over an edge of the annotation
                annotationEdge = this.MouseOverAnnotationEdge(cursorTime, this.Data[index].Data, timelinePanel.GetTimelineScroller(e.Source));
            }

            // If the shift key is down, the user is dropping the end selection marker. If there is no VO currently being snapped
            // to and the mouse is over an annotation edge, then manually set the selection marker right on the edge. Otherwise
            // let the event bubble up to the timeline visualization panel which will set the selection marker in the usual fashion.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if ((VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject == null) && (annotationEdge != AnnotationEdge.None))
                {
                    DateTime selectionMarkerTime = annotationEdge == AnnotationEdge.Left ? annotation.Data.Interval.Left : annotation.Data.Interval.Right;
                    this.Navigator.SelectionRange.SetRange(this.Navigator.SelectionRange.StartTime <= selectionMarkerTime ? this.Navigator.SelectionRange.StartTime : DateTime.MinValue, selectionMarkerTime);
                    e.Handled = true;
                }
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
                TimelineVisualizationPanel timelinePanel = this.Panel as TimelineVisualizationPanel;

                // Get the time at the mouse cursor
                DateTime cursorTime = timelinePanel.GetTimeAtMousePointer(e, false);

                // Get the item (if any) that straddles this time
                if (this.EnableAnnotationDrag)
                {
                    Canvas canvas = this.FindCanvas(e.Source);

                    int index = this.GetAnnotationIndexByTime(cursorTime);
                    if (index > -1)
                    {
                        AnnotationEdge annotationEdge = this.MouseOverAnnotationEdge(cursorTime, this.Data[index].Data, timelinePanel.GetTimelineScroller(e.Source));
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
            double percent = 5.0d / timelineScroller.ActualWidth;
            var viewRange = this.Navigator.ViewRange;
            TimeSpan hitTargetWidth = TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));

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
            // Get the time at the mouse cursor
            DateTime cursorTime = (this.Panel as TimelineVisualizationPanel).GetTimeAtMousePointer(e, true);

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