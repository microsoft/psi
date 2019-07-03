// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents a message reader from a multi-stream store.
    /// </summary>
    public interface ISimpleReader : IDisposable
    {
        /// <summary>
        /// Gets an enumerable of stream metadata contained in the underlying data store.
        /// </summary>
        IEnumerable<IStreamMetadata> AvailableStreams { get; }

        /// <summary>
        /// Gets the name of the application that generated the persisted files, or the root name of the files.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the directory in which the main persisted file resides.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Opens the specified store.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="serializers">Optional set of serialization configuration (known types, serializers, known assemblies).</param>
        void OpenStore(string name, string path, KnownSerializers serializers = null);

        /// <summary>
        /// Creates a new reader for the same store, without reloading the index and metadata files.
        /// </summary>
        /// <returns>A new reader for the same store.</returns>
        ISimpleReader OpenNew();

        /// <summary>
        /// Opens the specified logical storage stream for reading.
        /// </summary>
        /// <typeparam name="T">The type of messages in stream.</typeparam>
        /// <param name="streamName">The name of the storage stream to open.</param>
        /// <param name="target">The function to call for every message in this storage stream.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <returns>The metadata describing the opened storage stream.</returns>
        IStreamMetadata OpenStream<T>(string streamName, Action<T, Envelope> target, Func<T> allocator = null);

        /// <summary>
        /// Opens the specified logical storage stream for reading, in index form.
        /// That is, only index entries are provided to the target delegate.
        /// </summary>
        /// <typeparam name="T">The type of messages in stream.</typeparam>
        /// <param name="streamName">The name of the storage stream to open.</param>
        /// <param name="target">The function to call with the index of every message in this storage stream.</param>
        /// <returns>The metadata describing the opened storage stream.</returns>
        IStreamMetadata OpenStreamIndex<T>(string streamName, Action<IndexEntry, Envelope> target);

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this store, across all logical streams.
        /// </summary>
        /// <returns>The originating time interval.</returns>
        TimeInterval OriginatingTimeRange();

        /// <summary>
        /// Reads the message at the specified position.
        /// </summary>
        /// <typeparam name="T">The type of message to read.</typeparam>
        /// <param name="indexEntry">The position to read from.</param>
        /// <returns>The message read from the store.</returns>
        T Read<T>(IndexEntry indexEntry);

        /// <summary>
        /// Reads the message at the specified position.
        /// </summary>
        /// <typeparam name="T">The type of message to read.</typeparam>
        /// <param name="indexEntry">The position to read from.</param>
        /// <param name="objectToReuse">An unused object that can be used as a buffer to read into.</param>
        void Read<T>(IndexEntry indexEntry, ref T objectToReuse);

        /// <summary>
        /// Reads all the messages within the time interval specified by the replay descriptor and calls the registered delegates.
        /// </summary>
        /// <param name="descriptor">The replay descriptor providing the interval to read.</param>
        /// <param name="cancelationToken">A token that can be used to cancel the operation.</param>
        void ReadAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default(CancellationToken));
    }
}