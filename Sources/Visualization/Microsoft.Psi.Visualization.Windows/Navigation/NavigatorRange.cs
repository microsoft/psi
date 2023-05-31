// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents a time range used by the navigator.
    /// </summary>
    public class NavigatorRange : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatorRange"/> class.
        /// </summary>
        public NavigatorRange()
            : this(DateTime.MinValue, DateTime.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatorRange"/> class.
        /// </summary>
        /// <param name="startTime">The range's start time.</param>
        /// <param name="endTime">The range's end time.</param>
        public NavigatorRange(DateTime startTime, DateTime endTime)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        /// <summary>
        /// Occurs when the navigator range has changed.
        /// </summary>
        public event NavigatorTimeRangeChangedHandler RangeChanged;

        /// <summary>
        /// Gets the range duration.
        /// </summary>
        public TimeSpan Duration => this.EndTime - this.StartTime;

        /// <summary>
        /// Gets the range end time.
        /// </summary>
        [DataMember]
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the navigator range is finite.
        /// </summary>
        public bool IsFinite => (this.StartTime != DateTime.MinValue) && (this.EndTime != DateTime.MaxValue);

        /// <summary>
        /// Gets the range start time.
        /// </summary>
        [DataMember]
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets the navigator range as a time interval.
        /// </summary>
        public TimeInterval AsTimeInterval => new (this.StartTime, this.EndTime);

        /// <summary>
        /// Scrolls the start and end times of the Range.
        /// </summary>
        /// <param name="timespan">The amount of time to scroll the range.</param>
        public void ScrollBy(TimeSpan timespan)
        {
            this.Set(this.StartTime + timespan, this.EndTime + timespan);
        }

        /// <summary>
        /// Sets the navigator range based on a specified start and end time.
        /// </summary>
        /// <param name="startTime">Start time of the range.</param>
        /// <param name="endTime">End time of the range.</param>
        public void Set(DateTime startTime, DateTime endTime)
        {
            // Clamp the parameters if start >= endTime. Choose to set endTime to startTime + 1 tick.
            if (startTime >= endTime)
            {
                endTime = startTime + TimeSpan.FromTicks(1);
            }

            var originalStartTime = this.StartTime;
            var originalEndTime = this.EndTime;

            if (startTime != originalStartTime)
            {
                this.RaisePropertyChanging(nameof(this.StartTime));
            }

            if (endTime != originalEndTime)
            {
                this.RaisePropertyChanging(nameof(this.EndTime));
            }

            if (this.Duration != endTime - startTime)
            {
                this.RaisePropertyChanging(nameof(this.Duration));
            }

            this.StartTime = startTime;
            this.EndTime = endTime;

            this.RangeChanged?.Invoke(this, new NavigatorTimeRangeChangedEventArgs(originalStartTime, this.StartTime, originalEndTime, this.EndTime));

            if (startTime != originalStartTime)
            {
                this.RaisePropertyChanged(nameof(this.StartTime));
            }

            if (endTime != originalEndTime)
            {
                this.RaisePropertyChanged(nameof(this.EndTime));
            }

            if (this.Duration != originalEndTime - originalStartTime)
            {
                this.RaisePropertyChanged(nameof(this.Duration));
            }
        }

        /// <summary>
        /// Sets the navigator range based on a specified start time and duration.
        /// </summary>
        /// <param name="startTime">Start time of the range.</param>
        /// <param name="duration">Duration of the range.</param>
        public void Set(DateTime startTime, TimeSpan duration)
        {
            this.Set(startTime, startTime + duration);
        }

        /// <summary>
        /// Sets the navigator range to a specified time interval.
        /// </summary>
        /// <param name="timeInterval">Time interval to set the range with.</param>
        public void Set(TimeInterval timeInterval)
        {
            this.Set(timeInterval.Left, timeInterval.Right);
        }
    }
}
