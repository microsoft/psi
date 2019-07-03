// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Microsoft.Psi.Visualization.Controls;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Interaction logic for TimelineView.xaml.
    /// </summary>
    public partial class TimelineView : UserControl
    {
        /// <summary>
        /// Identifies the Mode dependency property.
        /// </summary>
        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(
                "Mode",
                typeof(TimelineViewMode),
                typeof(TimelineView),
                new PropertyMetadata(TimelineViewMode.ViewRange, OnModeChanged));

        /// <summary>
        /// Identifies the ShowTicks dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTicksProperty =
            DependencyProperty.Register(
                "ShowTicks",
                typeof(bool),
                typeof(TimelineView),
                new PropertyMetadata(true, OnShowTicksChanges));

        private static List<TickZoomLevelDescriptor> tickZoomLevelDescriptors = new List<TickZoomLevelDescriptor>()
        {
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.Microsecond, DurationInTicks = TimeSpan.FromMilliseconds(1).Ticks / 1000, StringFormat = "ss.ffffff" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.TenMicroseconds, DurationInTicks = TimeSpan.FromMilliseconds(1).Ticks / 100, StringFormat = "ss.ffffff" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.HundredMicroseconds, DurationInTicks = TimeSpan.FromMilliseconds(1).Ticks / 10, StringFormat = "ss.ffffff" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.Millisecond, DurationInTicks = TimeSpan.FromMilliseconds(1).Ticks, StringFormat = "mm:ss.fff" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.TenMilliseconds, DurationInTicks = TimeSpan.FromMilliseconds(1).Ticks * 10, StringFormat = "mm:ss.fff" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.HundredMilliseconds, DurationInTicks = TimeSpan.FromMilliseconds(1).Ticks * 100, StringFormat = "mm:ss.fff" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.Second, DurationInTicks = TimeSpan.FromSeconds(1).Ticks, StringFormat = "hh:mm:ss" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.TenSeconds, DurationInTicks = TimeSpan.FromSeconds(1).Ticks * 10, StringFormat = "hh:mm:ss" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.Minute, DurationInTicks = TimeSpan.FromMinutes(1).Ticks, StringFormat = "hh:mm" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.TenMinutes, DurationInTicks = TimeSpan.FromMinutes(1).Ticks * 10, StringFormat = "hh:mm" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.Hour, DurationInTicks = TimeSpan.FromHours(1).Ticks, StringFormat = "mm-dd.hh" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.SixHours, DurationInTicks = TimeSpan.FromHours(1).Ticks * 6, StringFormat = "mm-dd.hh" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.Day, DurationInTicks = TimeSpan.FromDays(1).Ticks, StringFormat = "yyyy-mm-dd" } },
            { new TickZoomLevelDescriptor { TickZoomLevel = TickZoomLevel.Week, DurationInTicks = TimeSpan.FromDays(1).Ticks * 7, StringFormat = "yyyy-mm-dd" } },
        };

        private TickZoomLevel tickZoomLevelMajor = TickZoomLevel.None;
        private Dictionary<Tuple<DateTime, TimeSpan>, TimelineSegmentView> segments = new Dictionary<Tuple<DateTime, TimeSpan>, TimelineSegmentView>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineView"/> class.
        /// </summary>
        public TimelineView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.NavigatorView_DataContextChanged;
        }

        /// <summary>
        /// Represents time zoom levels.
        /// </summary>
        public enum TickZoomLevel
        {
            /// <summary>
            /// No time zoom level.
            /// </summary>
            None,

            /// <summary>
            /// Microsecond time zoom level.
            /// </summary>
            Microsecond,

            /// <summary>
            /// Ten microsecond time zoom level.
            /// </summary>
            TenMicroseconds,

            /// <summary>
            /// Hundred microsecond time zoom level.
            /// </summary>
            HundredMicroseconds,

            /// <summary>
            /// Millisecond time zoom level.
            /// </summary>
            Millisecond,

            /// <summary>
            /// Ten millisecond time zoom level.
            /// </summary>
            TenMilliseconds,

            /// <summary>
            /// Hundred millisecond time zoom level.
            /// </summary>
            HundredMilliseconds,

            /// <summary>
            /// Second time zoom level.
            /// </summary>
            Second,

            /// <summary>
            /// Ten second time zoom level.
            /// </summary>
            TenSeconds,

            /// <summary>
            /// Minute time zoom level.
            /// </summary>
            Minute,

            /// <summary>
            /// Ten minute time zoom level.
            /// </summary>
            TenMinutes,

            /// <summary>
            /// Hour time zoom level.
            /// </summary>
            Hour,

            /// <summary>
            /// Six hours time zoom level.
            /// </summary>
            SixHours,

            /// <summary>
            /// Day time zoom level.
            /// </summary>
            Day,

            /// <summary>
            /// Week time zoom level.
            /// </summary>
            Week,
        }

        /// <summary>
        /// Gets or sets the timeline view mode.
        /// </summary>
        public TimelineViewMode Mode
        {
            get { return (TimelineViewMode)this.GetValue(ModeProperty); }
            set { this.SetValue(ModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the navigator.
        /// </summary>
        public Navigator Navigator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show ticks.
        /// </summary>
        public bool ShowTicks
        {
            get { return (bool)this.GetValue(ShowTicksProperty); }
            set { this.SetValue(ShowTicksProperty, value); }
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            this.UpdateCursorMarker();
            this.UpdateSelectionMarkers();
            this.ComputeTicks();
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TimelineView).OnModeChanged();
        }

        private static void OnShowTicksChanges(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TimelineView timeLineView = (TimelineView)d;
            timeLineView.timelineCanvas.Visibility = ((bool)e.NewValue) == true ? Visibility.Visible : Visibility.Hidden;
        }

        private void Clear()
        {
            this.segments.Clear();
            this.timelineCanvas.Children.Clear();
        }

        private void ComputeTicks()
        {
            if (this.Navigator == null)
            {
                return;
            }

            var range = this.Mode == TimelineViewMode.ViewRange ? this.Navigator.ViewRange : this.Navigator.DataRange;
            var viewDurationInTicks = range.Duration.Ticks;

            TickZoomLevelDescriptor tickZoomLevelDescriptorMajor = tickZoomLevelDescriptors[0];
            TickZoomLevel tickZoomLevelMajorLast = this.tickZoomLevelMajor;
            foreach (var tickZoomLevelDescriptor in tickZoomLevelDescriptors)
            {
                tickZoomLevelDescriptorMajor = tickZoomLevelDescriptor;
                var countSegments = viewDurationInTicks / tickZoomLevelDescriptor.DurationInTicks;
                var segmentWidth = this.ActualWidth / countSegments;
                if (segmentWidth > 50)
                {
                    break;
                }
            }

            this.tickZoomLevelMajor = tickZoomLevelDescriptorMajor.TickZoomLevel;

            // we've changed zoom levels so reset segments
            if (this.tickZoomLevelMajor != tickZoomLevelMajorLast)
            {
                this.Clear();
            }

            long segmentStart = (range.StartTime.Ticks - this.Navigator.DataRange.StartTime.Ticks) / tickZoomLevelDescriptorMajor.DurationInTicks;
            long segmentEnd = (range.EndTime.Ticks - this.Navigator.DataRange.StartTime.Ticks) / tickZoomLevelDescriptorMajor.DurationInTicks;

            // remove all unnessary segments (due to scrolling out of view)
            //   segment start + duration < visible start time... no part of segment is visible
            //   or segment start > visible range
            var segmentsToRemove = this.segments.Keys.Where(key => key.Item1 + key.Item2 < range.StartTime || key.Item1 > range.EndTime).ToList();
            foreach (var segmentToRemove in segmentsToRemove)
            {
                this.timelineCanvas.Children.Remove(this.segments[segmentToRemove]);
                this.segments.Remove(segmentToRemove);
            }

            for (long segment = segmentStart; segment <= segmentEnd; segment++)
            {
                var startTime = new DateTime(this.Navigator.DataRange.StartTime.Ticks + (segment * tickZoomLevelDescriptorMajor.DurationInTicks));
                var duration = new TimeSpan(tickZoomLevelDescriptorMajor.DurationInTicks);
                var key = new Tuple<DateTime, TimeSpan>(startTime, duration);
                if (!this.segments.ContainsKey(key))
                {
                    var ellapsedTime = new TimeSpan(segment * tickZoomLevelDescriptorMajor.DurationInTicks);
                    var label = ellapsedTime.ToString();
                    var segmentView = new TimelineSegmentView(this.Mode == TimelineViewMode.DataRange ? VerticalAlignment.Top : VerticalAlignment.Bottom, 10, label);
                    TimelineCanvas.SetStartTime(segmentView, startTime);
                    TimelineCanvas.SetDuration(segmentView, duration);
                    this.segments.Add(key, segmentView);
                    this.timelineCanvas.Children.Add(segmentView);
                }
            }
        }

        private void DataRange_RangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.UpdateCursorMarker();
            this.UpdateSelectionMarkers();
            if (e.NewStartTime != e.OriginalStartTime)
            {
                // our times are all expressed in ellapsed timne from DataRange.Start so we need to reset any timeline when this changes
                this.Clear();
                this.ComputeTicks();
            }
            else if (this.Mode == TimelineViewMode.DataRange)
            {
                this.ComputeTicks();
            }
        }

        private void Navigator_CursorChanged(object sender, NavigatorTimeChangedEventArgs e)
        {
            this.UpdateCursorMarker();
        }

        private void NavigatorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Navigator != null)
            {
                this.Navigator.CursorChanged -= this.Navigator_CursorChanged;
                ((NavigatorRange)this.Navigator.DataRange).RangeChanged -= this.DataRange_RangeChanged;
                ((NavigatorRange)this.Navigator.SelectionRange).RangeChanged -= this.SelectionRange_RangeChanged;
                ((NavigatorRange)this.Navigator.ViewRange).RangeChanged -= this.ViewRange_RangeChanged;
            }

            this.Navigator = e.NewValue as Navigator;
            if (this.Navigator != null)
            {
                this.Navigator.CursorChanged += this.Navigator_CursorChanged;
                ((NavigatorRange)this.Navigator.DataRange).RangeChanged += this.DataRange_RangeChanged;
                ((NavigatorRange)this.Navigator.SelectionRange).RangeChanged += this.SelectionRange_RangeChanged;
                ((NavigatorRange)this.Navigator.ViewRange).RangeChanged += this.ViewRange_RangeChanged;
            }
        }

        private void OnModeChanged()
        {
            this.timelineCanvas.SetBinding(TimelineCanvas.ViewStartTimeProperty, new Binding(this.Mode == TimelineViewMode.DataRange ? "DataRange.StartTime" : "ViewRange.StartTime"));
            this.timelineCanvas.SetBinding(TimelineCanvas.ViewDurationProperty, new Binding(this.Mode == TimelineViewMode.DataRange ? "DataRange.Duration" : "ViewRange.Duration"));
        }

        private void SelectionRange_RangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.UpdateSelectionMarkers();
        }

        private void UpdateCursorMarker()
        {
            if (this.Navigator == null)
            {
                return;
            }

            var range = this.Mode == TimelineViewMode.ViewRange ? this.Navigator.ViewRange : this.Navigator.DataRange;
            DateTime cursor = this.Navigator.Cursor;
            double percent = (double)(cursor - range.StartTime).Ticks / (double)range.Duration.Ticks;
            this.CursorMarker.X1 = this.CursorMarker.X2 = this.ActualWidth * percent;
        }

        private void UpdateSelectionMarkers()
        {
            if (this.Navigator == null)
            {
                return;
            }

            var range = this.Mode == TimelineViewMode.ViewRange ? this.Navigator.ViewRange : this.Navigator.DataRange;

            DateTime selectionStart = this.Navigator.SelectionRange.StartTime;
            double selectionStartPercent = (double)(selectionStart - range.StartTime).Ticks / (double)range.Duration.Ticks;
            this.SelectionStartMarker.X1 = this.SelectionStartMarker.X2 = this.Root.ActualWidth * selectionStartPercent;

            DateTime selectionEnd = this.Navigator.SelectionRange.EndTime;
            double selectionEndPercent = (double)(selectionEnd - range.StartTime).Ticks / (double)range.Duration.Ticks;
            this.SelectionEndMarker.X1 = this.SelectionEndMarker.X2 = this.Root.ActualWidth * selectionEndPercent;
        }

        private void ViewRange_RangeChanged(object sender, NavigatorTimeRangeChangedEventArgs e)
        {
            this.UpdateCursorMarker();
            this.UpdateSelectionMarkers();
            if (this.Mode == TimelineViewMode.ViewRange)
            {
                this.ComputeTicks();
            }
        }

        private struct TickZoomLevelDescriptor
        {
            public TickZoomLevel TickZoomLevel;
            public long DurationInTicks;
            public string StringFormat;
        }
    }
}