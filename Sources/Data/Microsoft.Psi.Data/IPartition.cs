// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines a partition that can be added to a session.
    /// </summary>
    public interface IPartition
    {
        /// <summary>
        /// Gets or sets the partition name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets a value indicating whether the the store for the partition is valid and readable.
        /// </summary>
        bool IsStoreValid { get; }

        /// <summary>
        /// Gets the originating time interval (earliest to latest) of the messages in this partition.
        /// </summary>
        TimeInterval MessageOriginatingTimeInterval { get; }

        /// <summary>
        /// Gets the creation time interval (earliest to latest) of the messages in this partition.
        /// </summary>
        TimeInterval MessageCreationTimeInterval { get; }

        /// <summary>
        /// Gets the time interval between open and closed time for all streams in this partition.
        /// </summary>
        TimeInterval TimeInterval { get; }

        /// <summary>
        /// Gets the size of the partition in bytes, if known.
        /// </summary>
        long? Size { get; }

        /// <summary>
        /// Gets the number of streams in the partition, if known.
        /// </summary>
        int? StreamCount { get; }

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
        /// Gets the type name of the IStreamReader for this partition.
        /// </summary>
        string StreamReaderTypeName { get; }

        /// <summary>
        /// Gets an enumerable of stream metadata contained in this partition.
        /// </summary>
        IEnumerable<IStreamMetadata> AvailableStreams { get; }
    }
}