// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Represents metadata used in storing stream data in a store.
    /// </summary>
    public interface IStreamMetadata
    {
        /// <summary>
        /// Gets the name of the stream the metadata represents.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the id of the stream the metadata represents.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the name of the type of data contained in the stream the metadata represents.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets the name of the type of supplemental metadata for the stream the metadata represents.
        /// </summary>
        string SupplementalMetadataTypeName { get; }

        /// <summary>
        /// Gets the name of the partition where the stream is stored.
        /// </summary>
        string PartitionName { get; }

        /// <summary>
        /// Gets the path of the partition where the stream is stored.
        /// </summary>
        string PartitionPath { get; }

        /// <summary>
        /// Gets the time when the stream was opened.
        /// </summary>
        DateTime OpenedTime { get; }

        /// <summary>
        /// Gets the time when the stream was closed.
        /// </summary>
        DateTime ClosedTime { get; }

        /// <summary>
        /// Gets a value indicating whether the stream has been closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Gets the first creation time of a message in the stream.
        /// </summary>
        /// <seealso cref="Envelope.CreationTime"/>
        DateTime FirstMessageCreationTime { get; }

        /// <summary>
        /// Gets the last creation time of a message in the stream.
        /// </summary>
        /// <seealso cref="Envelope.CreationTime"/>
        DateTime LastMessageCreationTime { get; }

        /// <summary>
        /// Gets the first originating time of a message in the stream.
        /// </summary>
        /// <seealso cref="Envelope.OriginatingTime"/>
        DateTime FirstMessageOriginatingTime { get; }

        /// <summary>
        /// Gets the last originating time of a message in the stream.
        /// </summary>
        /// <seealso cref="Envelope.OriginatingTime"/>
        DateTime LastMessageOriginatingTime { get; }

        /// <summary>
        /// Gets the average size of messages in the stream.
        /// </summary>
        int AverageMessageSize { get; }

        /// <summary>
        /// Gets the average latency of messages in the stream, in microseconds.
        /// </summary>
        int AverageLatency { get; }

        /// <summary>
        /// Gets the number of messages in the stream.
        /// </summary>
        int MessageCount { get; }

        /// <summary>
        /// Gets supplemental stream metadata.
        /// </summary>
        /// <typeparam name="T">Type of supplemental metadata.</typeparam>
        /// <returns>Supplemental metadata.</returns>
        T GetSupplementalMetadata<T>();

        /// <summary>
        /// Updates this stream metadata with the specified envelope and size.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <param name="size">The size.</param>
        void Update(Envelope envelope, int size);

        /// <summary>
        /// Updates this stream metadata with the times and originating times of the first and last messages.
        /// </summary>
        /// <param name="messagesTimeInterval">A TimeInterval representing the times of the first and last messages in the stream.</param>
        /// <param name="messagesOriginatingTimeInterval">A TimeInterval representing the originating times of the first and last messages in the stream.</param>
        void Update(TimeInterval messagesTimeInterval, TimeInterval messagesOriginatingTimeInterval);
    }
}