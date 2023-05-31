// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Represents an object used to read streams.
    /// </summary>
    /// <typeparam name="T">The type of messages in stream AFTER they have been adapted by the data adapter (if applicable).</typeparam>
    public class StreamIntervalProvider<T> : StreamDataProvider<T>, IStreamIntervalProvider
    {
        /// <summary>
        /// The collection of updates that have been performed on the stream since it was last saved.
        /// </summary>
        private readonly SortedDictionary<DateTime, StreamUpdateWithView<T>> updateList = new ();

        /// <summary>
        /// The list of subscribers to this stream interval provider.
        /// </summary>
        private readonly List<Guid> subscribers = new ();

        /// <summary>
        /// A lock for controlling concurrent access to the data and dataBuffer structures.
        /// </summary>
        private readonly object dataLock = new ();

        /// <summary>
        /// The data.
        /// </summary>
        private readonly ObservableKeyedCache<DateTime, Message<T>> data;

        /// <summary>
        /// The data buffer.
        /// </summary>
        private readonly List<Message<T>> dataBuffer;

        /// <summary>
        /// The collection of stream summary managers that summarize data from this stream interval provider.
        /// </summary>
        private readonly StreamSummaryManagers streamSummaryManagers = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamIntervalProvider{T}"/> class.
        /// </summary>
        /// <param name="streamSource">The stream source.</param>
        public StreamIntervalProvider(StreamSource streamSource)
            : base(streamSource)
        {
            this.StreamAdapter = streamSource.StreamAdapter;

            var itemComparer = Comparer<Message<T>>.Create((m1, m2) => m1.OriginatingTime.CompareTo(m2.OriginatingTime));
            this.data = new ObservableKeyedCache<DateTime, Message<T>>(null, itemComparer, m => m.OriginatingTime);
            this.data.CollectionChanged += this.OnCollectionChanged;
            this.dataBuffer = new List<Message<T>>(1000);
        }

        /// <summary>
        /// Gets the stream adapter.
        /// </summary>
        public IStreamAdapter StreamAdapter { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the stream reader has updates that have not yet been committed to disk.
        /// </summary>
        public bool HasUncommittedUpdates => this.updateList.Count > 0;

        /// <inheritdoc/>
        public override bool HasSubscribers => this.subscribers.Count > 0;

        /// <inheritdoc/>
        public override void OpenStream(IStreamReader streamReader)
        {
            if (streamReader == null)
            {
                throw new ArgumentNullException(nameof(streamReader));
            }

            if (this.StreamAdapter == null)
            {
                streamReader.OpenStream(this.StreamName, this.OnReceiveData, this.Allocator, this.Deallocator, this.OnReadError);
            }
            else
            {
                dynamic dynStreamAdapter = this.StreamAdapter;
                dynamic dynAdaptedReceiver = dynStreamAdapter.AdaptReceiver(new Action<T, Envelope>(this.OnReceiveData));
                dynamic dynReadError = new Action<SerializationException>(this.OnReadError);
                streamReader.OpenStream(this.StreamName, dynAdaptedReceiver, dynStreamAdapter.SourceAllocator, dynStreamAdapter.SourceDeallocator, dynReadError);
            }
        }

        /// <inheritdoc/>
        public override DateTime? GetTimeOfNearestMessage(DateTime time, NearestType nearestType)
        {
            int index = IndexHelper.GetIndexForTime(time, this.data.Count, (idx) => this.data[idx].OriginatingTime, nearestType);
            return (index >= 0) ? this.data[index].OriginatingTime : null;
        }

        /// <inheritdoc/>
        public override void DispatchData()
        {
            lock (this.dataLock)
            {
                if (this.dataBuffer.Count > 0)
                {
                    this.data.AddRange(this.dataBuffer);
                    this.dataBuffer.Clear();
                }
            }

            this.GetStreamSummaryManagers()
                .ForEach(streamSummaryManager =>
                {
                    // dispatch data for stream summary managers
                    streamSummaryManager.DispatchData();
                });
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            lock (this.dataLock)
            {
                this.data.CollectionChanged -= this.OnCollectionChanged;

                if (this.StreamAdapter == null)
                {
                    foreach (var message in this.data)
                    {
                        this.Deallocator?.Invoke(message.Data);
                    }

                    foreach (var message in this.dataBuffer)
                    {
                        this.Deallocator?.Invoke(message.Data);
                    }
                }
                else
                {
                    dynamic dynStreamAdapter = this.StreamAdapter;
                    foreach (var message in this.data)
                    {
                        dynStreamAdapter.Dispose(message.Data);
                    }

                    foreach (var message in this.dataBuffer)
                    {
                        dynStreamAdapter.Dispose(message.Data);
                    }
                }

                this.data.Clear();
                this.dataBuffer.Clear();
            }
        }

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
        public ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView ReadStream<TItem>(
            ObservableKeyedViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            if (viewMode == ObservableKeyedViewMode.Fixed && endTime < startTime)
            {
                throw new ArgumentException("End time must be greater than or equal to start time.", nameof(endTime));
            }
            else if (viewMode == ObservableKeyedViewMode.TailCount && tailCount <= 0)
            {
                throw new ArgumentException("Tail count must be greater than 0", nameof(tailCount));
            }
            else if (viewMode == ObservableKeyedViewMode.TailRange && tailRange == null)
            {
                throw new ArgumentNullException(nameof(tailRange));
            }

            lock (this.ReadRequestsInternal)
            {
                this.ReadRequestsInternal.AddRange(this.ComputeReadRequests(startTime, endTime));
            }

            return (this.data as ObservableKeyedCache<DateTime, Message<TItem>>).GetView(viewMode, startTime, endTime, tailCount, tailRange);
        }

        /// <inheritdoc/>
        public Guid RegisterStreamIntervalSubscriber(StreamSource streamSource)
        {
            Guid subscriberId;

            // If the data is summarized, pass the call to the stream summary manager.
            if (streamSource.Summarizer != null)
            {
                subscriberId = this.GetOrCreateStreamSummaryManager(streamSource).RegisterSubscriber();
            }
            else
            {
                subscriberId = Guid.NewGuid();
            }

            // Register with this stream interval provider as well.
            this.subscribers.Add(subscriberId);

            return subscriberId;
        }

        /// <inheritdoc/>
        public void UnregisterStreamIntervalSubscriber(Guid subscriberId)
        {
            // Unregister from the stream summary manager first
            this.GetStreamSummaryManagers().ForEach(ssm => ssm.UnregisterSubscriber(subscriberId));

            // Also unregister locally.
            lock (this.subscribers)
            {
                if (this.subscribers.Contains(subscriberId))
                {
                    this.subscribers.Remove(subscriberId);
                }
            }

            // Check if we have no more subscribers.
            if (this.subscribers.Count == 0)
            {
                this.OnNoRemainingSubscribers();
            }
        }

        /// <inheritdoc/>
        public ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView ReadSummary<TItem>(
            StreamSource streamSource,
            ObservableKeyedViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            TimeSpan interval,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            return this.GetStreamSummaryManagerOrDefault(streamSource).ReadSummary<TItem>(streamSource, viewMode, startTime, endTime, interval, tailCount, tailRange);
        }

        /// <summary>
        /// Performs a series of updates to the messages in a stream.
        /// </summary>
        /// <typeparam name="TItem">The type of the messages in the stream.</typeparam>
        /// <param name="updates">A collection of updates to perform.</param>
        public void UpdateStream<TItem>(IEnumerable<StreamUpdate<TItem>> updates)
        {
            foreach (StreamUpdate<TItem> update in updates)
            {
                switch (update.UpdateType)
                {
                    case StreamUpdateType.Add:
                        this.UpdateStreamAdd(update as StreamUpdate<T>);
                        break;
                    case StreamUpdateType.Replace:
                        this.UpdateStreamReplace(update as StreamUpdate<T>);
                        break;
                    case StreamUpdateType.Delete:
                        this.UpdateStreamDelete(update as StreamUpdate<T>);
                        break;
                    default:
                        throw new ArgumentException($"Update type {update.UpdateType} is not supported.");
                }

                // Get a view to prevent the cache item being collected.
                var view = this.data.GetView(ObservableKeyedViewMode.Fixed, update.Message.OriginatingTime, update.Message.OriginatingTime.AddTicks(1), 0, null);

                // Update or replace the existing update in the update list.
                this.updateList[update.Message.OriginatingTime] = new StreamUpdateWithView<T>(update as StreamUpdate<T>, view);
            }
        }

        /// <summary>
        /// Gets the collection of all updates to the stream.
        /// </summary>
        /// <returns>A collection of updates to the stream.  If the boolean value is true then the update is an upsert operation, otherwise it's a delete operation.</returns>
        public IEnumerable<(bool, dynamic, DateTime)> GetUncommittedUpdates()
        {
            var updates = new List<(bool, dynamic, DateTime)>();

            foreach (var originatingTime in this.updateList.Keys)
            {
                var update = this.updateList[originatingTime];
                updates.Add((update.UpdateType != StreamUpdateType.Delete, update.Message.Data, originatingTime));
            }

            this.updateList.Clear();

            return updates;
        }

        private IEnumerable<ReadRequest> ComputeReadRequests(DateTime startTime, DateTime endTime)
        {
            // get the existing read request intervals
            var existingReadRequestIntervals = this.ReadRequestsInternal.Where(rr => rr.ReadIndicesOnly == true).Select(rr => (rr.StartTime, rr.EndTime));

            // get the existing index view intervals
            var existingIndexViewIntervals = this.data.ViewExtents.Select(ve => (ve.Item1, ve.Item2));

            // compute the remaining intervals that should be retrieved
            var remainingIntervals = TimeIntervalHelper.ComputeRemainingIntervals(startTime, endTime, existingReadRequestIntervals.Union(existingIndexViewIntervals));

            // return a set of read requests based on these intervals
            return remainingIntervals.Select(interval => new ReadRequest(interval.StartTime, interval.EndTime, 0, null, true));
        }

        private void UpdateStreamAdd(StreamUpdate<T> update)
        {
            if (this.data.FirstOrDefault(m => m.OriginatingTime == update.Message.OriginatingTime) != default)
            {
                throw new ArgumentException("The cache already contains a message with the requested originating time");
            }

            this.data.Add(update.Message);
        }

        private void UpdateStreamReplace(StreamUpdate<T> update)
        {
            // Get the old message, it must exist.
            Message<T> oldMessage = this.data.FirstOrDefault(m => m.OriginatingTime == update.Message.OriginatingTime);
            if (oldMessage == default)
            {
                throw new ArgumentException("The cache does not contain a message with the requested originating time");
            }

            // Remove the old message
            this.data.Remove(oldMessage);

            // Insert the new message
            this.UpdateStreamAdd(update);
        }

        private void UpdateStreamDelete(StreamUpdate<T> update)
        {
            // Find the item in the cache, it must exist.
            if (!this.data.Remove(update.Message))
            {
                throw new ArgumentException("The cache does not contain a message with the requested originating time");
            }
        }

        /// <summary>
        /// Called when the simple reader has read a new message from the store.
        /// </summary>
        /// <param name="data">The data in the message that was read.</param>
        /// <param name="env">The envelope of the message that was read.</param>
        private void OnReceiveData(T data, Envelope env)
        {
            if (!this.IsStopped)
            {
                // If the update list contains an item with the same originating
                // time, then use that instead of the data coming from the store.
                if (this.updateList.Any() && this.updateList.ContainsKey(env.OriginatingTime))
                {
                    // Get the update object
                    StreamUpdateWithView<T> update = this.updateList[env.OriginatingTime];

                    // If the update was an Add, add its message to the data buffer instead
                    // of the one read from the store. If the update was a delete, then
                    // don't add anything to the data buffer.
                    switch (update.UpdateType)
                    {
                        case StreamUpdateType.Add:
                            lock (this.dataLock)
                            {
                                this.dataBuffer.Add(update.Message);
                            }

                            break;
                        case StreamUpdateType.Delete:
                            // The item was deleted, so do nothing.
                            break;
                        default:
                            throw new InvalidOperationException("The update type is not supported.");
                    }
                }
                else
                {
                    // the data is obtained from an IStreamReader, and we therefore need to clone it
                    // to hold on to it past the lifetime of the OnReceiveData method.
                    var message = new Message<T>(data.DeepClone(), env.OriginatingTime, env.CreationTime, env.SourceId, env.SequenceId);
                    lock (this.dataLock)
                    {
                        this.dataBuffer.Add(message);
                    }
                }
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // release any removed elements
            if (e.OldItems != null)
            {
                foreach (Message<T> item in e.OldItems)
                {
                    this.Deallocator?.Invoke(item.Data);
                }
            }
        }

        private List<StreamSummaryManager> GetStreamSummaryManagers()
        {
            lock (this.streamSummaryManagers)
            {
                return this.streamSummaryManagers.ToList();
            }
        }

        private StreamSummaryManager GetOrCreateStreamSummaryManager(StreamSource streamSource)
        {
            var streamSummaryManager = this.GetStreamSummaryManagerOrDefault(streamSource);

            if (streamSummaryManager == null)
            {
                lock (this.streamSummaryManagers)
                {
                    streamSummaryManager = new StreamSummaryManager(streamSource);
                    streamSummaryManager.NoRemainingSubscribers += this.StreamSummaryManager_NoRemainingSubscribers;
                    this.streamSummaryManagers.Add(streamSummaryManager);
                }
            }

            return streamSummaryManager;
        }

        private StreamSummaryManager GetStreamSummaryManagerOrDefault(StreamSource streamSource)
        {
            if (streamSource == null)
            {
                throw new ArgumentNullException(nameof(streamSource));
            }

            if (string.IsNullOrWhiteSpace(streamSource.StoreName))
            {
                throw new ArgumentNullException(nameof(streamSource.StoreName));
            }

            if (string.IsNullOrWhiteSpace(streamSource.StorePath))
            {
                throw new ArgumentNullException(nameof(streamSource.StorePath));
            }

            if (string.IsNullOrWhiteSpace(streamSource.StreamName))
            {
                throw new ArgumentNullException(nameof(streamSource.StreamName));
            }

            var key = Tuple.Create(streamSource.StoreName, streamSource.StorePath, streamSource.StreamName, streamSource.StreamAdapter);

            if (this.streamSummaryManagers.Contains(key))
            {
                return this.streamSummaryManagers[key];
            }
            else
            {
                return null;
            }
        }

        private void StreamSummaryManager_NoRemainingSubscribers(object sender, EventArgs e)
        {
            var streamSummaryManager = sender as StreamSummaryManager;

            // Create the key for the stream summary manager.
            var key = Tuple.Create(
                streamSummaryManager.StoreName,
                streamSummaryManager.StorePath,
                streamSummaryManager.StreamName,
                streamSummaryManager.StreamAdapter);

            // If the stream summary manager still has no subscribers, remove it from the collection and dispose of it.
            lock (this.streamSummaryManagers)
            {
                if (this.streamSummaryManagers[key].SubscriberCount == 0)
                {
                    this.streamSummaryManagers.Remove(key);
                    streamSummaryManager.NoRemainingSubscribers -= this.StreamSummaryManager_NoRemainingSubscribers;
                    streamSummaryManager.Dispose();
                }
            }
        }

        private class StreamSummaryManagers : KeyedCollection<Tuple<string, string, string, IStreamAdapter>, StreamSummaryManager>
        {
            protected override Tuple<string, string, string, IStreamAdapter> GetKeyForItem(StreamSummaryManager streamSummaryManager)
            {
                return Tuple.Create(streamSummaryManager.StoreName, streamSummaryManager.StorePath, streamSummaryManager.StreamName, streamSummaryManager.StreamAdapter);
            }
        }
    }
}
