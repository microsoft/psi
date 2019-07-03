// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Provides data for the <see cref="Pipeline.PipelineExceptionNotHandled"/> event.
    /// </summary>
    public class PipelineExceptionNotHandledEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineExceptionNotHandledEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception thrown by the pipeline.</param>
        internal PipelineExceptionNotHandledEventArgs(Exception exception)
        {
            this.Exception = exception;
        }

        /// <summary>
        /// Gets the exception thrown by the pipeline.
        /// </summary>
        public Exception Exception { get; }
    }
}