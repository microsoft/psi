// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents the summarizations for a specific stream.
    /// </summary>
    public class StreamSummaryManager
    {
        private const uint DefaultCacheCapacity = 16384;
        private List<IStreamSummary> summaryCaches;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamSummaryManager"/> class.
        /// </summary>
        /// <param name="storeName">Store name to summarize.</param>
        /// <param name="storePath">Store path to summarize.</param>
        /// <param name="streamName">Stream Name to summarize.</param>
        /// <param name="streamAdapterType">Stream adapter type.</param>
        public StreamSummaryManager(string storeName, string storePath, string streamName, Type streamAdapterType)
        {
            this.StoreName = storeName;
            this.StorePath = storePath;
            this.StreamName = streamName;
            this.StreamAdapterType = streamAdapterType;
            this.summaryCaches = new List<IStreamSummary>();
        }

        /// <summary>
        /// Gets the store name to summarize.
        /// </summary>
        public string StoreName { get; private set; }

        /// <summary>
        /// Gets the store path to summarize.
        /// </summary>
        public string StorePath { get; private set; }

        /// <summary>
        /// Gets the stream name to summarize.
        /// </summary>
        public string StreamName { get; private set; }

        /// <summary>
        /// Gets the stream adapater type.
        /// </summary>
        public Type StreamAdapterType { get; private set; }

        /// <summary>
        /// Gets the maximum summary interval.
        /// </summary>
        public TimeSpan MaxSummaryInterval => (this.summaryCaches.Count > 0) ? this.summaryCaches.Max(s => s.Interval) : TimeSpan.Zero;

        /// <summary>
        /// Periodically called by the <see cref="DataManager"/> on the UI thread to give stream summary managers time dispatch data from internal buffers to views.
        /// </summary>
        public void DispatchData()
        {
            this.summaryCaches.ForEach(s => s.DispatchData());
        }

        /// <summary>
        /// Finds the time of the next data point after the point indicated by the given time.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="time">Time of current data point.</param>
        /// <param name="interval">The time interval each summary value covers.</param>
        /// <returns>Time of the next data point.</returns>
        public DateTime FindNextDataPoint<T>(StreamBinding streamBinding, DateTime time, TimeSpan interval)
        {
            var searchInterval = interval;
            while (searchInterval <= this.MaxSummaryInterval)
            {
                var cache = this.GetSummaryCache(streamBinding, searchInterval, false);
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
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="time">Time of current data point.</param>
        /// <param name="interval">The time interval each summary value covers.</param>
        /// <returns>Time of the previous data point.</returns>
        public DateTime FindPreviousDataPoint<T>(StreamBinding streamBinding, DateTime time, TimeSpan interval)
        {
            var searchInterval = interval;
            while (searchInterval <= this.MaxSummaryInterval)
            {
                var cache = this.GetSummaryCache(streamBinding, searchInterval, false);
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

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="viewMode">The view mode, which may be either fixed or live data.</param>
        /// <param name="startTime">The start time of the view range.</param>
        /// <param name="endTime">The end time of the view range.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="tailCount">Not yet supported and should be set to zero.</param>
        /// <param name="tailRange">Tail duration function. Computes the view range start time given an end time. Applies to live view mode only.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(
            StreamBinding streamBinding,
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
                startTime = this.FindPreviousDataPoint<T>(streamBinding, startTime, interval);
            }

            if (endTime < DateTime.MaxValue)
            {
                // Extend the start time to include the next data point to facilitate continuous plots.
                endTime = this.FindNextDataPoint<T>(streamBinding, endTime, interval);
            }

            return this.GetSummaryCache(streamBinding, interval).ReadSummary<T>(viewMode, startTime, endTime, tailCount, tailRange);
        }

        private IStreamSummary GetSummaryCache(StreamBinding streamBinding, TimeSpan interval, bool create = true)
        {
            var summaryCache = this.summaryCaches.Find(s =>
                (s.Interval == interval) &&
                (s.SummarizerType == streamBinding.SummarizerType) &&
                StructuralComparisons.StructuralEqualityComparer.Equals(s.Parameters, streamBinding.SummarizerArgs));

            if ((summaryCache == null) && create)
            {
                summaryCache = typeof(StreamSummary<,>)
                    .MakeGenericType(streamBinding.Summarizer.SourceType, streamBinding.Summarizer.DestinationType)
                    .GetConstructor(new Type[] { typeof(StreamBinding), typeof(TimeSpan), typeof(uint) })
                    .Invoke(new object[] { streamBinding, interval, DefaultCacheCapacity }) as IStreamSummary;

                if (summaryCache == null)
                {
                    throw new InvalidOperationException("Unable to create instance of summary cache");
                }

                this.summaryCaches.Add(summaryCache);
            }

            return summaryCache;
        }
    }
}