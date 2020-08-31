// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents a collection of stream summary caches, each at a different zoom level, for a single stream in a store.
    /// </summary>
    public class StreamSummaryManager : IDisposable
    {
        private const uint DefaultCacheCapacity = 16384;
        private Dictionary<TimeSpan, IStreamSummary> summaryCaches;
        private List<Guid> consumers;
        private StreamSource streamSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamSummaryManager"/> class.
        /// </summary>
        /// <param name="streamSource">A stream source that specifies the store and stream that will be summarized.</param>
        public StreamSummaryManager(StreamSource streamSource)
        {
            if (streamSource == null)
            {
                throw new ArgumentNullException(nameof(streamSource));
            }

            this.streamSource = streamSource;
            this.summaryCaches = new Dictionary<TimeSpan, IStreamSummary>();
            this.consumers = new List<Guid>();
        }

        /// <summary>
        /// Occurs when the stream summary manager has no remaining consumers and can be disposed of.
        /// </summary>
        public event EventHandler NoRemainingConsumers;

        /// <summary>
        /// Gets the store name to summarize.
        /// </summary>
        public string StoreName => this.streamSource.StoreName;

        /// <summary>
        /// Gets the store path to summarize.
        /// </summary>
        public string StorePath => this.streamSource.StorePath;

        /// <summary>
        /// Gets the stream name to summarize.
        /// </summary>
        public string StreamName => this.streamSource.StreamName;

        /// <summary>
        /// Gets the stream adapter type.
        /// </summary>
        public IStreamAdapter StreamAdapter => this.streamSource.StreamAdapter;

        /// <summary>
        /// Gets the number of consumers of this stream summary manager.
        /// </summary>
        public int ConsumerCount => this.consumers.Count;

        /// <summary>
        /// Gets the maximum summary interval.
        /// </summary>
        protected TimeSpan MaxSummaryInterval => (this.summaryCaches.Count > 0) ? this.summaryCaches.Values.Max(s => s.Interval) : TimeSpan.Zero;

        /// <summary>
        /// Registers a new consumer of the stream summary manager.
        /// </summary>
        /// <returns>A unique consumer id that must provided by the consumer when it unregisters from this stream summary manager.</returns>
        public Guid RegisterConsumer()
        {
            Guid consumerId = Guid.NewGuid();

            lock (this.consumers)
            {
                this.consumers.Add(consumerId);
            }

            return consumerId;
        }

        /// <summary>
        /// Unregisters a consumer from the stream summary manager.
        /// </summary>
        /// <param name="consumerId">The consumer id that was returned to the consumer when it registered.</param>
        public void UnregisterConsumer(Guid consumerId)
        {
            lock (this.consumers)
            {
                if (this.consumers.Contains(consumerId))
                {
                    this.consumers.Remove(consumerId);
                }
                else
                {
                    throw new InvalidOperationException("An attempt was made to unregister from a stream summary manager consumer using an invalid consumer id.");
                }
            }

            // If there's no remaining consumers, raise an event to allow DataManager to remove this summary manager from its collection.
            if (!this.consumers.Any())
            {
                this.NoRemainingConsumers?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Determines whether a stream summary manager provides data for a consumer.
        /// </summary>
        /// <param name="consumerId">The id of the consumer.</param>
        /// <returns>True if the stream summary manager contains the consumer, otherwise false.</returns>
        public bool ContainsConsumer(Guid consumerId)
        {
            return this.consumers.Contains(consumerId);
        }

        /// <summary>
        /// Periodically called by the <see cref="DataManager"/> on the UI thread to give stream summary managers time dispatch data from internal buffers to views.
        /// </summary>
        public void DispatchData()
        {
            lock (this.summaryCaches)
            {
                foreach (IStreamSummary streamSummary in this.summaryCaches.Values)
                {
                    streamSummary.DispatchData();
                }
            }
        }

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="viewMode">The view mode, which may be either fixed or live data.</param>
        /// <param name="startTime">The start time of the view range.</param>
        /// <param name="endTime">The end time of the view range.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="tailCount">Not yet supported and should be set to zero.</param>
        /// <param name="tailRange">Tail duration function. Computes the view range start time given an end time. Applies to live view mode only.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(
            StreamSource streamSource,
            ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            TimeSpan interval,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            if (startTime > DateTime.MinValue)
            {
                // Extend the start time to include the preceding data point to facilitate continuous plots.
                startTime = this.FindPreviousDataPoint<T>(startTime, interval);
            }

            if (endTime < DateTime.MaxValue)
            {
                // Extend the start time to include the next data point to facilitate continuous plots.
                endTime = this.FindNextDataPoint<T>(endTime, interval);
            }

            return this.GetOrCreateSummaryCache(streamSource, interval).ReadSummary<T>(viewMode, startTime, endTime, tailCount, tailRange);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (this.summaryCaches)
            {
                // Cancel any pending operations on all stream summaries.
                foreach (IStreamSummary summaryCache in this.summaryCaches.Values)
                {
                    summaryCache.Cancel();
                    summaryCache.Dispose();
                }

                this.summaryCaches.Clear();
            }
        }

        /// <summary>
        /// Finds the time of the next data point after the point indicated by the given time.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="time">Time of current data point.</param>
        /// <param name="interval">The time interval each summary value covers.</param>
        /// <returns>Time of the next data point.</returns>
        private DateTime FindNextDataPoint<T>(DateTime time, TimeSpan interval)
        {
            var searchInterval = interval;
            while (searchInterval <= this.MaxSummaryInterval)
            {
                var cache = this.GetSummaryCache(searchInterval);
                if (cache != null)
                {
                    var intervalData = cache.Search<T>(time, StreamSummarySearchMode.Next);
                    var adjustedTime = (intervalData.OriginatingTime >= time) ? intervalData.OriginatingTime : intervalData.EndTime;
                    if (adjustedTime != DateTime.MinValue)
                    {
                        return adjustedTime + interval;
                    }
                }

                searchInterval = TimeSpan.FromTicks(Math.Max(1, searchInterval.Ticks * 2));
            }

            return time;
        }

        /// <summary>
        /// Finds the time of the previous data point before the point indicated by the given time.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="time">Time of current data point.</param>
        /// <param name="interval">The time interval each summary value covers.</param>
        /// <returns>Time of the previous data point.</returns>
        private DateTime FindPreviousDataPoint<T>(DateTime time, TimeSpan interval)
        {
            var searchInterval = interval;
            while (searchInterval <= this.MaxSummaryInterval)
            {
                var cache = this.GetSummaryCache(searchInterval);
                if (cache != null)
                {
                    var intervalData = cache.Search<T>(time, StreamSummarySearchMode.Previous);
                    var adjustedTime = (intervalData.EndTime <= time) ? intervalData.EndTime : intervalData.OriginatingTime;
                    if (adjustedTime != DateTime.MinValue)
                    {
                        return adjustedTime - interval;
                    }
                }

                searchInterval = TimeSpan.FromTicks(Math.Max(1, searchInterval.Ticks * 2));
            }

            return time;
        }

        private IStreamSummary GetSummaryCache(TimeSpan interval)
        {
            if (this.summaryCaches.ContainsKey(interval))
            {
                return this.summaryCaches[interval];
            }

            return null;
        }

        private IStreamSummary GetOrCreateSummaryCache(StreamSource streamSource, TimeSpan interval)
        {
            // Get the summary cache if it exists.
            IStreamSummary summaryCache = this.GetSummaryCache(interval);

            // A summary cache with the required interval does not exist, so create it now.
            if (summaryCache == null)
            {
                summaryCache = typeof(StreamSummary<,>)
                    .MakeGenericType(streamSource.Summarizer.SourceType, streamSource.Summarizer.DestinationType)
                    .GetConstructor(new Type[] { typeof(StreamSource), typeof(TimeSpan), typeof(uint) })
                    .Invoke(new object[] { streamSource, interval, DefaultCacheCapacity }) as IStreamSummary;

                if (summaryCache == null)
                {
                    throw new InvalidOperationException("Unable to create instance of summary cache");
                }

                this.summaryCaches[interval] = summaryCache;
            }

            return summaryCache;
        }
    }
}