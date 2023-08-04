// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Controls;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that time based visualizers can be rendered in.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationPanel : VisualizationPanel
    {
        /// <summary>
        /// The percentage of the total auto-computed Y axis range that's added
        /// to the actual Y axis range to prevent values at the minimum of maximum
        /// of this range from rendering right at the top or bottom edge of the
        /// timeline panel view.
        /// </summary>
        private const double YAxisAutoComputeModePaddingPercent = 10.0d;

        private Axis yAxis = new ();
        private Axis yAxisPropertyBrowser = new ();

        private TimelinePanelMousePosition mousePosition;
        private Grid viewport;

        private bool showLegend = false;
        private bool showTimeTicks = false;
        private AxisComputeMode axisComputeMode = AxisComputeMode.Auto;

        private RelayCommand clearSelectionCommand;
        private RelayCommand showHideLegendCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private RelayCommand<MouseEventArgs> mouseMoveCommand;
        private RelayCommand<RoutedEventArgs> viewportLoadedCommand;
        private RelayCommand<SizeChangedEventArgs> viewportSizeChangedCommand;
        private RelayCommand setAutoAxisComputeModeCommand;
        private TimelineValueThreshold threshold;

        private TimelineScroller timelineScroller = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineVisualizationPanel"/> class.
        /// </summary>
        public TimelineVisualizationPanel()
        {
            this.Name = "Timeline Panel";
            this.Height = 70;
            this.yAxis.PropertyChanged += this.OnYAxisPropertyChanged;
            this.yAxisPropertyBrowser.PropertyChanged += this.YAxisPropertyBrowser_PropertyChanged;

            this.Threshold = new TimelineValueThreshold();
            this.Threshold.PropertyChanged += this.OnThresholdPropertyChanged;
        }

        /// <summary>
        /// Gets the show/hide legend command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ShowHideLegendCommand
            => this.showHideLegendCommand ??= new RelayCommand(() => this.ShowLegend = !this.ShowLegend);

        /// <summary>
        /// Gets the clear selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearSelectionCommand
            => this.clearSelectionCommand ??= new RelayCommand(
                () => this.Container.Navigator.ClearSelection(),
                () => this.Container.Navigator.CanClearSelection());

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<RoutedEventArgs> ViewportLoadedCommand
            => this.viewportLoadedCommand ??= new RelayCommand<RoutedEventArgs>(e => this.viewport = e.Source as Grid);

        /// <summary>
        /// Gets the items control size changed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<SizeChangedEventArgs> ViewportSizeChangedCommand
            => this.viewportSizeChangedCommand ??= new RelayCommand<SizeChangedEventArgs>(e => this.OnViewportSizeChanged(e));

        /// <summary>
        /// Gets the mouse left button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public override RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
            => this.mouseLeftButtonDownCommand ??= new RelayCommand<MouseButtonEventArgs>(
                e =>
                {
                    // Set the current panel on click
                    if (!this.IsCurrentPanel)
                    {
                        this.IsTreeNodeSelected = true;
                        this.Container.CurrentPanel = this;

                        // If the panel contains any visualization objects, set the first one as selected.
                        if (this.VisualizationObjects.Any())
                        {
                            this.VisualizationObjects[0].IsTreeNodeSelected = true;
                        }
                    }

                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        var cursor = this.Navigator.Cursor;
                        this.Navigator.SelectionRange.Set(cursor, this.Navigator.SelectionRange.EndTime >= cursor ? this.Navigator.SelectionRange.EndTime : DateTime.MaxValue);
                        e.Handled = true;
                    }
                });

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownCommand
            => this.mouseRightButtonDownCommand ??= new RelayCommand<MouseButtonEventArgs>(
                e =>
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        var time = this.Navigator.Cursor;
                        this.Navigator.SelectionRange.Set(this.Navigator.SelectionRange.StartTime <= time ? this.Navigator.SelectionRange.StartTime : DateTime.MinValue, time);
                        e.Handled = true;
                    }
                });

        /// <summary>
        /// Gets the mouse move command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseEventArgs> MouseMoveCommand
            => this.mouseMoveCommand ??= new RelayCommand<MouseEventArgs>(
                e =>
                {
                    // Get the current mouse position
                    Point newMousePosition = e.GetPosition(this.viewport);

                    // Get the current scale factor between the axes logical bounds and the items control size.
                    double scaleY = (this.YAxis.Maximum - this.YAxis.Minimum) / this.viewport.ActualHeight;

                    // Set the mouse position in locical/image co-ordinates
                    this.MousePosition = new TimelinePanelMousePosition(this.GetTimeAtMousePointer(e, false), this.YAxis.Maximum - newMousePosition.Y * scaleY /*+ this.YAxis.Minimum*/);
                });

        /// <summary>
        /// Gets the set auto axis compute mode command for both the X and Y axes.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SetAutoAxisComputeModeCommand
            => this.setAutoAxisComputeModeCommand ??= new RelayCommand(() => this.AxisComputeMode = AxisComputeMode.Auto);

        /// <summary>
        /// Gets or sets the Y Axis for the panel.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public Axis YAxis
        {
            get => this.yAxis;
            set => this.Set(nameof(this.YAxis), ref this.yAxis, value);
        }

        /// <summary>
        /// Gets or sets the Y axis data displayed in the property browser.
        /// </summary>
        [IgnoreDataMember]
        [PropertyOrder(2)]
        [DisplayName("Y Axis")]
        [Description("The Y axis for the visualization panel.")]
        [ExpandableObject]
        public Axis YAxisPropertyBrowser
        {
            get => this.yAxisPropertyBrowser;

            set
            {
                if (this.yAxisPropertyBrowser.Range != value.Range)
                {
                    this.yAxisPropertyBrowser = value;
                    this.AxisComputeMode = AxisComputeMode.Manual;
                    this.YAxis.SetRange(value.Minimum, value.Maximum);
                }
            }
        }

        /// <summary>
        /// Gets or sets the axis compute mode.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Axis Compute Mode")]
        [Description("Specifies whether the axis is computed automatically or set manually.")]
        public AxisComputeMode AxisComputeMode
        {
            get { return this.axisComputeMode; }

            set
            {
                this.Set(nameof(this.AxisComputeMode), ref this.axisComputeMode, value);
                this.CalculateYAxisRange();
            }
        }

        /// <summary>
        /// Gets a string version of the mouse position.
        /// </summary>
        [IgnoreDataMember]
        [PropertyOrder(4)]
        [DisplayName("Mouse Position")]
        [Description("The position of the mouse within the Timeline Visualization panel.")]
        public string MousePositionString => $"{DateTimeHelper.FormatTime(this.mousePosition.X)}, {this.mousePosition.Y:F}";

        /// <summary>
        /// Gets or sets the mouse position in the panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public TimelinePanelMousePosition MousePosition
        {
            get { return this.mousePosition; }

            set
            {
                this.Set(nameof(this.MousePosition), ref this.mousePosition, value);
                this.RaisePropertyChanged(nameof(this.MousePositionString));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the legend should be shown.
        /// </summary>
        [DataMember]
        [PropertyOrder(5)]
        [DisplayName("Show Legend")]
        [Description("Show the legend for the visualization panel.")]
        public bool ShowLegend
        {
            get { return this.showLegend; }
            set { this.Set(nameof(this.ShowLegend), ref this.showLegend, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the time ticks should be shown.
        /// </summary>
        [DataMember]
        [PropertyOrder(6)]
        [DisplayName("Show Time Ticks")]
        [Description("Display an additional timeline along the bottom of the visualization panel.")]
        public bool ShowTimeTicks
        {
            get { return this.showTimeTicks; }
            set { this.Set(nameof(this.ShowTimeTicks), ref this.showTimeTicks, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the time ticks should be shown.
        /// </summary>
        [DataMember]
        [PropertyOrder(7)]
        [ExpandableObject]
        [DisplayName("Threshold Settings")]
        [Description("Settings related to how values that are above or below a threshold are rendered.")]
        public TimelineValueThreshold Threshold
        {
            get => this.threshold;
            set => this.Set(nameof(this.Threshold), ref this.threshold, value);
        }

        /// <summary>
        /// Gets the height of the low-opacity threshold rectangle.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public double ThresholdRectangleHeight
            => this.Threshold.ThresholdType switch
            {
                TimelineValueThreshold.TimelineThresholdType.Minimum => (this.YAxis.Maximum - this.Threshold.ThresholdValue) / (this.YAxis.Maximum - this.YAxis.Minimum) * this.Height,
                TimelineValueThreshold.TimelineThresholdType.Maximum => (this.Threshold.ThresholdValue - this.YAxis.Minimum) / (this.YAxis.Maximum - this.YAxis.Minimum) * this.Height,
                _ => 0.0d,
            };

        /// <summary>
        /// Gets the resulting opacity after applying the low-opacity threshold rectangle.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public double ThresholdRectangleOpacity => 1.0d - this.Threshold.Opacity;

        /// <summary>
        /// Gets the vertical alignment of the low-opacity threshold rectangle.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public VerticalAlignment ThresholdRectangleVerticalAlignment => this.Threshold.ThresholdType == TimelineValueThreshold.TimelineThresholdType.Maximum ? VerticalAlignment.Bottom : VerticalAlignment.Top;

        /// <summary>
        /// Gets the low-opacity threshold rectangle's visibility.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public Visibility ThresholdRectangleVisibility => this.Threshold.ThresholdType == TimelineValueThreshold.TimelineThresholdType.None ? Visibility.Collapsed : Visibility.Visible;

        /// <inheritdoc />
        public override bool ShowZoomToPanelMenuItem => true;

        /// <inheritdoc />
        public override bool CanZoomToPanel => this.VisualizationObjects.Count > 0;

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new () { VisualizationPanelType.Timeline };

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = new List<ContextMenuItemInfo>()
            {
                // The show/hide legend menu
                new ContextMenuItemInfo(IconSourcePath.Legend, this.ShowLegend ? $"Hide Legend" : $"Show Legend", this.ShowHideLegendCommand),
                new ContextMenuItemInfo(
                    null,
                    "Auto-Fit Axes",
                    this.SetAutoAxisComputeModeCommand,
                    isEnabled: this.AxisComputeMode == AxisComputeMode.Manual),
                null,
            };

            items.AddRange(base.ContextMenuItemsInfo());
            return items;
        }

        /// <summary>
        /// Gets the time at the mouse pointer, optionally adjusting for visualization object snap.
        /// </summary>
        /// <param name="mouseEventArgs">A mouse event args object.</param>
        /// <param name="useSnap">If true, and if a visualization object is currently being snapped to, then adjust the time to the nearest message in the visualization object being snapped to.</param>
        /// <returns>The time represented by the mouse pointer.</returns>
        public DateTime GetTimeAtMousePointer(MouseEventArgs mouseEventArgs, bool useSnap)
        {
            TimelineScroller root = this.GetTimelineScroller(mouseEventArgs.Source);
            if (root != null)
            {
                Point point = mouseEventArgs.GetPosition(root);
                double percent = point.X / root.ActualWidth;
                var viewRange = this.Navigator.ViewRange;
                DateTime time = viewRange.StartTime + TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));

                // If we're currently snapping to some Visualization Object, adjust the time to the timestamp of the nearest message
                DateTime? snappedTime = null;
                if (useSnap == true && VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject is IStreamVisualizationObject snapToVisualizationObject)
                {
                    snappedTime = DataManager.Instance.GetTimeOfNearestMessage(snapToVisualizationObject.StreamSource, time, NearestType.Nearest);
                }

                return snappedTime ?? time;
            }

            return DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the timeline scroller parent of a framework element.
        /// </summary>
        /// <param name="sourceElement">The framework element to search from.</param>
        /// <returns>The timeline scroller object.</returns>
        public TimelineScroller GetTimelineScroller(object sourceElement)
        {
            if (this.timelineScroller == null)
            {
                // Walk up the visual tree until we either find the
                // Timeline Scroller or fall off the top of the tree
                DependencyObject target = sourceElement as DependencyObject;
                while (target != null && target is not TimelineScroller)
                {
                    target = VisualTreeHelper.GetParent(target);
                }

                this.timelineScroller = target as TimelineScroller;
            }

            return this.timelineScroller;
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
            => XamlHelper.CreateTemplate(this.GetType(), typeof(TimelineVisualizationPanelView));

        /// <inheritdoc/>
        protected override void OnVisualizationObjectYValueRangeChanged(object sender, EventArgs e)
        {
            this.CalculateYAxisRange();
            base.OnVisualizationObjectYValueRangeChanged(sender, e);
        }

        /// <summary>
        /// Called when a property of the Y axis has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnYAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Axis.Range))
            {
                this.RaisePropertyChanged(nameof(this.YAxis));
                this.RaisePropertyChanged(nameof(this.ThresholdRectangleHeight));

                if (this.yAxis.Range != this.yAxisPropertyBrowser.Range)
                {
                    this.YAxisPropertyBrowser.SetRange(this.YAxis.Minimum, this.YAxis.Maximum);
                }
            }
        }

        private void OnViewportSizeChanged(SizeChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(this.ThresholdRectangleHeight));
        }

        private void YAxisPropertyBrowser_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Axis.Range) && this.yAxis.Range != this.yAxisPropertyBrowser.Range)
            {
                // Switch to manual axis compute mode
                this.AxisComputeMode = AxisComputeMode.Manual;

                // Set the y axis to match the values in the property browser
                this.YAxis.SetRange(this.YAxisPropertyBrowser.Minimum, this.YAxisPropertyBrowser.Maximum);
            }
        }

        private void CalculateYAxisRange()
        {
            // If the y axis is in auto mode, then recalculate the y axis range
            if (this.AxisComputeMode == AxisComputeMode.Auto)
            {
                // Get the Y value range of all visualization objects that are Y value range providers and have a non-null Y value range
                var yValueRanges = this.VisualizationObjects
                    .Where(vo => vo is IYValueRangeProvider)
                    .Select(vo => vo as IYValueRangeProvider)
                    .Select(vrp => vrp.YValueRange)
                    .Where(vr => vr != null);

                // Set the Y axis range to cover all of the axis ranges of the visualization objects.
                // If no visualization object reported a range, then use the default instead.
                if (yValueRanges.Any())
                {
                    double minimum = yValueRanges.Min(ar => ar.Minimum);
                    double maximum = yValueRanges.Max(ar => ar.Maximum);

                    // Slightly inflate the y axis range so that values right at the edges of the
                    // range are not rendered up against the top or bottom of the timeline panel view.
                    double padding = (maximum - minimum) * YAxisAutoComputeModePaddingPercent / 100.0d;
                    minimum -= padding;
                    maximum += padding;

                    this.YAxis.SetRange(minimum, maximum);
                }
                else
                {
                    this.YAxis.SetDefaultRange();
                }
            }
        }

        private void OnThresholdPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TimelineValueThreshold.ThresholdType):
                    this.RaisePropertyChanged(nameof(this.ThresholdRectangleHeight));
                    this.RaisePropertyChanged(nameof(this.ThresholdRectangleVerticalAlignment));
                    this.RaisePropertyChanged(nameof(this.ThresholdRectangleVisibility));
                    break;
                case nameof(TimelineValueThreshold.ThresholdValue):
                    this.RaisePropertyChanged(nameof(this.ThresholdRectangleHeight));
                    break;
                case nameof(TimelineValueThreshold.Opacity):
                    this.RaisePropertyChanged(nameof(this.ThresholdRectangleOpacity));
                    break;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Ensure axis range in property browser match actual axis range
            if (this.YAxisPropertyBrowser.Range != this.YAxis.Range)
            {
                this.YAxisPropertyBrowser.SetRange(this.YAxis.Range.Minimum, this.YAxis.Range.Maximum);
            }
        }
    }
}