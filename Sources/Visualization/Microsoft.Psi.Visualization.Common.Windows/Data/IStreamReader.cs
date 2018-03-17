// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents an object used to read streams.
    /// </summary>
    public interface IStreamReader : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this reader has been canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Gets a list of outstanding read requests.
        /// </summary>
        IReadOnlyList<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>> ReadRequests { get; }

        /// <summary>
        /// Gets the stream adapter type.
        /// </summary>
        Type StreamAdapterType { get; }

        /// <summary>
        /// Gets the stream binding.
        /// </summary>
        StreamBinding StreamBinding { get; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        string StreamName { get; }

        /// <summary>
        /// Gets the store name.
        /// </summary>
        string StoreName { get; }

        /// <summary>
        /// Gets the store path.
        /// </summary>
        string StorePath { get; }

        /// <summary>
        /// Cancels this reader.
        /// </summary>
        void Cancel();

        /// <summary>
        /// Completes the any read requests identified by the matching start and end times.
        /// </summary>
        /// <param name="startTime">Start time of read requests to complete.</param>
        /// <param name="endTime">End time of read requests to complete.</param>
        void CompleteReadRequest(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Dispatches read data to clients of this reader. Called by <see cref="DataStoreReader"/> on the UI thread to populate data cache.
        /// </summary>
        void DispatchData();

        /// <summary>
        /// Open stream given a reader.
        /// </summary>
        /// <param name="reader">Reader to open stream with.</param>
        void OpenStream(ISimpleReader reader);

        /// <summary>
        /// Reads a single message from a stream identified by a reader and an index entry.
        /// </summary>
        /// <typeparam name="TItem">The type of the message to read.</typeparam>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="indexEntry">The index entry indicating which message to read.</param>
        /// <returns>The message that was read.</returns>
        TItem Read<TItem>(ISimpleReader reader, IndexEntry indexEntry);

        /// <summary>
        /// Creates a view of the indices identified by the matching start and end times and asychronously fills it in.
        /// </summary>
        /// <param name="startTime">Start time of indices to read.</param>
        /// <param name="endTime">End time of indices to read.</param>
        /// <returns>Observable view of indices.</returns>
        ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView ReadIndex(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Creates a view of the messages identified by the matching parameters and asynchronously fills it in.
        /// View mode can be one of three values:
        ///     Fixed - fixed range based on start and end times
        ///     TailCount - sliding dynamic range that includes the tail of the underlying data based on quantity
        ///     TailRange - sliding dynamic range that includes the tail of the underlying data based on function
        /// </summary>
        /// <typeparam name="TItem">The type of the message to read.</typeparam>
        /// <param name="viewMode">Mode the view will be created in</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView ReadStream<TItem>(
            ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange);
    }
}