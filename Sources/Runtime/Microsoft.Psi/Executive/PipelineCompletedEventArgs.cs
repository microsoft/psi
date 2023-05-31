// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides data for the <see cref="Pipeline.PipelineCompleted"/> event.
    /// </summary>
    public class PipelineCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="completedOriginatingTime">The time the pipeline completed.</param>
        /// <param name="abandonedPendingWorkitems">True if workitems were abandoned, false otherwise.</param>
        /// <param name="errors">The set of errors that caused the pipeline to stop, if any.</param>
        internal PipelineCompletedEventArgs(DateTime completedOriginatingTime, bool abandonedPendingWorkitems, List<Exception> errors)
        {
            this.CompletedOriginatingTime = completedOriginatingTime;
            this.AbandonedPendingWorkitems = abandonedPendingWorkitems;
            this.Errors = errors.AsReadOnly();
        }

        /// <summary>
        /// Gets the time when the pipeline completed.
        /// </summary>
        public DateTime CompletedOriginatingTime { get; private set; }

        /// <summary>
        /// Gets a value indicating whether any workitems have been abandoned.
        /// </summary>
        public bool AbandonedPendingWorkitems { get; private set; }

        /// <summary>
        /// Gets the set of errors that caused the pipeline to stop, if any.
        /// </summary>
        public IReadOnlyList<Exception> Errors { get; private set; }
    }
}