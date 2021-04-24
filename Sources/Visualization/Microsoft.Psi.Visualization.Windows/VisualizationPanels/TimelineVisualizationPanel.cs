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
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization;
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
        private Axis yAxis = new Axis();
        private bool showLegend = false;
        private bool showTimeTicks = false;

        private RelayCommand clearSelectionCommand;
        private RelayCommand showHideLegendCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;

        private TimelineScroller timelineScroller = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineVisualizationPanel"/> class.
        /// </summary>
        public TimelineVisualizationPanel()
        {
            this.Name = "Timeline Panel";
            this.Height = 70;
            this.yAxis.PropertyChanged += this.OnYAxisPropertyChanged;
        }

        /// <summary>
        /// Gets the show/hide legend command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ShowHideLegendCommand
        {
            get
            {
                if (this.showHideLegendCommand == null)
                {
                    this.showHideLegendCommand = new RelayCommand(() => this.ShowLegend = !this.ShowLegend);
                }

                return this.showHideLegendCommand;
            }
        }

        /// <summary>
        /// Gets the clear selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ClearSelectionCommand
        {
            get
            {
                if (this.clearSelectionCommand == null)
                {
                    this.clearSelectionCommand = new RelayCommand(
                        () => this.Container.Navigator.ClearSelection(),
                        () => this.Container.Navigator.CanClearSelection());
                }

                return this.clearSelectionCommand;
            }
        }

        /// <summary>
        /// Gets the mouse left button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public override RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
        {
            get
            {
                if (this.mouseLeftButtonDownCommand == null)
                {
                    this.mouseLeftButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
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
                                var time = this.Navigator.Cursor;
                                this.Navigator.SelectionRange.SetRange(time, this.Navigator.SelectionRange.EndTime >= time ? this.Navigator.SelectionRange.EndTime : DateTime.MaxValue);
                                e.Handled = true;
                            }
                        });
                }

                return this.mouseLeftButtonDownCommand;
            }
        }

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownCommand
        {
            get
            {
                if (this.mouseRightButtonDownCommand == null)
                {
                    this.mouseRightButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
                        e =>
                        {
                            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                            {
                                var time = this.Navigator.Cursor;
                                this.Navigator.SelectionRange.SetRange(this.Navigator.SelectionRange.StartTime <= time ? this.Navigator.SelectionRange.StartTime : DateTime.MinValue, time);
                                e.Handled = true;
                            }
                        });
                }

                return this.mouseRightButtonDownCommand;
            }
        }

        /// <summary>
        /// Gets or sets the Y Axis for the panel.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("Y Axis")]
        [Description("The Y axis for the visualization panel.")]
        [ExpandableObject]
        public Axis YAxis
        {
            get { return this.yAxis; }
            set { this.Set(nameof(this.YAxis), ref this.yAxis, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the legend should be shown.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
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
        [PropertyOrder(4)]
        [DisplayName("Show Time Ticks")]
        [Description("Display an additional timeline along the bottom of the visualization panel.")]
        public bool ShowTimeTicks
        {
            get { return this.showTimeTicks; }
            set { this.Set(nameof(this.ShowTimeTicks), ref this.showTimeTicks, value); }
        }

        /// <inheritdoc />
        public override bool ShowZoomToPanelMenuItem => true;

        /// <inheritdoc />
        public override bool CanZoomToPanel => this.VisualizationObjects.Count > 0;

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new List<VisualizationPanelType>() { VisualizationPanelType.Timeline };

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
                    snappedTime = DataManager.Instance.GetTimeOfNearestMessage(snapToVisualizationObject.StreamSource, time, NearestMessageType.Nearest);
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
                while (target != null && !(target is TimelineScroller))
                {
                    target = VisualTreeHelper.GetParent(target);
                }

                this.timelineScroller = target as TimelineScroller;
            }

            return this.timelineScroller;
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(TimelineVisualizationPanelView));
        }

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
            if (e.PropertyName == nameof(Axis.AxisComputeMode))
            {
                this.CalculateYAxisRange();
            }
            else if (e.PropertyName == nameof(Axis.Range))
            {
                this.RaisePropertyChanged(nameof(this.YAxis));
            }
        }

        private void CalculateYAxisRange()
        {
            // If the y axis is in auto mode, then recalculate the y axis range
            if (this.YAxis.AxisComputeMode == AxisComputeMode.Auto)
            {
                // Get the Y value range of all visualization objects that are Y value range providers and have a non-null Y value range
                var yValueRanges = this.VisualizationObjects
                    .Where(vo => vo is IYValueRangeProvider)
                    .Select(vo => vo as IYValueRangeProvider)
                    .Select(vrp => vrp.YValueRange)
                    .Where(vr => vr != null);

                // Set the Y axis range to cover all of the axis ranges of the visualization objects.
                // If no visualization object reported a range, then use the defualt instead.
                if (yValueRanges.Any())
                {
                    this.YAxis.SetRange(yValueRanges.Min(ar => ar.Minimum), yValueRanges.Max(ar => ar.Maximum));
                }
                else
                {
                    this.YAxis.SetDefaultRange();
                }
            }
        }
    }
}