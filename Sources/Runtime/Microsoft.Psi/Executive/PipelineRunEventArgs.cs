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
        /// <param name="startOriginatingTime">The time the pipeline started running.</param>
        internal PipelineRunEventArgs(DateTime startOriginatingTime)
        {
            this.StartOriginatingTime = startOriginatingTime;
        }

        /// <summary>
        /// Gets the time when the pipeline started running.
        /// </summary>
        public DateTime StartOriginatingTime { get; private set; }
    }
}