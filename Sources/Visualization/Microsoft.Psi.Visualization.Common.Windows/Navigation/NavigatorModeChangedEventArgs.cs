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
    public delegate void NavigatorModeChangedHandler(object sender, NavigatorModeChangedEventArgs e);

    /// <summary>
    /// Represents event data for when the navigator mode change event is raised.
    /// </summary>
    public class NavigatorModeChangedEventArgs : EventArgs
    {
        /// <summary>Initializes a new instance of the <see cref="NavigatorModeChangedEventArgs" /> class.</summary>
        /// <param name="originalValue">The original value.</param>
        /// <param name="newValue">The new value.</param>
        public NavigatorModeChangedEventArgs(NavigationMode originalValue, NavigationMode newValue)
        {
            this.OriginalValue = originalValue;
            this.NewValue = newValue;
        }

        /// <summary>
        /// Gets the original value.
        /// </summary>
        public NavigationMode OriginalValue { get; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public NavigationMode NewValue { get; }
    }
}