// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents an object used to read streams.
    /// </summary>
    public interface IStreamCache : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this reader has been canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Gets a list of outstanding read requests.
        /// </summary>
        IReadOnlyList<ReadRequest> ReadRequests { get; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        string StreamName { get; }

        /// <summary>
        /// Gets the stream adapter.
        /// </summary>
        IStreamAdapter StreamAdapter { get; }

        /// <summary>
        /// Gets a value indicating whether the stream reader currently has any instant stream readers.
        /// </summary>
        bool HasInstantStreamReaders { get; }

        /// <summary>
        /// Gets a value indicating whether the stream reader has updates that have not yet been committed to disk.
        /// </summary>
        bool HasUncommittedUpdates { get; }

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
        /// <param name="streamReader">Reader to open stream with.</param>
        /// <param name="useIndex">Indicates reader should read the stream index.</param>
        void OpenStream(IStreamReader streamReader, bool useIndex);

        /// <summary>
        /// Registers an instant data target to be notified when new data for a stream is available.
        /// </summary>
        /// <typeparam name="TTarget">The type of data the instant visualization object consumes.</typeparam>
        /// <param name="target">An instant data target that specifies the stream binding, the cursor epsilon, and the callback to call when new data is available.</param>
        /// <param name="viewRange">The initial time range over which data is expected.</param>
        void RegisterInstantDataTarget<TTarget>(InstantDataTarget target, TimeInterval viewRange);

        /// <summary>
        /// Unregisters an instant data target from data notification.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        void UnregisterInstantDataTarget(Guid registrationToken);

        /// <summary>
        /// Updates the cursor epsilon for an instant data target.  Changes to cursor epsilon will
        /// impact which data is served to the instant visualization object for a given cursor time.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        /// <param name="epsilon">A relative time interval specifying the window around a message time that may be considered a match.</param>
        void UpdateInstantDataTargetEpsilon(Guid registrationToken, RelativeTimeInterval epsilon);

        /// <summary>
        /// Reads instant data from the stream at the given cursor time and notifies all registered instant visualization objects of the new data.
        /// </summary>
        /// <param name="streamReader">The reader to read from.</param>
        /// <param name="cursorTime">The current time at the cursor.</param>
        void ReadInstantData(IStreamReader streamReader, DateTime cursorTime);

        /// <summary>
        /// Gets originating time of the message in a stream that's closest to a given time.
        /// </summary>
        /// <param name="time">The time for which to return the message with the closest originating time.</param>
        /// <returns>The originating time of the message closest to time.</returns>
        public DateTime? GetOriginatingTimeOfNearestInstantMessage(DateTime time);

        /// <summary>
        /// Notifies the data store reader that the range of data that may be of interest to instant data targets has changed.
        /// </summary>
        /// <param name="viewRange">The new view range.</param>
        void OnInstantViewRangeChanged(TimeInterval viewRange);

        /// <summary>
        /// Creates a view of the indices identified by the matching start and end times and asynchronously fills it in.
        /// </summary>
        /// <param name="startTime">Start time of indices to read.</param>
        /// <param name="endTime">End time of indices to read.</param>
        /// <returns>Observable view of cache entries.</returns>
        ObservableKeyedCache<DateTime, StreamCacheEntry>.ObservableKeyedView ReadIndex(DateTime startTime, DateTime endTime);

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
            ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
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