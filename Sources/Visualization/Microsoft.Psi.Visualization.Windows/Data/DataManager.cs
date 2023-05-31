// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Provides read access to data stores used by the visualization system.
    /// Manages underlying sets of <see cref="DataStoreReader"/>s and <see cref="StreamSummaryManager"/>s.
    /// </summary>
    public class DataManager : IDisposable
    {
        /// <summary>
        /// The singleton instance of the <see cref="DataManager"/>.
        /// </summary>
        public static readonly DataManager Instance = new ();

        /// <summary>
        /// The lock used to control access to readAndPublishStreamValueDateTime.
        /// </summary>
        private readonly object readAndPublishStreamValueDateTimeLock = new ();

        private readonly DispatcherTimer dataDispatchTimer;

        /// <summary>
        /// The collection of data store readers.  The keys are a tuple of store name and store path.
        /// </summary>
        private Dictionary<(string storeName, string storePath), DataStoreReader> dataStoreReaders = new ();

        /// <summary>
        /// The task that identifies the current operation for reading and publishing stream values,
        /// or null if no such operation is currently in progress.
        /// </summary>
        private Task readAndPublishStreamValueTask = null;

        /// <summary>
        /// The time to be used for the next read and publish of stream values after the
        /// current read-and-publish operation finishes, or null if there are no
        /// further read-and-publish requests outstanding.
        /// </summary>
        private DateTime? readAndPublishStreamValueDateTime;

        private bool disposed;

        private DataManager()
        {
            this.disposed = false;
            this.readAndPublishStreamValueDateTime = null;
            this.dataDispatchTimer = new DispatcherTimer(
                TimeSpan.FromSeconds(1.0 / 30.0),
                DispatcherPriority.Background,
                (s, e) => this.DoBatchProcessing(),
                Dispatcher.CurrentDispatcher);
            this.dataDispatchTimer.Start();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DataManager"/> class.
        /// </summary>
        ~DataManager()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Event that fires when a data store's dirty/clean status has changed.
        /// </summary>
        public event EventHandler<DataStoreStatusChangedEventArgs> DataStoreStatusChanged;

        /// <summary>
        /// Event that fires when a stream is unable to be read from.
        /// </summary>
        public event EventHandler<StreamReadErrorEventArgs> StreamReadError;

        /// <inheritdoc />
        public void Dispose() => this.Dispose(true);

        /// <summary>
        /// Registers a stream value subscriber.
        /// </summary>
        /// <typeparam name="TData">The type of data expected by the stream value subscriber.</typeparam>
        /// <param name="streamSource">Information about the stream source and the required stream adapter.</param>
        /// <param name="epsilonInterval">The epsilon interval to use when retrieving stream values.</param>
        /// <param name="target">The target method to call when new data is available.</param>
        /// <param name="cacheInterval">The time interval to cache.</param>
        /// <returns>A subscriber id that can be used to unregister the stream value subscriber.</returns>
        /// <remarks>
        /// The target action is called when new data becomes available. The data object passed is only
        /// valid for the duration of the target action; as a result, if the stream value subscriber needs
        /// to hold on to this object past the endpoint of the target method, it must make a copy of it.
        /// </remarks>
        public Guid RegisterStreamValueSubscriber<TData>(
            StreamSource streamSource,
            RelativeTimeInterval epsilonInterval,
            Action<bool, TData, DateTime, DateTime> target,
            TimeInterval cacheInterval)
        {
            // Get the appropriate data store reader
            var dataStoreReader = this.GetOrCreateDataStoreReader(streamSource);

            // Construct the typed method for getting or creating the stream value provider
            var method = typeof(DataStoreReader).GetMethod(nameof(DataStoreReader.GetOrCreateStreamValueProvider), BindingFlags.NonPublic | BindingFlags.Instance);
            var streamValueProviderType = streamSource.StreamAdapter != null ? streamSource.StreamAdapter.SourceType : typeof(TData);
            var genericMethod = method.MakeGenericMethod(streamValueProviderType);

            // Construct the stream value provider
            var streamValueProvider = (IStreamValueProvider)genericMethod.Invoke(dataStoreReader, new object[] { streamSource });

            // Register the stream value subscriber
            var subscriberId = streamValueProvider.RegisterStreamValueSubscriber(streamSource.StreamAdapter, epsilonInterval, target);

            // Set the cache interval
            streamValueProvider.SetCacheInterval(cacheInterval);

            return subscriberId;
        }

        /// <summary>
        /// Unregisters a stream value subscriber.
        /// </summary>
        /// <typeparam name="TData">The type of data expected by the stream value subscriber.</typeparam>
        /// <param name="subscriberId">The subscriber id that the subscriber was assigned when it was initially registered.</param>
        public void UnregisterStreamValueSubscriber<TData>(Guid subscriberId) =>
            this.GetDataStoreReaders()
                .ForEach(dataStoreReader => dataStoreReader.UnregisterStreamValueSubscriber<TData>(subscriberId));

        /// <summary>
        /// Reads and publishes a stream value to all stream value subscribers based on the
        /// specified date-time. This method is only called by the UI thread.
        /// </summary>
        /// <param name="dateTime">The time for the values to be read.</param>
        public void ReadAndPublishStreamValue(DateTime dateTime)
        {
            // Update the time where the next read should occur
            lock (this.readAndPublishStreamValueDateTimeLock)
            {
                this.readAndPublishStreamValueDateTime = dateTime;

                // If the read and publish task is not running, start it.
                if ((this.readAndPublishStreamValueTask == null) || this.readAndPublishStreamValueTask.IsCompleted)
                {
                    this.readAndPublishStreamValueTask = Task.Run(this.ReadAndPublishStreamValueTask);
                }
            }
        }

        /// <summary>
        /// Sets the cache interval for stream value providers.
        /// </summary>
        /// <param name="timeInterval">The new cache interval for stream value providers.</param>
        public void SetStreamValueProvidersCacheInterval(TimeInterval timeInterval) =>
            this.GetDataStoreReaders()
                .ForEach(dataStoreReader => dataStoreReader.SetStreamValueProvidersCacheInterval(timeInterval));

        /// <summary>
        /// Checks if a stream is known to be unreadable.
        /// </summary>
        /// <param name="streamSource">The stream source indicating which stream to check.</param>
        /// <returns>True if the stream is currently considered readable, otherwise false.</returns>
        public bool IsUnreadable(StreamSource streamSource) =>
            this.GetOrCreateDataStoreReader(streamSource)
                .StreamIsUnreadable(streamSource.StreamName);

        /// <summary>
        /// Creates and asynchronously fills in a view of the messages for a specified stream source and time interval.
        /// </summary>
        /// <typeparam name="T">The type of the data in the stream.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(
            StreamSource streamSource,
            DateTime startTime,
            DateTime endTime) =>
            this.GetOrCreateStreamIntervalProvider<T>(streamSource)
                .ReadStream<T>(ObservableKeyedViewMode.Fixed, startTime, endTime, 0, null);

        /// <summary>
        /// Creates a view of the messages identified by the matching tail count and asynchronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(
            StreamSource streamSource,
            uint tailCount) =>
            this.GetOrCreateStreamIntervalProvider<T>(streamSource)
                .ReadStream<T>(ObservableKeyedViewMode.TailCount, DateTime.MinValue, DateTime.MaxValue, tailCount, null);

        /// <summary>
        /// Creates a view of the messages identified by the matching tail range and asynchronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(
            StreamSource streamSource,
            Func<DateTime, DateTime> tailRange) =>
            this.GetOrCreateStreamIntervalProvider<T>(streamSource)
                .ReadStream<T>(ObservableKeyedViewMode.TailRange, DateTime.MinValue, DateTime.MaxValue, 0, tailRange);

        /// <summary>
        /// Registers a subscriber of stream interval data.
        /// </summary>
        /// <typeparam name="T">The message data type.</typeparam>
        /// <param name="streamSource">A stream source that indicates the store and stream data that the client consumes.</param>
        /// <returns>A unique subscriber id that should be provided when the subscriber unregisters.</returns>
        public Guid RegisterStreamIntervalSubscriber<T>(StreamSource streamSource) =>
            this.GetOrCreateStreamIntervalProvider<T>(streamSource)
                .RegisterStreamIntervalSubscriber(streamSource);

        /// <summary>
        /// Unregisters a subscriber of stream interval data.
        /// </summary>
        /// <param name="subscriberId">The id that was returned to the subscriber when it registered as a stream interval subscriber.</param>
        public void UnregisterStreamIntervalSubscriber(Guid subscriberId) =>
            this.GetDataStoreReaders()
                .ForEach(dsr => dsr.UnregisterStreamIntervalSubscriber(subscriberId));

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="startTime">The start time of the view range.</param>
        /// <param name="endTime">The end time of the view range.</param>
        /// <param name="summaryInterval">The time interval each summary value should cover.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(
            StreamSource streamSource,
            DateTime startTime,
            DateTime endTime,
            TimeSpan summaryInterval) =>
            this.GetOrCreateStreamIntervalProvider<T>(streamSource)
                .ReadSummary<T>(streamSource, ObservableKeyedViewMode.Fixed, startTime, endTime, summaryInterval, 0, null);

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="summaryInterval">The time interval each summary value should cover.</param>
        /// <param name="tailCount">Number of items to include in view.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(
            StreamSource streamSource,
            TimeSpan summaryInterval,
            uint tailCount) =>
            this.GetOrCreateStreamIntervalProvider<T>(streamSource)
                .ReadSummary<T>(streamSource, ObservableKeyedViewMode.TailCount, DateTime.MinValue, DateTime.MaxValue, summaryInterval, tailCount, null);

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="summaryInterval">The time interval each summary value should cover.</param>
        /// <param name="tailRange">Tail duration function. Computes the view range start time given an end time. Applies to live view mode only.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(
            StreamSource streamSource,
            TimeSpan summaryInterval,
            Func<DateTime, DateTime> tailRange) =>
            this.GetOrCreateStreamIntervalProvider<T>(streamSource)
                .ReadSummary<T>(streamSource, ObservableKeyedViewMode.TailRange, DateTime.MinValue, DateTime.MaxValue, summaryInterval, 0, tailRange);

        /// <summary>
        /// Gets the supplemental metadata for a stream specified by a stream source.
        /// </summary>
        /// <typeparam name="TSupplementalMetadata">The type of the supplemental metadata.</typeparam>
        /// <param name="streamSource">The stream source.</param>
        /// <returns>The supplemental metadata for the stream.</returns>
        public TSupplementalMetadata GetSupplementalMetadata<TSupplementalMetadata>(StreamSource streamSource) =>
            this.GetOrCreateDataStoreReader(streamSource)
                .GetSupplementalMetadata<TSupplementalMetadata>(streamSource.StreamName);

        /// <summary>
        /// Gets the supplemental metadata for a specified stream.
        /// </summary>
        /// <typeparam name="TSupplementalMetadata">The type of the supplemental metadata.</typeparam>
        /// <param name="storeName">The store name.</param>
        /// <param name="storePath">The store path.</param>
        /// <param name="streamReaderType">The type of stream reader.</param>
        /// <param name="streamName">The stream name.</param>
        /// <returns>The supplemental metadata for the stream.</returns>
        public TSupplementalMetadata GetSupplementalMetadataByName<TSupplementalMetadata>(string storeName, string storePath, Type streamReaderType, string streamName) =>
            this.GetOrCreateDataStoreReader(storeName, storePath, streamReaderType)
                .GetSupplementalMetadata<TSupplementalMetadata>(streamName);

        /// <summary>
        /// Performs a series of updates to the messages in a stream.  Stream bindings that use
        /// either a stream adapter or a stream summarizer are not permitted to update a stream.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="updates">A collection of updates to perform.</param>
        public void UpdateStream<T>(StreamSource streamSource, IEnumerable<StreamUpdate<T>> updates)
        {
            if ((streamSource.StreamAdapter != null) || (streamSource.Summarizer != null))
            {
                throw new InvalidOperationException("StreamSources that use either a StreamAdapter or a Summarizer are not permitted to update streams.");
            }

            // Call update stream on the datastore reader
            this.GetDataStoreReaderOrDefault(streamSource.StoreName, streamSource.StorePath)
                .UpdateStream(streamSource, updates);

            // Notify listeners that the store and stream are now dirty
            this.DataStoreStatusChanged?.Invoke(this, new DataStoreStatusChangedEventArgs(streamSource.StoreName, true, streamSource.StreamName));
        }

        /// <summary>
        /// Saves all changes to a store to disk.
        /// </summary>
        /// <param name="storeName">The name of the store to save.</param>
        /// <param name="storePath">The path to the store to save.</param>
        /// <param name="progress">A progress interface to report back to.</param>
        public void SaveStore(string storeName, string storePath, IProgress<double> progress)
        {
            var dataStoreReader = this.GetDataStoreReaderOrDefault(storeName, storePath);
            string[] savedStreams = dataStoreReader.SaveChanges(progress);

            // Notify listeners that the store is now clean
            this.DataStoreStatusChanged?.Invoke(this, new DataStoreStatusChangedEventArgs(storeName, false, savedStreams));
        }

        /// <summary>
        /// Gets the time of the nearest message to a specified time, on a specified stream.
        /// </summary>
        /// <param name="streamSource">The stream source specifying the stream of interest.</param>
        /// <param name="time">The time to find the nearest message to.</param>
        /// <param name="nearestType">The type of nearest message to find.</param>
        /// <returns>The time of the nearest message, if one is found or null otherwise.</returns>
        public DateTime? GetTimeOfNearestMessage(StreamSource streamSource, DateTime time, NearestType nearestType)
        {
            return streamSource != null ? this.GetOrCreateDataStoreReader(streamSource)
                .GetTimeOfNearestMessage(streamSource, time, nearestType) : null;
        }

        /// <summary>
        /// Runs a task to read and publish stream values.
        /// </summary>
        private void ReadAndPublishStreamValueTask()
        {
            DateTime? dateTime;

            while (true)
            {
                // Get the cursor time for the next read request and then null it out
                lock (this.readAndPublishStreamValueDateTimeLock)
                {
                    dateTime = this.readAndPublishStreamValueDateTime;
                    this.readAndPublishStreamValueDateTime = null;
                }

                // If there's a cursor time, initiate a read and publish values request on
                // each data store reader, otherwise we're done.
                if (dateTime.HasValue)
                {
                    try
                    {
                        Parallel.ForEach(
                            this.GetDataStoreReaders(),
                            dataStoreReader => dataStoreReader.ReadAndPublishStreamValues(dateTime.Value));
                    }
                    catch (Exception ex)
                    {
                        string GetMessageTrace(Exception ex, string message)
                        {
                            return
                                ex != null
                                ? GetMessageTrace(ex.InnerException, message + Environment.NewLine + ex.Message)
                                : message;
                        }

                        Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            new MessageBoxWindow(
                                Application.Current.MainWindow,
                                "Error",
                                GetMessageTrace(ex, $"An error occurred while attempting to read stream values.{Environment.NewLine}"),
                                "Close",
                                null).ShowDialog();
                        }));
                    }
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Disposes of an instance of the <see cref="DataManager"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether the method call comes from a Dispose method (its value is true) or from its destructor (its value is false).</param>
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
            foreach (var dataStoreReader in this.dataStoreReaders.Values)
            {
                dataStoreReader.Dispose();
            }

            this.dataStoreReaders = null;
            this.disposed = true;
            GC.SuppressFinalize(this);
        }

        private void DoBatchProcessing()
        {
            // Iterate over a copy of the collection as it may be modified by a
            // viz object while iterating. Same goes for the streamSummaryManagers below.
            this.GetDataStoreReaders()
                .ForEach(dataStoreReader =>
                {
                    // run data store readers
                    dataStoreReader.Run();

                    // dispatch data for data stream readers
                    dataStoreReader.DispatchData();
                });
        }

        private List<DataStoreReader> GetDataStoreReaders()
        {
            // Locks the data store reader collection and then extracts the list.
            lock (this.dataStoreReaders)
            {
                return this.dataStoreReaders.Values.ToList();
            }
        }

        private DataStoreReader GetOrCreateDataStoreReader(StreamSource streamSource) =>
            this.GetOrCreateDataStoreReader(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType);

        private DataStoreReader GetOrCreateDataStoreReader(string storeName, string storePath, Type streamReaderType)
        {
            if (string.IsNullOrWhiteSpace(storeName))
            {
                throw new ArgumentNullException(nameof(storeName));
            }

            if (string.IsNullOrWhiteSpace(storePath))
            {
                throw new ArgumentNullException(nameof(storePath));
            }

            var dataStoreReader = this.GetDataStoreReaderOrDefault(storeName, storePath);

            if (dataStoreReader == null)
            {
                lock (this.dataStoreReaders)
                {
                    dataStoreReader = new DataStoreReader(storeName, storePath, streamReaderType);
                    dataStoreReader.NoRemainingSubscribers += this.DataStoreReader_NoRemainingSubscribers;
                    dataStoreReader.StreamReadError += this.DataStoreReader_StreamReadError;
                    this.dataStoreReaders.Add((storeName, storePath), dataStoreReader);
                }
            }

            return dataStoreReader;
        }

        private DataStoreReader GetDataStoreReaderOrDefault(string storeName, string storePath)
        {
            var key = (storeName, storePath);

            lock (this.dataStoreReaders)
            {
                if (this.dataStoreReaders.ContainsKey(key))
                {
                    return this.dataStoreReaders[key];
                }
            }

            return null;
        }

        private IStreamIntervalProvider GetOrCreateStreamIntervalProvider<T>(StreamSource streamSource) =>
            this.GetOrCreateDataStoreReader(streamSource)
                .GetOrCreateStreamIntervalProvider<T>(streamSource);

        private void DataStoreReader_NoRemainingSubscribers(object sender, EventArgs e)
        {
            var dataStoreReader = sender as DataStoreReader;

            lock (this.dataStoreReaders)
            {
                // If the data store reader has no more subscribers, remove it from the collection.
                if (!dataStoreReader.HasSubscribers)
                {
                    var key = (dataStoreReader.StoreName, dataStoreReader.StorePath);
                    this.dataStoreReaders.Remove(key);
                    dataStoreReader.NoRemainingSubscribers -= this.DataStoreReader_NoRemainingSubscribers;
                    dataStoreReader.StreamReadError -= this.DataStoreReader_StreamReadError;
                    dataStoreReader.Dispose();
                }
            }
        }

        private void DataStoreReader_StreamReadError(object sender, StreamReadErrorEventArgs e)
        {
            // Alert the visualization objects that the stream is unreadable.
            this.StreamReadError?.Invoke(this, e);
        }
    }
}