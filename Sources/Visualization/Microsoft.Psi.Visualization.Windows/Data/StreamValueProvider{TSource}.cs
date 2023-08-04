// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Implements a provider of stream values.
    /// </summary>
    /// <typeparam name="TSource">The type of messages in stream.</typeparam>
    /// <remarks>
    /// A stream value provider is associated with a specific stream. It reads values
    /// from the stream, and can supply adapted values to registered subscribers. The
    /// registrations are managed via the <see cref="DataManager"/> object. The provider
    /// is associated with a specified stream, and supports multiple subscribers to
    /// that stream which may have different epsilon-intervals for access, and different
    /// adapters. Note that the type of data ultimately produced might not be TSource,
    /// since adapters might be involved.
    /// </remarks>
    internal class StreamValueProvider<TSource> : StreamDataProvider<TSource>, IStreamValueProvider
    {
        /// <summary>
        /// The collection of publishers, organized by the relative time interval.
        /// </summary>
        private readonly Dictionary<RelativeTimeInterval, List<IStreamValuePublisher<TSource>>> publishers;

        /// <summary>
        /// A lock for controlling concurrent access to the index and indexBuffer structures.
        /// </summary>
        private readonly object indexLock = new ();

        /// <summary>
        /// The index.
        /// </summary>
        private readonly ObservableKeyedCache<DateTime, MessageIndex<TSource>> index;

        /// <summary>
        /// The index buffer.
        /// </summary>
        private readonly List<MessageIndex<TSource>> indexBuffer;

        /// <summary>
        /// The index view.
        /// </summary>
        private ObservableKeyedCache<DateTime, MessageIndex<TSource>>.ObservableKeyedView indexView = null;

        /// <summary>
        /// The time range of the index view.
        /// </summary>
        private NavigatorRange indexViewRange = new (DateTime.MinValue, DateTime.MinValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamValueProvider{T}"/> class.
        /// </summary>
        /// <param name="streamSource">The stream source.</param>
        public StreamValueProvider(StreamSource streamSource)
            : base(streamSource)
        {
            this.indexView = null;
            this.publishers = new Dictionary<RelativeTimeInterval, List<IStreamValuePublisher<TSource>>>();

            var indexComparer = Comparer<MessageIndex<TSource>>.Create((i1, i2) => i1.OriginatingTime.CompareTo(i2.OriginatingTime));
            this.index = new ObservableKeyedCache<DateTime, MessageIndex<TSource>>(null, indexComparer, ie => ie.OriginatingTime);
            this.indexBuffer = new List<MessageIndex<TSource>>(1000);
        }

        /// <inheritdoc/>
        public override bool HasSubscribers => this.publishers.Count > 0;

        /// <summary>
        /// Dispatches read data to clients of this reader. Called by <see cref="DataStoreReader"/> on the UI thread to populate data cache.
        /// </summary>
        public override void DispatchData()
        {
            lock (this.indexLock)
            {
                if (this.indexBuffer.Count > 0)
                {
                    this.index.AddRange(this.indexBuffer);
                    this.indexBuffer.Clear();
                }
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            lock (this.indexLock)
            {
                this.index.Clear();
                this.indexBuffer.Clear();
            }
        }

        /// <inheritdoc/>
        public Guid RegisterStreamValueSubscriber<TTarget>(
            IStreamAdapter streamAdapter,
            RelativeTimeInterval epsilonTimeInterval,
            Action<bool, TTarget, DateTime, DateTime> callback)
        {
            var publisher = default(StreamValuePublisher<TSource, TTarget>);

            lock (this.publishers)
            {
                // Check if we have a publisher for this epsilon interval
                if (!this.publishers.ContainsKey(epsilonTimeInterval))
                {
                    this.publishers.Add(epsilonTimeInterval, new List<IStreamValuePublisher<TSource>>());
                }

                // Get the list of publishers for the specified epsilon interval
                var epsilonTimeIntervalPublishers = this.publishers[epsilonTimeInterval];

                // Check if we already have a publisher that uses the specified stream adapter
                publisher = epsilonTimeIntervalPublishers.FirstOrDefault(p => p.StreamAdapter.Equals(streamAdapter)) as StreamValuePublisher<TSource, TTarget>;
                if (publisher == null)
                {
                    // If not found, create a new publisher
                    publisher = new StreamValuePublisher<TSource, TTarget>(streamAdapter);
                    epsilonTimeIntervalPublishers.Add(publisher);
                }
            }

            // Register the subscriber with this publisher
            return publisher.RegisterSubscriber(callback);
        }

        /// <inheritdoc/>
        public void UnregisterStreamValueSubscriber<TTarget>(Guid subscriberId)
        {
            lock (this.publishers)
            {
                foreach (var epsilonTimeIntervalPublishers in this.publishers.Values)
                {
                    foreach (var publisher in epsilonTimeIntervalPublishers)
                    {
                        if (publisher.HasSubscriber(subscriberId))
                        {
                            publisher.UnregisterSubscriber(subscriberId);
                        }
                    }

                    epsilonTimeIntervalPublishers.RemoveAll(publisher => !publisher.HasSubscribers);
                }

                foreach (var epsilonTimeInterval in this.publishers.Keys.ToArray())
                {
                    if (!this.publishers[epsilonTimeInterval].Any())
                    {
                        this.publishers.Remove(epsilonTimeInterval);
                    }
                }
            }

            // If no publishers remain, remove the index view
            if (!this.publishers.Any())
            {
                this.OnNoRemainingSubscribers();
            }
        }

        /// <inheritdoc/>
        public void SetCacheInterval(TimeInterval viewRange)
        {
            // Check if the navigator view range exceeds the current range of the data index
            if (viewRange.Left < this.indexViewRange.StartTime || viewRange.Right > this.indexViewRange.EndTime)
            {
                // Set a new data index range thats extends to the left and right of the navigator view by the navigator view
                // duration so that we're not constantly needing to initiate an index read every time the navigator moves.
                TimeSpan viewDuration = viewRange.Span;
                this.indexViewRange.Set(
                    viewRange.Left > DateTime.MinValue + viewDuration ? viewRange.Left - viewDuration : DateTime.MinValue,
                    viewRange.Right < DateTime.MaxValue - viewDuration ? viewRange.Right + viewDuration : DateTime.MaxValue);

                this.indexView = this.ReadIndex(this.indexViewRange.StartTime, this.indexViewRange.EndTime);
            }
        }

        /// <inheritdoc/>
        public void ReadAndPublishStreamValue(IStreamReader streamReader, DateTime dateTime)
        {
            foreach (var epsilonTimeInterval in this.publishers.Keys)
            {
                // Get the index of the data, given the cursor time, and the epsilon interval.
                int index = IndexHelper.GetIndexForTime(dateTime, epsilonTimeInterval, this.index?.Count ?? 0, (idx) => this.index[idx].OriginatingTime);

                if (index >= 0)
                {
                    // Get the index entry
                    var indexedStreamReaderThunk = this.index[index];

                    // Read the data (this allocates)
                    var data = indexedStreamReaderThunk.Read(streamReader);

                    // Publish the value
                    foreach (var publisher in this.publishers[epsilonTimeInterval])
                    {
                        publisher.PublishValue(true, data, indexedStreamReaderThunk.OriginatingTime, indexedStreamReaderThunk.CreationTime);
                    }

                    // Deallocate to control the lifetime of the read object
                    this.Deallocator?.Invoke(data);
                }
                else
                {
                    // Cache miss, attempt to seek directly. The cache miss could happen because the
                    // the stream index data structure is being populated, or because the epsilon interval
                    // is set such that no data point is found. In either case, a direct read is
                    // attempted.
                    streamReader.Seek(dateTime + epsilonTimeInterval, true);
                    streamReader.OpenStream(
                        this.StreamName,
                        (data, envelope) =>
                        {
                            foreach (var publisher in this.publishers[epsilonTimeInterval])
                            {
                                publisher.PublishValue(true, data, envelope.OriginatingTime, envelope.CreationTime);
                            }
                        },
                        this.Allocator,
                        this.Deallocator);

                    // This will force the read of the next message. If a message within the seek
                    // constraints is found, the delegate passed to OpenStream above is executed
                    // to capture the data. If no message exists in that specified seek interval
                    // the delegate does not execute and found below becomes false.
                    var found = streamReader.MoveNext(out var envelope);

                    // If no message was found
                    if (!found)
                    {
                        // Then signal the data providers that to message is available
                        foreach (var publisher in this.publishers[epsilonTimeInterval])
                        {
                            publisher.PublishValue(false, default, default, default);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void OpenStream(IStreamReader streamReader) =>
            streamReader.OpenStreamIndex(this.StreamName, this.OnReceiveIndex, this.Allocator);

        /// <inheritdoc/>
        public override DateTime? GetTimeOfNearestMessage(DateTime time, NearestType nearestType)
        {
            int index = IndexHelper.GetIndexForTime(time, this.indexView.Count, (idx) => this.indexView[idx].OriginatingTime, nearestType);
            return (index >= 0) ? this.indexView[index].OriginatingTime : null;
        }

        private ObservableKeyedCache<DateTime, MessageIndex<TSource>>.ObservableKeyedView ReadIndex(DateTime startTime, DateTime endTime)
        {
            lock (this.ReadRequestsInternal)
            {
                this.ReadRequestsInternal.AddRange(this.ComputeReadRequests(startTime, endTime));
            }

            return this.index.GetView(ObservableKeyedViewMode.Fixed, startTime, endTime, 0, null);
        }

        private IEnumerable<ReadRequest> ComputeReadRequests(DateTime startTime, DateTime endTime)
        {
            // get the existing read request intervals
            var existingReadRequestIntervals = this.ReadRequestsInternal.Where(rr => rr.ReadIndicesOnly == true).Select(rr => (rr.StartTime, rr.EndTime));

            // get the existing index view intervals
            var existingIndexViewIntervals = this.index.ViewExtents.Select(ve => (ve.Item1, ve.Item2));

            // compute the remaining intervals that should be retrieved
            var remainingIntervals = TimeIntervalHelper.ComputeRemainingIntervals(startTime, endTime, existingReadRequestIntervals.Union(existingIndexViewIntervals));

            // return a set of read requests based on these intervals
            return remainingIntervals.Select(interval => new ReadRequest(interval.StartTime, interval.EndTime, 0, null, true));
        }

        private void OnReceiveIndex(Func<IStreamReader, TSource> indexThunk, Envelope env)
        {
            if (!this.IsStopped)
            {
                lock (this.indexLock)
                {
                    this.indexBuffer.Add(new MessageIndex<TSource>(indexThunk, env.CreationTime, env.OriginatingTime));
                }
            }
        }
    }
}
