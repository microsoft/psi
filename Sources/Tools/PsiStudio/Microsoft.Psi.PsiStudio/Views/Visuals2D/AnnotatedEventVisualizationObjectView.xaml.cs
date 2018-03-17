// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Extensions.Annotations;
    using Microsoft.Psi.Extensions.Base;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for AnnotatedEventVisualizationObjectView.xaml
    /// </summary>
    public partial class AnnotatedEventVisualizationObjectView : UserControl
    {
        private ObservableCollection<AnnotationTag> tags = new ObservableCollection<AnnotationTag>();
        private ScaleTransform scaleTransform = new ScaleTransform();
        private TranslateTransform translateTransform = new TranslateTransform();
        private Navigator navigator;
        private DateTime? startTime;
        private Path annotation;
        private Point startingPosition;
        private DateTime startingStartTime;
        private DateTime startingEndTime;
        private bool dragging;
        private DragAction dragAction;
        private DateTime dragLeftStop;
        private DateTime dragRightStop;
        private Cursor previousCursor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotatedEventVisualizationObjectView"/> class.
        /// </summary>
        public AnnotatedEventVisualizationObjectView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.AnnotatedEventVisualizationObjectView_DataContextChanged;
            this.SizeChanged += this.AnnotatedEventVisualizationObjectView_SizeChanged;
            this.Unloaded += this.AnnotatedEventVisualizationObjectView_Unloaded;
            this.Loaded += (s, e) => this.Focus();
            this.dragAction = DragAction.None;
        }

        private enum DragAction
        {
            None,
            MoveStart,
            MoveEnd,
            MoveEvent
        }

        /// <summary>
        /// Gets the annotated event visualization object.
        /// </summary>
        public AnnotatedEventVisualizationObject AnnotatedEventVisualizationObject { get; private set; }

        /// <summary>
        /// Update cursor based on mouse position.
        /// </summary>
        /// <param name="pt">Postion of the mouse.</param>
        public void UpdateCursor(Point pt)
        {
            this.dragAction = this.HitTest(this.DynamicCanvas, pt);

            if (this.dragAction == DragAction.MoveStart || this.dragAction == DragAction.MoveEnd)
            {
                Mouse.OverrideCursor = Cursors.SizeWE;
            }
            else if (this.dragAction == DragAction.MoveEvent)
            {
                Mouse.OverrideCursor = Cursors.Hand;
            }
            else
            {
                Mouse.OverrideCursor = this.previousCursor;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            if (this.annotation != null)
            {
                var tag = this.annotation.DataContext as AnnotationTag;
                this.AnnotatedEventVisualizationObject.Container.Navigator.SelectionRange.SetRange(tag.Message.Data.StartTime, tag.Message.Data.EndTime);
            }

            base.OnMouseDoubleClick(e);
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            this.previousCursor = Mouse.OverrideCursor;
            base.OnMouseEnter(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            Mouse.OverrideCursor = this.previousCursor;
            base.OnMouseLeave(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            var pt = e.GetPosition(this.DynamicCanvas);
            this.dragAction = this.HitTest(this.DynamicCanvas, pt);

            if (this.dragAction == DragAction.None)
            {
                this.startingPosition = default(Point);
                this.startingStartTime = default(DateTime);
                this.startingEndTime = default(DateTime);
                return;
            }
            else
            {
                var tag = this.annotation.DataContext as AnnotationTag;
                var data = tag.Message.Data;
                this.dragLeftStop = data.StartTime;
                this.dragRightStop = data.EndTime;

                if (this.dragAction == DragAction.MoveStart || this.dragAction == DragAction.MoveEvent)
                {
                    var leftTag = this.AnnotatedEventVisualizationObject.Data.Where(ae => ae != tag.Message && ae.Data.EndTime <= data.StartTime).OrderBy(ae => ae.Data.EndTime).LastOrDefault();
                    this.dragLeftStop = leftTag == default(Message<AnnotatedEvent>) ? DateTime.MinValue : leftTag.Data.EndTime;
                }

                if (this.dragAction == DragAction.MoveEnd || this.dragAction == DragAction.MoveEvent)
                {
                    var rightTag = this.AnnotatedEventVisualizationObject.Data.Where(ae => ae != tag.Message && ae.Data.StartTime >= data.EndTime).OrderBy(ae => ae.Data.StartTime).FirstOrDefault();
                    this.dragRightStop = rightTag == default(Message<AnnotatedEvent>) ? DateTime.MaxValue : rightTag.Data.StartTime;
                }
            }

            this.DynamicCanvas.CaptureMouse();
            this.startingPosition = pt;
            this.dragging = true;
            base.OnMouseLeftButtonDown(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            this.UpdateCursor(e.GetPosition(this.DynamicCanvas));
            this.startingPosition = default(Point);
            this.dragging = false;
            this.DynamicCanvas.ReleaseMouseCapture();
            base.OnMouseLeftButtonUp(e);
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var pt = e.GetPosition(this.DynamicCanvas);

            // Checking that mouse cursor is at zero to avoid focus issues
            if (pt.X == 0 && pt.Y == 0)
            {
                return;
            }

            if (!this.dragging)
            {
                this.UpdateCursor(pt);
            }
            else
            {
                var tag = this.annotation.DataContext as AnnotationTag;
                var data = tag.Message.Data;
                var delta = (pt.X - this.startingPosition.X) / this.scaleTransform.ScaleX;

                if (this.dragAction == DragAction.MoveStart)
                {
                    data.StartTime = this.startingStartTime.AddSeconds(delta);

                    // ensure start doesn't cross over right stop
                    if (data.StartTime > this.dragRightStop)
                    {
                        data.StartTime = this.dragRightStop;
                    }

                    // ensure start doesn't cross over left stop
                    else if (data.StartTime < this.dragLeftStop)
                    {
                        data.StartTime = this.dragLeftStop;
                    }
                }
                else if (this.dragAction == DragAction.MoveEnd)
                {
                    data.EndTime = this.startingEndTime.AddSeconds(delta);

                    // ensure end doesn't cross over left stop
                    if (data.EndTime < this.dragLeftStop)
                    {
                        data.EndTime = this.dragLeftStop;
                    }

                    // ensure end doesn't cross over right stop
                    else if (data.EndTime > this.dragRightStop)
                    {
                        data.EndTime = this.dragRightStop;
                    }
                }
                else if (this.dragAction == DragAction.MoveEvent)
                {
                    TimeSpan duration = data.Duration;
                    data.StartTime = this.startingStartTime.AddSeconds(delta);
                    data.EndTime = this.startingEndTime.AddSeconds(delta);

                    // ensure start doesn't cross over left stop
                    if (data.StartTime < this.dragLeftStop)
                    {
                        data.StartTime = this.dragLeftStop;
                        data.EndTime = data.StartTime + duration;
                    }

                    // ensure end doesn't cross over right stop
                    else if (data.EndTime > this.dragRightStop)
                    {
                        data.EndTime = this.dragRightStop;
                        data.StartTime = data.EndTime - duration;
                    }
                }

                TimeSpan start = data.StartTime - this.startTime.Value;
                Rect rect = new Rect(start.TotalSeconds, 0.0, data.Duration.TotalSeconds, 1);
                var transform = this.annotation.Data.Transform;
                this.annotation.Data = new RectangleGeometry(rect) { Transform = transform };
                this.UpdateTextPosition(tag);

                this.AnnotatedEventVisualizationObject.IsDirty = true;
            }

            base.OnMouseMove(e);
        }

        private void AnnotatedEventVisualizationObjectView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.AnnotatedEventVisualizationObject = this.DataContext as AnnotatedEventVisualizationObject;
            this.AnnotatedEventVisualizationObject.PropertyChanged += this.AnnotatedEventVisualizationObject_PropertyChanged;
            this.AnnotatedEventVisualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;

            this.navigator = this.AnnotatedEventVisualizationObject.Navigator;
            this.navigator.ViewRange.RangeChanged += this.Navigator_ViewRangeChanged;

            this.Data_CollectionChanged(this.AnnotatedEventVisualizationObject.Data, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            if (this.AnnotatedEventVisualizationObject.Data != null)
            {
                this.AnnotatedEventVisualizationObject.Data.CollectionChanged += this.Data_CollectionChanged;
            }
        }

        private void Configuration_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AnnotatedEventVisualizationObjectConfiguration.TextColor))
            {
                foreach (var tag in this.tags)
                {
                    this.UpdateTextColor(tag);
                }
            }
        }

        private void AnnotatedEventVisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.AnnotatedEventVisualizationObject.Data))
            {
                this.Data_CollectionChanged(this.AnnotatedEventVisualizationObject.Data, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                if (this.AnnotatedEventVisualizationObject.Data != null)
                {
                    this.AnnotatedEventVisualizationObject.Data.CollectionChanged += this.Data_CollectionChanged;
                }
            }
            else if (e.PropertyName == nameof(this.AnnotatedEventVisualizationObject.Configuration))
            {
                this.AnnotatedEventVisualizationObject.Configuration.PropertyChanged += this.Configuration_PropertyChanged;
                foreach (var tag in this.tags)
                {
                    this.UpdateTextColor(tag);
                }
            }
        }

        private void AddTag(Message<AnnotatedEvent> annotatedEvent)
        {
            if (!this.startTime.HasValue)
            {
                this.startTime = annotatedEvent.Data.StartTime;
                this.CalculateXTransform();
            }

            TimeSpan start = annotatedEvent.Data.StartTime - this.startTime.Value;
            Rect rect = new Rect(start.TotalSeconds, 0.0, annotatedEvent.Data.Duration.TotalSeconds, 1);
            var tag = new AnnotationTag(this, annotatedEvent, rect);
            this.tags.Add(tag);
            this.DynamicCanvas.Children.Add(tag.RectanglePath);
            this.DynamicCanvas.Children.Add(tag.TextBlock);
            this.UpdateTextColor(tag);
            this.UpdateTextPosition(tag);
        }

        private void RemoveTag(Message<AnnotatedEvent> annotatedEvent)
        {
            var tag = this.tags.Single(t => t.Message == annotatedEvent);
            this.DynamicCanvas.Children.Remove(tag.RectanglePath);
            this.DynamicCanvas.Children.Remove(tag.TextBlock);
            this.tags.Remove(tag);
        }

        private void ResetTags()
        {
            this.DynamicCanvas.Children.Clear();
            this.tags.Clear();
        }

        private bool CalculateXTransform()
        {
            if (!this.startTime.HasValue)
            {
                return false;
            }

            double oldScaleX = this.scaleTransform.ScaleX;
            double oldTranslateX = this.translateTransform.X;

            var timeSpan = this.navigator.ViewRange.Duration;
            this.scaleTransform.ScaleX = this.ActualWidth / timeSpan.TotalSeconds;
            this.scaleTransform.ScaleY = this.ActualHeight;
            this.translateTransform.X = -(this.navigator.ViewRange.StartTime - this.startTime.Value).TotalSeconds;

            // Adjust the placement of the text blocks every time we re-calculate XTransform.
            foreach (var tag in this.tags)
            {
                this.UpdateTextPosition(tag);
            }

            return oldScaleX != this.scaleTransform.ScaleX || oldTranslateX != this.translateTransform.X;
        }

        private void AnnotatedEventVisualizationObjectView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.CalculateXTransform();
        }

        private void AnnotatedEventVisualizationObjectView_Unloaded(object sender, RoutedEventArgs e)
        {
            ((NavigatorRange)this.navigator.ViewRange).RangeChanged -= this.Navigator_ViewRangeChanged;
            if (this.AnnotatedEventVisualizationObject.Data != null)
            {
                this.AnnotatedEventVisualizationObject.Data.CollectionChanged -= this.Data_CollectionChanged;
            }
        }

        private void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    var annotatedEvent = (Message<AnnotatedEvent>)item;
                    this.AddTag(annotatedEvent);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems)
                {
                    var annotatedEvent = (Message<AnnotatedEvent>)item;
                    this.RemoveTag(annotatedEvent);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.ResetTags();
                if (this.AnnotatedEventVisualizationObject.Data != null && this.AnnotatedEventVisualizationObject.Data.Count > 0)
                {
                    this.CalculateXTransform();
                    foreach (var annotatedEvent in this.AnnotatedEventVisualizationObject.Data)
                    {
                        this.AddTag(annotatedEvent);
                    }
                }
            }
            else
            {
                throw new NotImplementedException($"AnnotatedEventVisualizationObjectView.Data_CollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }
        }

        private void Navigator_ViewRangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.CalculateXTransform();
        }

        private void UpdateTextColor(AnnotationTag tag)
        {
            tag.TextBlock.Foreground = new SolidColorBrush(this.AnnotatedEventVisualizationObject.Configuration.TextColor);
        }

        private void UpdateTextPosition(AnnotationTag tag)
        {
            var delta = this.ActualWidth * (tag.Message.Data.StartTime - this.navigator.ViewRange.StartTime).Ticks / this.navigator.ViewRange.Duration.Ticks;
            var duration = this.ActualWidth * (tag.Message.Data.EndTime - tag.Message.Data.StartTime).Ticks / this.navigator.ViewRange.Duration.Ticks;
            tag.TextBlock.SetValue(Canvas.LeftProperty, (double)delta);
            tag.TextBlock.Height = this.ActualHeight;
            tag.TextBlock.Width = duration;
            tag.TextBlock.Foreground = new SolidColorBrush(this.AnnotatedEventVisualizationObject.Configuration.TextColor);
        }

        private DragAction HitTest(FrameworkElement element, Point pt)
        {
            var hitTolerance = 4;
            var hitGeometry = new RectangleGeometry(new Rect(pt.X - hitTolerance, pt.Y - hitTolerance, hitTolerance * 2, hitTolerance * 2));

            this.annotation = null;
            VisualTreeHelper.HitTest(
                element,
                (potentialHitTestTarget) => { return (potentialHitTestTarget is Path) ? HitTestFilterBehavior.Continue : HitTestFilterBehavior.ContinueSkipSelf; },
                (result) =>
                {
                    this.annotation = result.VisualHit as Path;
                    return HitTestResultBehavior.Stop;
                },
                new GeometryHitTestParameters(hitGeometry));

            if (this.annotation == null)
            {
                return DragAction.None;
            }

            var tag = this.annotation.DataContext as AnnotationTag;
            this.startingStartTime = tag.Message.Data.StartTime;
            this.startingEndTime = tag.Message.Data.EndTime;

            var bounds = this.annotation.Data.GetRenderBounds(new Pen(this.annotation.Stroke, this.annotation.StrokeThickness));
            var originalBounds = bounds;
            bounds.Inflate(hitTolerance, hitTolerance);
            if (bounds.Left <= pt.X && pt.X <= bounds.Left + (hitTolerance * 2))
            {
                // Snap to event ends to simplify annotations
                if (!this.dragging)
                {
                    var x = this.DynamicCanvas.PointToScreen(new Point(originalBounds.Left, 0));
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)x.X, System.Windows.Forms.Cursor.Position.Y);
                }

                return DragAction.MoveStart;
            }

            if (bounds.Right - (hitTolerance * 2) <= pt.X && pt.X <= bounds.Right)
            {
                // Snap to event ends to simplify annotations
                if (!this.dragging)
                {
                    var x = this.DynamicCanvas.PointToScreen(new Point(originalBounds.Right, 0));
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)x.X, System.Windows.Forms.Cursor.Position.Y);
                }

                return DragAction.MoveEnd;
            }
            else if (bounds.Contains(pt))
            {
                return DragAction.MoveEvent;
            }
            else
            {
                Debug.Assert(false, "HitTest found an annotation, but bounds checking didn't reveal a drag action.");
                return DragAction.None;
            }
        }

        private class AnnotationTag : ObservableObject
        {
            private AnnotatedEventVisualizationObjectView parent;
            private AnnotatedEventVisualizationObject vizObj;
            private TransformGroup transformGroup;
            private ScaleTransform scaleTransform;
            private Message<AnnotatedEvent> message;
            private RectangleGeometry rectangleGeometry;
            private Path rectanglePath;
            private TextBlock textBlock;
            private ObservableCollection<object> internalContextMenuItems;
            private ReadOnlyObservableCollection<object> contextMenuItems;

            public AnnotationTag(AnnotatedEventVisualizationObjectView parent, Message<AnnotatedEvent> message, Rect rect)
            {
                this.parent = parent;
                this.vizObj = this.parent.AnnotatedEventVisualizationObject;
                this.message = message;

                this.transformGroup = new TransformGroup();
                this.scaleTransform = parent.scaleTransform;
                this.transformGroup.Children.Add(parent.translateTransform);
                this.transformGroup.Children.Add(parent.scaleTransform);

                // Currently, only support one annotation
                var annotation = message.Data.Annotations[0];
                var schemaValue = this.vizObj.Definition.Schemas[0].Values.FirstOrDefault(v => object.Equals(v.Value, annotation));
                if (schemaValue == null)
                {
                    schemaValue = this.vizObj.Definition.Schemas[0].Values.FirstOrDefault(v => object.Equals(v.Value, null)) ?? this.vizObj.Definition.Schemas[0].Values.FirstOrDefault();
                }

                this.rectangleGeometry = new RectangleGeometry(rect) { Transform = this.transformGroup };
                this.rectanglePath = new Path()
                {
                    Data = this.rectangleGeometry,
                    DataContext = this,
                    ContextMenu = parent.Resources["AnnotationContextMenu"] as ContextMenu,
                    Fill = (schemaValue != null) ? schemaValue.Color.ToMediaBrush() : System.Drawing.Color.Black.ToMediaBrush(),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Tag = annotation
                };

                this.textBlock = new TextBlock()
                {
                    Background = null,
                    IsHitTestVisible = false,
                    Name = "Value",
                    Padding = new Thickness(5),
                    Text = annotation,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };

                // create context menu items
                this.internalContextMenuItems = new ObservableCollection<object>(this.vizObj.Definition.Schemas);
                this.contextMenuItems = new ReadOnlyObservableCollection<object>(this.internalContextMenuItems);
                this.rectanglePath.ContextMenu.ItemsSource = this.contextMenuItems;

                // add in separators (nulls) and commands (tuple<string, ICommand, object>)
                this.internalContextMenuItems.Add(null);
                this.internalContextMenuItems.Add(Tuple.Create<string, ICommand, object>("Set value...", parent.AnnotatedEventVisualizationObject.SetValueCommand, this.Message.Data));
                this.internalContextMenuItems.Add(null);
                this.internalContextMenuItems.Add(Tuple.Create<string, ICommand, object>("Delete", parent.AnnotatedEventVisualizationObject.DeleteEventCommand, this.Message));
                this.internalContextMenuItems.Add(null);
                this.internalContextMenuItems.Add(Tuple.Create<string, ICommand, object>("Save Annotations", parent.AnnotatedEventVisualizationObject.SaveAnnotationsCommand, null));
            }

            public ReadOnlyObservableCollection<object> ContexMenuItems => this.contextMenuItems;

            public Message<AnnotatedEvent> Message
            {
                get { return this.message; }
                set { this.Set(nameof(this.Message), ref this.message, value); }
            }

            public AnnotatedEventVisualizationObjectView Parent
            {
                get { return this.parent; }
                set { this.Set(nameof(this.Parent), ref this.parent, value); }
            }

            public Path RectanglePath
            {
                get { return this.rectanglePath; }
                set { this.Set(nameof(this.RectanglePath), ref this.rectanglePath, value); }
            }

            public TextBlock TextBlock
            {
                get { return this.textBlock; }
                set { this.Set(nameof(this.TextBlock), ref this.textBlock, value); }
            }
        }
    }
}