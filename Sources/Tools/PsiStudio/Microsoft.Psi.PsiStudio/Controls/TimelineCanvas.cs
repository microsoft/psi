// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Represents an element that positions child elements based on time.
    /// </summary>
    public class TimelineCanvas : Canvas
    {
        /// <summary>
        /// Identifies the Duration dependency property.
        /// </summary>
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.RegisterAttached(
                "Duration",
                typeof(TimeSpan),
                typeof(TimelineCanvas),
                new FrameworkPropertyMetadata(TimeSpan.MinValue));

        /// <summary>
        /// Identifies the EndTime dependency property.
        /// </summary>
        public static readonly DependencyProperty EndTimeProperty =
            DependencyProperty.RegisterAttached(
                "EndTime",
                typeof(DateTime),
                typeof(TimelineCanvas),
                new FrameworkPropertyMetadata(DateTime.MinValue));

        /// <summary>
        /// Identifies the StarTime dependency property.
        /// </summary>
        public static readonly DependencyProperty StartTimeProperty =
            DependencyProperty.RegisterAttached(
                "StartTime",
                typeof(DateTime),
                typeof(TimelineCanvas),
                new FrameworkPropertyMetadata(DateTime.MinValue));

        /// <summary>
        /// Identifies the ViewDuration dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewDurationProperty =
            DependencyProperty.Register(
                "ViewDuration",
                typeof(TimeSpan),
                typeof(TimelineCanvas),
                new FrameworkPropertyMetadata(TimeSpan.MinValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, OnViewRangeChanged));

        /// <summary>
        /// Identifies the ViewStartTime dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewStartTimeProperty =
            DependencyProperty.Register(
                "ViewStartTime",
                typeof(DateTime),
                typeof(TimelineCanvas),
                new FrameworkPropertyMetadata(DateTime.MinValue, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, OnViewRangeChanged));

        /// <summary>
        /// Gets or sets the minimum item width.
        /// </summary>
        public double MinItemWidth { get; set; }

        /// <summary>
        /// Gets or sets the view duration.
        /// </summary>
        public TimeSpan ViewDuration
        {
            get { return (TimeSpan)this.GetValue(TimelineCanvas.ViewDurationProperty); }
            set { this.SetValue(TimelineCanvas.ViewDurationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the view start time.
        /// </summary>
        public DateTime ViewStartTime
        {
            get { return (DateTime)this.GetValue(TimelineCanvas.ViewStartTimeProperty); }
            set { this.SetValue(TimelineCanvas.ViewStartTimeProperty, value); }
        }

        /// <summary>
        /// Gets the value of the Duration dependency property from the specified element.
        /// </summary>
        /// <param name="element">Element to get Duration property from.</param>
        /// <returns>The value of the Duration property.</returns>
        public static TimeSpan GetDuration(UIElement element)
        {
            TimeSpan duration = (TimeSpan)element.GetValue(DurationProperty);
            if (duration == TimeSpan.MinValue)
            {
                DateTime endTime = (DateTime)element.GetValue(EndTimeProperty);
                DateTime startTime = (DateTime)element.GetValue(StartTimeProperty);
                if (startTime != DateTime.MinValue && endTime != DateTime.MinValue)
                {
                    duration = endTime - startTime;
                }
                else
                {
                    duration = TimeSpan.FromSeconds(1);
                }
            }

            return duration;
        }

        /// <summary>
        /// Sets the value of the Duration dependency property on the specified element.
        /// </summary>
        /// <param name="element">Element to set Duration property on.</param>
        /// <param name="value">Value to set Duration property to.</param>
        public static void SetDuration(UIElement element, TimeSpan value)
        {
            element.SetValue(DurationProperty, value);
        }

        /// <summary>
        /// Gets the value of the EndTime dependency property from the specified element.
        /// </summary>
        /// <param name="element">Element to get EndTime property from.</param>
        /// <returns>The value of the Duration property.</returns>
        public static DateTime GetEndTime(UIElement element)
        {
            DateTime endTime = (DateTime)element.GetValue(EndTimeProperty);
            if (endTime == DateTime.MinValue)
            {
                DateTime startTime = (DateTime)element.GetValue(StartTimeProperty);
                endTime = startTime + GetDuration(element);
            }

            return endTime;
        }

        /// <summary>
        /// Sets the value of the EndTime dependency property on the specified element.
        /// </summary>
        /// <param name="element">Element to set EndTime property on.</param>
        /// <param name="value">Value to set EndTime property to.</param>
        public static void SetEndTime(UIElement element, DateTime value)
        {
            element.SetValue(EndTimeProperty, value);
        }

        /// <summary>
        /// Gets the value of the StartTime dependency property from the specified element.
        /// </summary>
        /// <param name="element">Element to get StartTime property from.</param>
        /// <returns>The value of the StartTime property.</returns>
        public static DateTime GetStartTime(UIElement element)
        {
            DateTime startTime = (DateTime)element.GetValue(StartTimeProperty);
            if (startTime == DateTime.MinValue)
            {
                DateTime endTime = (DateTime)element.GetValue(EndTimeProperty);
                if (endTime != DateTime.MinValue)
                {
                    startTime = endTime - GetDuration(element);
                }
            }

            return startTime;
        }

        /// <summary>
        /// Sets the value of the StartTime dependency property on the specified element.
        /// </summary>
        /// <param name="element">Element to set StartTime property on.</param>
        /// <param name="value">Value to set StartTime property to.</param>
        public static void SetStartTime(UIElement element, DateTime value)
        {
            element.SetValue(StartTimeProperty, value);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            double scale = arrangeBounds.Width / this.ViewDuration.Ticks;
            double offset = this.ViewStartTime.Ticks;
            UIElementCollection children = this.InternalChildren;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                long duration = TimelineCanvas.GetDuration(child).Ticks;
                long start = TimelineCanvas.GetStartTime(child).Ticks;
                double width = Math.Max(duration * scale, this.MinItemWidth);
                double left = (start - offset) * scale;
                child.Arrange(new Rect(left, 0, width, arrangeBounds.Height));
            }

            return arrangeBounds;
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            double scale = constraint.Width / this.ViewDuration.Ticks;
            double offset = this.ViewStartTime.Ticks;
            UIElementCollection children = this.InternalChildren;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                long duration = TimelineCanvas.GetDuration(child).Ticks;
                long start = TimelineCanvas.GetStartTime(child).Ticks;
                double width = Math.Max(duration * scale, this.MinItemWidth);
                double left = (start - offset) * scale;
                child.Measure(new Size(width, constraint.Height));
            }

            return constraint;
        }

        private static void OnViewRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as TimelineCanvas).UpdateViewRange();
        }

        private void UpdateViewRange()
        {
            this.InvalidateArrange();
        }
    }
}
