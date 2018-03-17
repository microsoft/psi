// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Server;

    /// <summary>
    /// Represents a time range used by the navigator.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteNavigatorRangeCLSIDString)]
    [ComVisible(false)]
    public class NavigatorRange : ReferenceCountedObject, IRemoteNavigatorRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatorRange"/> class.
        /// </summary>
        public NavigatorRange()
        {
            this.StartTime = DateTime.MinValue;
            this.EndTime = DateTime.MaxValue;
        }

        /// <summary>
        /// Occurs when the navigator range has changed.
        /// </summary>
        public event NavigatorTimeRangeChangedHandler RangeChanged;

        /// <inheritdoc />
        public TimeSpan Duration => this.EndTime - this.StartTime;

        /// <inheritdoc />
        [DataMember]
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the navigator range is finite.
        /// </summary>
        public bool IsFinite => (this.StartTime != DateTime.MinValue) && (this.EndTime != DateTime.MinValue);

        /// <inheritdoc />
        [DataMember]
        public DateTime StartTime { get; private set; }

        /// <inheritdoc />
        public void SetRange(DateTime startTime, DateTime endTime)
        {
            // Clamp the parameters if start >= endTime. Choose to set endTime to startTime + 1 tick.
            if (startTime >= endTime)
            {
                endTime = startTime + TimeSpan.FromTicks(1);
            }

            var originalStartTime = this.StartTime;
            var originalEndTime = this.EndTime;
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
        /// Sets the range.
        /// </summary>
        /// <param name="startTime">Start time of the range.</param>
        /// <param name="duration">Duration of the range.</param>
        public void SetRange(DateTime startTime, TimeSpan duration)
        {
            this.SetRange(startTime, startTime + duration);
        }

        /// <summary>
        /// Sets the range.
        /// </summary>
        /// <param name="timeInterval">Time interval to set the range with.</param>
        public void SetRange(TimeInterval timeInterval)
        {
            this.SetRange(timeInterval.Left, timeInterval.Right);
        }
    }
}
