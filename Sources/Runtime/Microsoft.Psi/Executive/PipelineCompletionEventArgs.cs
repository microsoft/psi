// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Class encapsulating the event arguments provided by the <see cref="Pipeline.PipelineCompletionEvent"/>.
    /// </summary>
    public class PipelineCompletionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineCompletionEventArgs"/> class.
        /// </summary>
        /// <param name="completionDateTime">The time of completion.</param>
        /// <param name="abandonedPendingWorkitems">True if workitems were abandoned, false otherwise.</param>
        /// <param name="errors">The set of errors that caused the pipeline to stop, if any</param>
        internal PipelineCompletionEventArgs(DateTime completionDateTime, bool abandonedPendingWorkitems, List<Exception> errors)
        {
            this.CompletionDateTime = completionDateTime;
            this.AbandonedPendingWorkitems = abandonedPendingWorkitems;
            this.Errors = errors.AsReadOnly();
        }

        /// <summary>
        /// Gets the time when the pipeline stopped.
        /// </summary>
        public DateTime CompletionDateTime { get; private set; }

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