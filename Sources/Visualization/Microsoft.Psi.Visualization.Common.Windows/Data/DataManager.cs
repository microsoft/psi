// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Threading;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Provides cached-controlled read access to data stores used by the visualization runtime.
    /// Manages underlying sets of <see cref="DataStoreReader"/>s and <see cref="StreamSummaryManager"/>s.
    /// </summary>
    public class DataManager : IDisposable
    {
        /// <summary>
        /// The singleton instance of the <see cref="DataManager"/>.
        /// </summary>
        public static readonly DataManager Instance = new DataManager();

        private DataStoreReaders dataStoreReaders = new DataStoreReaders();
        private StreamSummaryManagers streamSummaryManagers = new StreamSummaryManagers();
        private DispatcherTimer dataDispatchTimer;
        private bool disposed;

        private DataManager()
        {
            this.disposed = false;
            this.dataDispatchTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(1.0 / 30.0), DispatcherPriority.Background, (s, e) => this.DoBatchProcessing(), Dispatcher.CurrentDispatcher);
            this.dataDispatchTimer.Start();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DataManager"/> class.
        /// </summary>
        ~DataManager()
        {
            this.Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Gets the simple reader for the specified stream binding.
        /// </summary>
        /// <param name="streamBinding">The stream binding.</param>
        /// <returns>The simple reader.</returns>
        public ISimpleReader GetReader(StreamBinding streamBinding)
        {
            return this.FindDataStoreReader(streamBinding, false)?.GetReader();
        }

        /// <summary>
        /// Reads a single message from a stream identified by a stream binding and an index entry.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="indexEntry">The index entry indicating which message to read.</param>
        /// <returns>The message that was read.</returns>
        public T Read<T>(StreamBinding streamBinding, IndexEntry indexEntry)
        {
            DataStoreReader dataStoreReader = this.FindDataStoreReader(streamBinding, false);
            if (dataStoreReader == null)
            {
                throw new ArgumentOutOfRangeException(nameof(streamBinding), "Stream binding must reference an open data store.");
            }

            return dataStoreReader.Read<T>(streamBinding, indexEntry);
        }

        /// <summary>
        /// Creates a view of the indices identified by the matching start and end times and asychronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="startTime">Start time of indices to read.</param>
        /// <param name="endTime">End time of indices to read.</param>
        /// <returns>Observable view of indices.</returns>
        public ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView ReadIndex<T>(StreamBinding streamBinding, DateTime startTime, DateTime endTime)
        {
            if (endTime < startTime)
            {
                throw new ArgumentException("End time must be greater than or equal to start time.", nameof(endTime));
            }

            var dataStoreReader = this.FindDataStoreReader(streamBinding, true);
            return dataStoreReader.ReadIndex<T>(streamBinding, startTime, endTime);
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching start and end times and asychronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(StreamBinding streamBinding, DateTime startTime, DateTime endTime)
        {
            if (endTime < startTime)
            {
                throw new ArgumentException("End time must be greater than or equal to start time.", nameof(endTime));
            }

            var dataStoreReader = this.FindDataStoreReader(streamBinding, true);
            return dataStoreReader.ReadStream(streamBinding, ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode.Fixed, startTime, endTime, 0, null);
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching tail count and asychronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(StreamBinding streamBinding, uint tailCount)
        {
            if (tailCount == 0)
            {
                throw new ArgumentException("Tail count must be greater than 0", nameof(tailCount));
            }

            var dataStoreReader = this.FindDataStoreReader(streamBinding, true);
            return dataStoreReader.ReadStream(streamBinding, ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode.TailCount, DateTime.MinValue, DateTime.MaxValue, tailCount, null);
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching tail range and asychronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(StreamBinding streamBinding, Func<DateTime, DateTime> tailRange)
        {
            if (tailRange == null)
            {
                throw new ArgumentNullException(nameof(tailRange));
            }

            var dataStoreReader = this.FindDataStoreReader(streamBinding, true);
            return dataStoreReader.ReadStream(streamBinding, ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode.TailRange, DateTime.MinValue, DateTime.MaxValue, 0, tailRange);
        }

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="startTime">The start time of the view range.</param>
        /// <param name="endTime">The end time of the view range.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(StreamBinding streamBinding, DateTime startTime, DateTime endTime, TimeSpan interval)
        {
            var viewMode = ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode.Fixed;
            return this.FindStreamSummaryManager(streamBinding).ReadSummary(streamBinding, viewMode, startTime, endTime, interval, 0, null);
        }

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="tailCount">Number of items to include in view.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(StreamBinding streamBinding, TimeSpan interval, uint tailCount)
        {
            var viewMode = ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode.TailCount;
            return this.FindStreamSummaryManager(streamBinding).ReadSummary(streamBinding, viewMode, DateTime.MinValue, DateTime.MaxValue, interval, tailCount, null);
        }

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="tailRange">Tail duration function. Computes the view range start time given an end time. Applies to live view mode only.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(StreamBinding streamBinding, TimeSpan interval, Func<DateTime, DateTime> tailRange)
        {
            var viewMode = ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode.TailRange;
            return this.FindStreamSummaryManager(streamBinding).ReadSummary(streamBinding, viewMode, DateTime.MinValue, DateTime.MaxValue, interval, 0, tailRange);
        }

        /// <summary>
        /// Disposes of an instance of the <see cref="DataManager"/> class.
        /// </summary>
        /// <param name="disposing">Indicates wheter the method call comes from a Dispose method (its value is true) or from its destructor (its value is false).</param>
        private void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                // Free any managed objects here.
            }

            // Free any unmanaged objects here.
            foreach (var dataStoreReader in this.dataStoreReaders)
            {
                dataStoreReader.Dispose();
            }

            this.dataStoreReaders = null;
            this.disposed = true;
            GC.SuppressFinalize(this);
        }

        private void DoBatchProcessing()
        {
            lock (this.dataStoreReaders)
            {
                // Iterate over a copy of the collection as it may be modified by a
                // viz object while iterating. Same goes for the streamSummaryManagers below.
                foreach (var dataStoreReader in this.dataStoreReaders.ToList())
                {
                    // run data store readers
                    dataStoreReader.Run();

                    // dispatch data for data stream readers
                    dataStoreReader.DispatchData();
                }
            }

            lock (this.streamSummaryManagers)
            {
                foreach (var streamSummaryManager in this.streamSummaryManagers.ToList())
                {
                    // dispatch data for stream summary managers
                    streamSummaryManager.DispatchData();
                }
            }
        }

        private DataStoreReader FindDataStoreReader(StreamBinding streamBinding, bool createDataStoreReader = true)
        {
            if (streamBinding == null)
            {
                throw new ArgumentNullException(nameof(streamBinding));
            }

            lock (this.dataStoreReaders)
            {
                DataStoreReader dataStoreReader = new DataStoreReader(streamBinding.StoreName, streamBinding.StorePath, streamBinding.SimpleReaderType);
                var key = Tuple.Create(dataStoreReader.StoreName, dataStoreReader.StorePath);

                if (this.dataStoreReaders.Contains(key))
                {
                    // dispose existing dataStoreReader before re-assignment
                    dataStoreReader.Dispose();
                    dataStoreReader = this.dataStoreReaders[key];
                }
                else if (createDataStoreReader)
                {
                    this.dataStoreReaders.Add(dataStoreReader);
                }
                else
                {
                    // dispose existing dataStoreReader before re-assignment
                    dataStoreReader.Dispose();
                    dataStoreReader = null;
                }

                return dataStoreReader;
            }
        }

        private StreamSummaryManager FindStreamSummaryManager(StreamBinding streamBinding, bool createStreamSummaryManager = true)
        {
            if (streamBinding == null)
            {
                throw new ArgumentNullException(nameof(streamBinding));
            }

            lock (this.streamSummaryManagers)
            {
                var key = Tuple.Create(streamBinding.StoreName, streamBinding.StorePath, streamBinding.StreamName, streamBinding.StreamAdapterType);
                StreamSummaryManager streamSummaryManager = default(StreamSummaryManager);

                if (this.streamSummaryManagers.Contains(key))
                {
                    streamSummaryManager = this.streamSummaryManagers[key];
                }
                else if (createStreamSummaryManager)
                {
                    streamSummaryManager = new StreamSummaryManager(
                        streamBinding.StoreName,
                        streamBinding.StorePath,
                        streamBinding.StreamName,
                        streamBinding.StreamAdapterType);

                    this.streamSummaryManagers.Add(streamSummaryManager);
                }

                return streamSummaryManager;
            }
        }

        private class DataStoreReaders : KeyedCollection<Tuple<string, string>, DataStoreReader>
        {
            protected override Tuple<string, string> GetKeyForItem(DataStoreReader dataStoreReader)
            {
                return Tuple.Create(dataStoreReader.StoreName, dataStoreReader.StorePath);
            }
        }

        private class StreamSummaryManagers : KeyedCollection<Tuple<string, string, string, Type>, StreamSummaryManager>
        {
            protected override Tuple<string, string, string, Type> GetKeyForItem(StreamSummaryManager streamSummaryManager)
            {
                return Tuple.Create(streamSummaryManager.StoreName, streamSummaryManager.StorePath, streamSummaryManager.StreamName, streamSummaryManager.StreamAdapterType);
            }
        }
    }
}