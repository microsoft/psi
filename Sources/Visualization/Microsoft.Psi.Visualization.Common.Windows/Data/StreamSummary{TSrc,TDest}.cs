// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents the summarized data of a stream generated from a specified summarizer type.
    /// Incoming data is summarized over a fixed interval and the resulting summarized values
    /// are stored in the cache as <see cref="IntervalData"/> items.
    /// </summary>
    /// <typeparam name="TSrc">The type of stream messages.</typeparam>
    /// <typeparam name="TDest">The type of the summarized interval data.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class StreamSummary<TSrc, TDest> : IStreamSummary
    {
        private Dictionary<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>, ObservableKeyedCache<DateTime, Message<TSrc>>.ObservableKeyedView> activeStreamViews;
        private ObservableKeyedCache<DateTime, IntervalData<TDest>> summaryCache;
        private Dictionary<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>, ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView> cachedSummaryViews;
        private List<List<IntervalData<TDest>>> summaryDataBuffer;
        private ISummarizer<TSrc, TDest> summarizer;
        private Func<IntervalData<TDest>, DateTime> keySelector;
        private Comparer<IntervalData<TDest>> itemComparer;
        private StreamBinding streamBinding;
        private TimeSpan interval;
        private uint maxCacheSize;
        private bool isCanceled = false;
        private object bufferLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamSummary{TSrc, TDest}"/> class.
        /// </summary>
        /// <param name="streamBinding">Stream binding indicating which stream to summarize.</param>
        /// <param name="interval">The time interval over which summary <see cref="IntervalData"/> values are calculated.</param>
        /// <param name="maxCacheSize">The maximum amount of data to cache before purging older summarized data.</param>
        public StreamSummary(StreamBinding streamBinding, TimeSpan interval, uint maxCacheSize)
        {
            this.streamBinding = streamBinding;
            this.interval = interval;
            this.maxCacheSize = maxCacheSize;

            this.summaryDataBuffer = new List<List<IntervalData<TDest>>>();

            this.keySelector = s => Summarizer<TSrc, TDest>.GetIntervalStartTime(s.OriginatingTime, interval);
            this.itemComparer = Comparer<IntervalData<TDest>>.Create((r1, r2) => this.keySelector(r1).CompareTo(this.keySelector(r2)));
            this.summaryCache = new ObservableKeyedCache<DateTime, IntervalData<TDest>>(null, this.itemComparer, this.keySelector);

            this.activeStreamViews = new Dictionary<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>, ObservableKeyedCache<DateTime, Message<TSrc>>.ObservableKeyedView>();
            this.cachedSummaryViews = new Dictionary<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>, ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView>();

            // Cache the summarizer (cast to the correct type) to call its methods later on without dynamic binding
            this.summarizer = this.StreamBinding.Summarizer as ISummarizer<TSrc, TDest>;
        }

        /// <inheritdoc />
        public TimeSpan Interval => this.interval;

        /// <inheritdoc />
        public virtual object[] Parameters => this.streamBinding.SummarizerArgs;

        /// <inheritdoc />
        public Type SummarizerType => this.streamBinding.SummarizerType;

        /// <summary>
        /// Gets ths stream binding.
        /// </summary>
        public StreamBinding StreamBinding => this.streamBinding;

        /// <summary>
        /// Gets a value indicating whether the stream summarizer has been canceled.
        /// </summary>
        public bool IsCanceled => this.isCanceled;

        /// <summary>
        /// Cancels this stream summarizer.
        /// </summary>
        public void Cancel()
        {
            this.isCanceled = true;
            this.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (this.bufferLock)
            {
                this.summaryCache?.Clear();
                this.summaryCache = null;
                this.summaryDataBuffer?.Clear();
                this.summaryDataBuffer = null;
            }
        }

        /// <inheritdoc />
        public void DispatchData()
        {
            lock (this.bufferLock)
            {
                if (this.summaryDataBuffer.Count > 0)
                {
                    // Add each contiguous range separately, checking for merges of the start and
                    // end elements with matching IntervalData values already in the cache.
                    foreach (var range in this.summaryDataBuffer)
                    {
                        var firstInterval = range[0];
                        var lastInterval = range[range.Count - 1];

                        // Check whether the first and last intervals already exist in the cache. If so, we
                        // assume these are overlapping intervals and perform a merge. We further assume
                        // that intervals between first and last do not already exist in the cache (i.e. only
                        // the boundary intervals can overlap, which should be the case if range requests
                        // do not overlap.
                        IntervalData<TDest> overlapped;
                        if (this.summaryCache.TryGetValue(this.keySelector(range[0]), out overlapped))
                        {
                            // Use summarizer-specific method to combine the two IntervalData values
                            overlapped = this.summarizer.Combine(range[0], overlapped);
                            this.summaryCache.UpdateOrAdd(overlapped);
                        }

                        if (this.summaryCache.TryGetValue(this.keySelector(range[range.Count - 1]), out overlapped))
                        {
                            // Use summarizer-specific method to combine the two IntervalData values
                            overlapped = this.summarizer.Combine(range[range.Count - 1], overlapped);
                            this.summaryCache.UpdateOrAdd(overlapped);
                        }

                        // Now add the entire range - the overlapped items which have already been updated will be ignored
                        this.summaryCache.AddRange(range);
                    }

                    this.summaryDataBuffer.Clear();
                }
            }
        }

        /// <inheritdoc />
        public ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView ReadSummary<TItem>(
            ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            if (viewMode == ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView.ViewMode.TailRange)
            {
                // Just read directly from the stream with the same tail range in live mode
                this.ReadStream(tailRange);
            }
            else if (viewMode == ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView.ViewMode.TailCount)
            {
                // We should read enough of the stream to generate the last tailCount intervals. So take the product of our
                // summarization interval and tailCount, and use that interval as the tail range to read from the stream.
                TimeSpan tailInterval = TimeSpan.FromTicks(this.Interval.Ticks * tailCount);
                this.ReadStream(last => last - tailInterval);
            }
            else if (viewMode == ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView.ViewMode.Fixed)
            {
                // Ranges for which we have not yet computed summary data.
                foreach (var range in this.ComputeRangeRequests(startTime, endTime))
                {
                    this.ReadStream(range.Item1, range.Item2);
                }
            }
            else
            {
                throw new NotSupportedException($"Summarization not yet supported in {viewMode} view mode.");
            }

            // Get or create the summary view from the cache
            return this.GetCachedSummaryView(
                (ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView.ViewMode)viewMode,
                startTime,
                endTime,
                tailCount,
                tailRange) as ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView;
        }

        /// <inheritdoc />
        public IntervalData<TItem> Search<TItem>(DateTime time, StreamSummarySearchMode mode)
        {
            var cache = this.summaryCache as ObservableKeyedCache<DateTime, IntervalData<TItem>>;

            // Find the first time range that contains the specified search time point
            var viewExtent = cache.ViewExtents.FirstOrDefault(v => v.Item1 <= time && v.Item2 >= time);

            if (viewExtent != null)
            {
                // Get a fixed view of the data over the time range to search
                var view = cache.GetView(
                    ObservableKeyedCache<DateTime, IntervalData<TItem>>.ObservableKeyedView.ViewMode.Fixed,
                    viewExtent.Item1,
                    viewExtent.Item2,
                    0,
                    null);

                int lo = 0;
                int hi = view.Count - 1;

                // Do a binary search for the IntervalData that includes the specified time
                while (lo <= hi)
                {
                    int mid = lo + ((hi - lo) / 2);
                    var current = view[mid];
                    if (time < current.OriginatingTime)
                    {
                        hi = mid - 1;
                    }
                    else if (time > current.EndTime)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        // An IntervalData was found that includes the specified time
                        return view[mid];
                    }
                }

                // No match was found, so return either the previous or next IntervalData
                // in the sequence based on the specified StreamSummarySearchMode.
                if (mode == StreamSummarySearchMode.Previous && hi >= 0)
                {
                    return view[hi];
                }
                else if (mode == StreamSummarySearchMode.Next && lo < view.Count)
                {
                    return view[lo];
                }
            }

            return default(IntervalData<TItem>);
        }

        private List<Tuple<DateTime, DateTime>> ComputeRangeRequests(DateTime startTime, DateTime endTime)
        {
            // adjust range request to account for existing views
            IEnumerable<Tuple<DateTime, DateTime>> views = null;
            lock (this.summaryCache)
            {
                views = this.summaryCache.ViewExtents
                    .Where(rr => rr.Item1 <= endTime && rr.Item2 >= startTime)
                    .Select(rr => Tuple.Create(rr.Item1, rr.Item2));
            }

            var newRangeRequests = new List<Tuple<DateTime, DateTime>>();
            this.ComputeRangeRequests(newRangeRequests, views, ref startTime, ref endTime);

            // finally add remaining range (if any) to range requests
            if (startTime < endTime)
            {
                newRangeRequests.Add(Tuple.Create(startTime, endTime));
            }

            return newRangeRequests;
        }

        private IEnumerable<Tuple<DateTime, DateTime>> ComputeRangeRequests(
            List<Tuple<DateTime, DateTime>> newRangeRequests,
            IEnumerable<Tuple<DateTime, DateTime>> ranges,
            ref DateTime startTime,
            ref DateTime endTime)
        {
            foreach (var range in ranges)
            {
                // completely overlapping
                if (range.Item1 <= startTime && range.Item2 >= endTime)
                {
                    startTime = endTime;
                    break;
                }

                // overlapping start
                else if (range.Item1 <= startTime && range.Item2 >= startTime)
                {
                    startTime = range.Item2;
                }

                // overlapping end
                else if (range.Item1 <= endTime && range.Item2 >= endTime)
                {
                    endTime = range.Item1;
                }

                // overlapping middle
                else if (range.Item1 >= startTime && range.Item2 <= endTime)
                {
                    // compute read requests for first new range
                    newRangeRequests.AddRange(this.ComputeRangeRequests(startTime, range.Item1));

                    // continue comptuing for second new range
                    startTime = range.Item2;
                }
            }

            return newRangeRequests;
        }

        private void Data_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IEnumerable<Message<TSrc>> dataSource = null;

            // Select added range or entire range as data source
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                dataSource = e.NewItems.Cast<Message<TSrc>>();
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Summarize the entire view
                dataSource = sender as IEnumerable<Message<TSrc>>;
            }
            else
            {
                throw new NotImplementedException($"{nameof(StreamSummary<TSrc, TDest>)}.Data_CollectionChanged: Unexpected collectionChanged {e.Action} action.");
            }

            // Start a task to summarize the data
            Task.Factory.StartNew(() =>
            {
                this.OnReceiveData(this.summarizer.Summarize(dataSource, this.interval));
            });
        }

        /// <summary>
        /// Returns a view over the summary data and ensure that the view is preserved in the cache.
        /// </summary>
        /// <param name="viewMode">The view mode.</param>
        /// <param name="startTime">Start stime of the view.</param>
        /// <param name="endTime">End time of the view.</param>
        /// <param name="tailCount">Number of items to include in view.</param>
        /// <param name="tailRange">Tail duration function.</param>
        /// <returns>The requested summary view.</returns>
        private ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView GetCachedSummaryView(
            ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView newView;
            var newViewKey = Tuple.Create(startTime, endTime, tailCount, tailRange);

            if (!this.cachedSummaryViews.TryGetValue(newViewKey, out newView))
            {
                // Create the requested view over the cached summary data.
                newView = this.summaryCache.GetView(viewMode, startTime, endTime, tailCount, tailRange);

                // Retain cached data by maintaining a table of summary views for which we want the data to be retained.
                // This is currently done for fixed mode views only, as the views are constantly being updated in live
                // mode, so it probably makes sense to just defer to the underlying cache to manage data retention.
                if (viewMode == ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView.ViewMode.Fixed)
                {
                    // List of cached views, ordered by (startTime, endTime)
                    var cachedViews = this.cachedSummaryViews.OrderBy(v => v.Key.Item1).ThenBy(v => v.Key.Item2).ToList();
                    foreach (var existingView in cachedViews)
                    {
                        // Terminate when we have passed the end time of the new view
                        if (existingView.Key.Item1 > endTime)
                        {
                            break;
                        }

                        // Skip views that are disjoint from the new view
                        if (existingView.Key.Item2 < startTime)
                        {
                            continue;
                        }

                        // Extend start time to include existing overlapping view
                        if (startTime > existingView.Key.Item1)
                        {
                            startTime = existingView.Key.Item1;
                        }

                        // Extend end time to include existing overlapping view
                        if (endTime < existingView.Key.Item2)
                        {
                            endTime = existingView.Key.Item2;
                        }

                        // Remove existing overlapping view which will be subsumed by the new range (startTime, endTime)
                        this.cachedSummaryViews.Remove(existingView.Key);
                    }

                    // Get a new view covering the expanded time range
                    var adjustedView = this.summaryCache.GetView(viewMode, startTime, endTime, tailCount, tailRange);

                    // Add it to the table of cached views to preserve the underlying data
                    var adjustedViewKey = Tuple.Create(startTime, endTime, tailCount, tailRange);
                    this.cachedSummaryViews.Add(adjustedViewKey, adjustedView);

                    // Prune the table of cached views to keep them within the cache limit
                    this.PurgeSummaryViews(newViewKey, newView);
                }
            }

            // Remove references to stream views that we no longer need
            this.PurgeStreamViews();

            return newView;
        }

        private void OnReceiveData(List<IntervalData<TDest>> items)
        {
            if (!this.IsCanceled && (items.Count > 0))
            {
                lock (this.bufferLock)
                {
                    // Find range whose last interval matches the first interval (head) of the range being added
                    int headMerge = this.summaryDataBuffer.FindIndex(list => this.itemComparer.Compare(list[list.Count - 1], items[0]) == 0);
                    if (headMerge != -1)
                    {
                        // Merge the tail of the predecesor with the head of the range being added
                        var predecessor = this.summaryDataBuffer[headMerge];

                        // Use summarizer-specific method to combine the two IntervalData values
                        items[0] = this.summarizer.Combine(predecessor[predecessor.Count - 1], items[0]);

                        // Remove the duplicate which has been merged with the head
                        predecessor.RemoveAt(predecessor.Count - 1);
                        if (predecessor.Count == 0)
                        {
                            // Remove empty list
                            this.summaryDataBuffer.RemoveAt(headMerge);
                        }
                    }

                    // Find range whose first interval matches the last interval (tail) of the range being added
                    int tailMerge = this.summaryDataBuffer.FindIndex(list => this.itemComparer.Compare(list[0], items[items.Count - 1]) == 0);
                    if (tailMerge != -1)
                    {
                        // Merge the head of the successor with the tail of the range being added
                        var successor = this.summaryDataBuffer[tailMerge];

                        // Use summarizer-specific method to combine the two IntervalData values
                        items[items.Count - 1] = this.summarizer.Combine(successor[0], items[items.Count - 1]);

                        // Remove the duplicate which has been merged with the tail
                        successor.RemoveAt(0);
                        if (successor.Count == 0)
                        {
                            // Remove empty list
                            this.summaryDataBuffer.RemoveAt(tailMerge);
                        }
                    }

                    // Insert list of new items in sorted order
                    int insert = this.summaryDataBuffer.FindIndex(list => list[0].OriginatingTime > items[0].OriginatingTime);
                    if (insert != -1)
                    {
                        this.summaryDataBuffer.Insert(insert, items);
                    }
                    else
                    {
                        this.summaryDataBuffer.Add(items);
                    }
                }
            }
        }

        /// <summary>
        /// Removes stale stream views which are no longer needed.
        /// </summary>
        private void PurgeStreamViews()
        {
            // Ensure that only live views are present in the summary cache and that data under any
            // removed dead views are pruned (unless also referenced by a live view). This ensures
            // that the cache state reflects only the live data and we can release any stream views
            // which do not overlap with any live summary view.
            this.summaryCache.PruneDeadViews();

            var unusedStreamViews = new List<KeyValuePair<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>, ObservableKeyedCache<DateTime, Message<TSrc>>.ObservableKeyedView>>();

            foreach (var streamView in this.activeStreamViews)
            {
                bool isLive = false;
                foreach (var viewExtent in this.summaryCache.ViewExtents)
                {
                    if ((streamView.Key.Item1 < viewExtent.Item2) && (streamView.Key.Item2 > viewExtent.Item1))
                    {
                        // If streamView overlaps with an existing summary view, assume it is live
                        isLive = true;
                        break;
                    }
                }

                if (!isLive)
                {
                    // Unsubscribe from view - if the view truly is unused and we hold the only reference to it,
                    // then this may be unnecessary as the view will be GC'd anyway. However, if there are other
                    // references to the view, then we don't want to continue receiving notifications for it.
                    streamView.Value.DetailedCollectionChanged -= this.Data_CollectionChanged;
                    unusedStreamViews.Add(streamView);
                }
            }

            // Remove the reference to unused views, potentially allowing the data within the view extent
            // to be purged from the stream cache if there are no other views over it.
            unusedStreamViews.ForEach(view => this.activeStreamViews.Remove(view.Key));
        }

        /// <summary>
        /// Purges the cache of summary views which do not include the specified protected range, but
        /// only if the total number of underlying data items across all the views exceeds the limit.
        /// </summary>
        /// <param name="protectedViewKey">The key associated with the protected view.</param>
        /// <param name="protectedView">The protected view.</param>
        private void PurgeSummaryViews(
            Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>> protectedViewKey,
            ObservableKeyedCache<DateTime, IntervalData<TDest>>.ObservableKeyedView protectedView)
        {
            // Check if we need to purge
            if (this.cachedSummaryViews.Values.Sum(v => v.Count) <= this.maxCacheSize)
            {
                return;
            }

            // List of cached views, ordered by (startTime, endTime)
            var startTime = protectedViewKey.Item1;
            var endTime = protectedViewKey.Item2;
            var cachedViews = this.cachedSummaryViews.OrderBy(v => v.Key.Item1).ThenBy(v => v.Key.Item2).ToList();
            foreach (var existingView in cachedViews)
            {
                // Skip overlapping views and preserve them
                if ((startTime < existingView.Key.Item2) && (endTime > existingView.Key.Item1))
                {
                    continue;
                }

                // Remove all disjoint views that do not touch the current view range
                this.cachedSummaryViews.Remove(existingView.Key);
            }

            // Check whether the cached views still exceed the limit. The above removal may not have
            // removed enough. If so, then simply clear the entire cache, leaving just the current
            // protected view.
            if (this.cachedSummaryViews.Values.Sum(v => v.Count) > this.maxCacheSize)
            {
                this.cachedSummaryViews.Clear();
                this.cachedSummaryViews.Add(protectedViewKey, protectedView);
            }
        }

        private void ReadStream(DateTime startTime, DateTime endTime)
        {
            var streamViewKey = Tuple.Create(startTime, endTime, 0u, (Func<DateTime, DateTime>)null);

            // Check to ensure we aren't already reading from the same view range.
            if (!this.activeStreamViews.ContainsKey(streamViewKey))
            {
                var streamView = DataManager.Instance.ReadStream<TSrc>(this.streamBinding, startTime, endTime);
                this.Data_CollectionChanged(streamView, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                streamView.DetailedCollectionChanged += this.Data_CollectionChanged;
                this.activeStreamViews.Add(streamViewKey, streamView);
            }
        }

        private void ReadStream(Func<DateTime, DateTime> tailRange)
        {
            var streamViewKey = Tuple.Create(DateTime.MinValue, DateTime.MaxValue, 0u, tailRange);

            // Check to ensure we aren't already reading from the same view range.
            if (!this.activeStreamViews.ContainsKey(streamViewKey))
            {
                var streamView = DataManager.Instance.ReadStream<TSrc>(this.streamBinding, tailRange);
                this.Data_CollectionChanged(streamView, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                streamView.DetailedCollectionChanged += this.Data_CollectionChanged;
                this.activeStreamViews.Add(streamViewKey, streamView);
            }
        }
    }
}
