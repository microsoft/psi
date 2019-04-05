// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Navigation
{
    using System;

    /// <summary>
    /// Represents the method that will handle an event that has cursor mode changed event data.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An object that contains cursot mode changed event data.</param>
    public delegate void CursorModeChangedHandler(object sender, CursorModeChangedEventArgs e);

    /// <summary>
    /// Represents event data for when the cursor mode change event is raised.
    /// </summary>
    public class CursorModeChangedEventArgs : EventArgs
    {
        /// <summary>Initializes a new instance of the <see cref="CursorModeChangedEventArgs" /> class.</summary>
        /// <param name="originalValue">The original value.</param>
        /// <param name="newValue">The new value.</param>
        public CursorModeChangedEventArgs(CursorMode originalValue, CursorMode newValue)
        {
            this.OriginalValue = originalValue;
            this.NewValue = newValue;
        }

        /// <summary>
        /// Gets the original value.
        /// </summary>
        public CursorMode OriginalValue { get; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public CursorMode NewValue { get; }
    }
}