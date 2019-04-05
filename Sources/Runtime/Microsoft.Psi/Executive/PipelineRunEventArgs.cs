// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Class encapsulating the event arguments provided by the <see cref="Pipeline.PipelineRun"/> event.
    /// </summary>
    public class PipelineRunEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineRunEventArgs"/> class.
        /// </summary>
        /// <param name="runDateTime">The time the pipeline started running.</param>
        internal PipelineRunEventArgs(DateTime runDateTime)
        {
            this.RunDateTime = runDateTime;
        }

        /// <summary>
        /// Gets the time when the pipeline started running.
        /// </summary>
        public DateTime RunDateTime { get; private set; }
    }
}