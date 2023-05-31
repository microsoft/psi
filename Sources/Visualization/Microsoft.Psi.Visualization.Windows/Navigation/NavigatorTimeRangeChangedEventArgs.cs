// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    using System;

    /// <summary>
    /// Represents the method that will handle an event that has navigator changed event data.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An object that contains navigator changed event data.</param>
    public delegate void NavigatorTimeRangeChangedHandler(object sender, NavigatorTimeRangeChangedEventArgs e);

    /// <summary>Provides data for the <see cref="NavigatorRange" /> changed events.</summary>
    public class NavigatorTimeRangeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatorTimeRangeChangedEventArgs"/> class.
        /// </summary>
        /// <param name="originalStartTime">The original start time.</param>
        /// <param name="newStartTime">The new start time.</param>
        /// <param name="originalEndTime">The original end time.</param>
        /// <param name="newEndTime">The new end time.</param>
        public NavigatorTimeRangeChangedEventArgs(
            DateTime originalStartTime,
            DateTime newStartTime,
            DateTime originalEndTime,
            DateTime newEndTime)
        {
            this.OriginalStartTime = originalStartTime;
            this.NewStartTime = newStartTime;
            this.OriginalEndTime = originalEndTime;
            this.NewEndTime = newEndTime;
        }

        /// <summary>
        /// Gets the original start time.
        /// </summary>
        public DateTime OriginalStartTime { get; }

        /// <summary>
        /// Gets the new start time.
        /// </summary>
        public DateTime NewStartTime { get; }

        /// <summary>
        /// Gets the original end time.
        /// </summary>
        public DateTime OriginalEndTime { get; }

        /// <summary>
        /// Gets the new end time.
        /// </summary>
        public DateTime NewEndTime { get; }
    }
}