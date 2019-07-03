// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    /// <summary>
    /// An enumeration that defines the different modes for searching a stream summary.
    /// </summary>
    public enum StreamSummarySearchMode
    {
        /// <summary>
        /// Specifies a search mode that requires a match, otherwise no result is returned.
        /// </summary>
        Exact,

        /// <summary>
        /// Specifies a search mode that will return the previous value in the stream summary, if an exact match was not found.
        /// </summary>
        Previous,

        /// <summary>
        /// Specifies a search mode that will return the next value in the stream summary, if an exact match was not found.
        /// </summary>
        Next,
    }
}
