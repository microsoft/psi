// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents summarizers that perform interval-based data summarization over a series of data values.
    /// </summary>
    public interface ISummarizer
    {
        /// <summary>
        /// Gets the destination data type.
        /// </summary>
        Type DestinationType { get; }

        /// <summary>
        /// Gets the source data type.
        /// </summary>
        Type SourceType { get; }
    }
}
