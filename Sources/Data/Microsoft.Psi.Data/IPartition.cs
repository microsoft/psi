// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines a partition that can be added to a session. A partition represents a single data stream in a data store.
    /// </summary>
    public interface IPartition
    {
        /// <summary>
        /// Gets or sets the partition name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in this partition.
        /// </summary>
        TimeInterval OriginatingTimeInterval { get; }

        /// <summary>
        /// Gets or sets the session that this partition belongs to.
        /// </summary>
        Session Session { get; set; }

        /// <summary>
        /// Gets the store name of this partition.
        /// </summary>
        string StoreName { get; }

        /// <summary>
        /// Gets the store path of this partition.
        /// </summary>
        string StorePath { get; }

        /// <summary>
        /// Gets an enumerable of stream metadata contained in this partition.
        /// </summary>
        IEnumerable<IStreamMetadata> AvailableStreams { get; }
    }
}