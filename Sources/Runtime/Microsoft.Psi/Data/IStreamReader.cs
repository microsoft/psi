// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Threading;

    /// <summary>
    /// Represents a reader of multiple streams of typed messages.
    /// </summary>
    /// <remarks>
    /// This interface provides the basis for enabling \psi tools and APIs to operate with different kinds of stream stores.
    /// In addition to this interface, a proper stream reader must provide a constructor accepting the Name and Path.
    /// For integration with Platform for Situated Intelligence Studio, implementations should include a class-level StreamReaderAttribute.
    /// </remarks>
    public interface IStreamReader : IDisposable
    {
        /// <summary>
        /// Gets the name of the application that generated the persisted files, or the root name of the files.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the directory in which the main persisted file resides.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the set of streams in this store.
        /// </summary>
        IEnumerable<IStreamMetadata> AvailableStreams { get; }

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this store, across all streams.
        /// </summary>
        TimeInterval MessageCreationTimeInterval { get; }

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this store, across all streams.
        /// </summary>
        TimeInterval MessageOriginatingTimeInterval { get; }

        /// <summary>
        /// Gets the interval between the opened and closed times, across all streams.
        /// </summary>
        TimeInterval StreamTimeInterval { get; }

        /// <summary>
        /// Gets the size of data accessed by the stream reader (in bytes), if known. Otherwise null.
        /// </summary>
        long? Size { get; }

        /// <summary>
        /// Gets the number of streams, if known. Otherwise null.
        /// </summary>
        int? StreamCount { get; }

        /// <summary>
        /// Creates a new reader for the same store, without reloading internal state (index, metadata, ...).
        /// </summary>
        /// <returns>A new reader for the same store.</returns>
        IStreamReader OpenNew();

        /// <summary>
        /// Opens the specified stream for reading.
        /// </summary>
        /// <typeparam name="T">The type of messages in stream.</typeparam>
        /// <param name="name">The name of the stream to open.</param>
        /// <param name="target">The function to call for every message in this stream.</param>
        /// <param name="allocator">An optional allocator to use when deserializing messages.</param>
        /// <param name="deallocator">An optional deallocator to use after the messages have been sent to the consumers.</param>
        /// <param name="errorHandler">The function to call if an error occurs when reading the stream.</param>
        /// <returns>The metadata describing the opened stream.</returns>
        /// <remarks>
        /// The target action is called for every message in the stream, as a traversal of
        /// the store is performed by calling the <see cref="MoveNext(out Envelope)"/> or
        /// <see cref="ReadAll(ReplayDescriptor, CancellationToken)"/> methods. The target
        /// action should not hold on to a reference or a shallow copy of the message past
        /// the action's return point, as the stream reader infrastructure might dispose
        /// of it. If the target needs to hold on to the message, it must create a deep
        /// clone of it.
        /// </remarks>
        IStreamMetadata OpenStream<T>(string name, Action<T, Envelope> target, Func<T> allocator = null, Action<T> deallocator = null, Action<SerializationException> errorHandler = null);

        /// <summary>
        /// Opens the specified stream for reading, in index form; providing only index entries to the target delegate.
        /// </summary>
        /// <typeparam name="T">The type of messages in stream.</typeparam>
        /// <param name="name">The name of the stream to open.</param>
        /// <param name="target">The function to call with a thunk which may be called to read, and envelope for every message in this stream.</param>
        /// <param name="allocator">An optional allocator to use when constructing messages.</param>
        /// <returns>The metadata describing the opened stream.</returns>
        /// <remarks>
        /// The target action is saved and later called as the data is read when
        /// <see cref="MoveNext(out Envelope)"/> or <see cref="ReadAll(ReplayDescriptor, CancellationToken)"/>
        /// are called. The target action is given the message Envelope and a Func which
        /// should be used to retrieve the message data. This Func may be held past the
        /// return point of the target action, as a kind of "index-thunk" that can be later
        /// called to retrieve the data. This function may be called, given the current
        /// IStreamReader or a new instance against the same store. Internally, the Func
        /// is likely a closure over information needed for retrieval (byte position, file
        /// extent, etc.) but these implementation details remain opaque to users of the
        /// reader (different implementors of the <see cref="IStreamReader"/> interface
        /// may choose different mechanisms for implementing this functionality.)
        ///
        /// In contrast to the <see cref="OpenStream{T}(string, Action{T, Envelope}, Func{T}, Action{T}, Action{SerializationException})"/>
        /// approach for reading stream messages, when using this method, not deallocator
        /// is provided. The reposibility for deallocating or disposing the messages
        /// returned by the index function rests with the caller of that function.
        /// </remarks>
        IStreamMetadata OpenStreamIndex<T>(string name, Action<Func<IStreamReader, T>, Envelope> target, Func<T> allocator = null);

        /// <summary>
        /// Moves the reader to the start of the specified interval and restricts the read to messages within the interval.
        /// </summary>
        /// <param name="interval">The interval for reading data.</param>
        /// <param name="useOriginatingTime">Indicates whether the interval refers to originating times or creation times.</param>
        void Seek(TimeInterval interval, bool useOriginatingTime = false);

        /// <summary>
        /// Positions the reader to the next message from any one of the opened streams.
        /// </summary>
        /// <param name="envelope">The envelope associated with the message read.</param>
        /// <returns>True if there are more messages, false if no more messages are available.</returns>
        bool MoveNext(out Envelope envelope);

        /// <summary>
        /// Indicates whether this store is still being written to by an active writer.
        /// </summary>
        /// <returns>True if an active writer is still writing to this store, false otherwise.</returns>
        bool IsLive();

        /// <summary>
        /// Returns a metadata descriptor for the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The metadata describing the specified stream.</returns>
        IStreamMetadata GetStreamMetadata(string streamName);

        /// <summary>
        /// Returns the supplemental metadata for a specified stream.
        /// </summary>
        /// <typeparam name="T">Type of supplemental metadata.</typeparam>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The metadata associated with the stream.</returns>
        T GetSupplementalMetadata<T>(string streamName);

        /// <summary>
        /// Checks whether the specified stream exists in this store.
        /// </summary>
        /// <param name="name">The name of the stream to look for.</param>
        /// <returns>True if a stream with the specified name exists, false otherwise.</returns>
        bool ContainsStream(string name);

        /// <summary>
        /// Reads all the messages within the time interval specified by the replay descriptor and calls the registered delegates.
        /// </summary>
        /// <param name="descriptor">The replay descriptor providing the interval to read.</param>
        /// <param name="cancelationToken">A token that can be used to cancel the operation.</param>
        void ReadAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default);
    }
}
