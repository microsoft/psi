// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines an interface for providers of stream interval data.
    /// </summary>
    public interface IStreamIntervalProvider : IStreamDataProvider
    {
        /// <summary>
        /// Gets the stream adapter.
        /// </summary>
        IStreamAdapter StreamAdapter { get; }

        /// <summary>
        /// Gets a value indicating whether the stream reader has updates that have not yet been committed to disk.
        /// </summary>
        bool HasUncommittedUpdates { get; }

        /// <summary>
        /// Creates a view of the messages identified by the matching parameters and asynchronously fills it in.
        /// View mode can be one of three values:
        ///     Fixed - fixed range based on start and end times
        ///     TailCount - sliding dynamic range that includes the tail of the underlying data based on quantity
        ///     TailRange - sliding dynamic range that includes the tail of the underlying data based on function.
        /// </summary>
        /// <typeparam name="TItem">The type of the message to read.</typeparam>
        /// <param name="viewMode">Mode the view will be created in.</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView ReadStream<TItem>(
            ObservableKeyedViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange);

        /// <summary>
        /// Registers a subscriber to stream interval data.
        /// </summary>
        /// <param name="streamSource">A stream source that indicates the store and stream data that the client consumes.</param>
        /// <returns>A unique subscriber id that should be provided when the subscriber unregisters.</returns>
        public Guid RegisterStreamIntervalSubscriber(StreamSource streamSource);

        /// <summary>
        /// Unregisters a subscriber from stream interval data.
        /// </summary>
        /// <param name="subscriberId">The id that was returned to the subscriber when it registered.</param>
        public void UnregisterStreamIntervalSubscriber(Guid subscriberId);

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="TItem">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="viewMode">The view mode, which may be either fixed or live data.</param>
        /// <param name="startTime">The start time of the view range.</param>
        /// <param name="endTime">The end time of the view range.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="tailCount">Not yet supported and should be set to zero.</param>
        /// <param name="tailRange">Tail duration function. Computes the view range start time given an end time. Applies to live view mode only.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView ReadSummary<TItem>(
            StreamSource streamSource,
            ObservableKeyedViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            TimeSpan interval,
            uint tailCount,
            Func<DateTime, DateTime> tailRange);

        /// <summary>
        /// Performs a series of updates to the messages in a stream.
        /// </summary>
        /// <typeparam name="TItem">The type of the messages in the stream.</typeparam>
        /// <param name="updates">A collection of updates to perform.</param>
        void UpdateStream<TItem>(IEnumerable<StreamUpdate<TItem>> updates);

        /// <summary>
        /// Gets the collection of all updates to the stream.
        /// </summary>
        /// <returns>A collection of updates to the stream.  If the boolean value is true then the update is an upsert operation, otherwise it's a delete operation.</returns>
        IEnumerable<(bool, dynamic, DateTime)> GetUncommittedUpdates();
    }
}
