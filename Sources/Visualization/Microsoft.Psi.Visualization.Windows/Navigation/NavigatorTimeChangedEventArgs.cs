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
    public delegate void NavigatorTimeChangedHandler(object sender, NavigatorTimeChangedEventArgs e);

    /// <summary>Provides data for the <see cref="Navigator" /> changed events.</summary>
    public class NavigatorTimeChangedEventArgs : EventArgs
    {
        /// <summary>Initializes a new instance of the <see cref="NavigatorTimeChangedEventArgs" /> class.</summary>
        /// <param name="originalTime">The original value.</param>
        /// <param name="newTime">The new value.</param>
        public NavigatorTimeChangedEventArgs(DateTime originalTime, DateTime newTime)
        {
            this.OriginalTime = originalTime;
            this.NewTime = newTime;
        }

        /// <summary>
        /// Gets the original value.
        /// </summary>
        public DateTime OriginalTime { get; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public DateTime NewTime { get; }
    }
}