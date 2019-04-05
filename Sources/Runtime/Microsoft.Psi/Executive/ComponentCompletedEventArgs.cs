// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Class encapsulating the event arguments provided by the <see cref="Pipeline.ComponentCompleted"/> event.
    /// </summary>
    public class ComponentCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="componentName">The name of the component.</param>
        /// <param name="completedDateTime">The time the component completed.</param>
        internal ComponentCompletedEventArgs(string componentName, DateTime completedDateTime)
        {
            this.ComponentName = componentName;
            this.CompletedDateTime = completedDateTime;
        }

        /// <summary>
        /// Gets the name of the component which completed.
        /// </summary>
        public string ComponentName { get; private set; }

        /// <summary>
        /// Gets the time when the component completed.
        /// </summary>
        public DateTime CompletedDateTime { get; private set; }
    }
}