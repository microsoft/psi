// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Helpers;
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
        public static readonly DataManager Instance = new DataManager();

        private readonly StreamSummaryManagers streamSummaryManagers = new StreamSummaryManagers();

        /// <summary>
        /// The lock used to control access to readAndPublishStreamValueDateTime.
        /// </summary>
        private readonly object readAndPublishStreamValueDateTimeLock = new object();

        private readonly DispatcherTimer dataDispatchTimer;

        /// <summary>
        /// The collection of data store readers.  The keys are a tuple of store name and store path.
        /// </summary>
        private Dictionary<(string storeName, string storePath), DataStoreReader> dataStoreReaders = new Dictionary<(string storeName, string storePath), DataStoreReader>();

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
        /// <returns>A registration token that can be used to unregister the stream value subscriber.</returns>
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
            var registrationToken = streamValueProvider.RegisterStreamValueSubscriber(streamSource.StreamAdapter, epsilonInterval, target);

            // Set the cache interval
            streamValueProvider.SetCacheInterval(cacheInterval);

            return registrationToken;
        }

        /// <summary>
        /// Unregisters a stream value subscriber.
        /// </summary>
        /// <typeparam name="TData">The type of data expected by the stream value subscriber.</typeparam>
        /// <param name="registrationToken">The registration token that the subscriber was assigned when it was initially registered.</param>
        public void UnregisterStreamValueSubscriber<TData>(Guid registrationToken) =>
            this.GetDataStoreReaders()
                .ForEach(dataStoreReader => dataStoreReader.UnregisterStreamValueSubscriber<TData>(registrationToken));

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
        /// Registers a consumer of summary data.
        /// </summary>
        /// <param name="streamSource">A stream source that indicates the store and stream summary data that the client consumes.</param>
        /// <returns>A unique consumer id that should be provided when the consumer unregisters from the stream summary manager.</returns>
        public Guid RegisterSummaryDataConsumer(StreamSource streamSource) =>
            this.GetOrCreateStreamSummaryManager(streamSource)
                .RegisterConsumer();

        /// <summary>
        /// Unregisters a consumer of summary data.
        /// </summary>
        /// <param name="consumerId">The id that was returned to the consumer when it registered as a summary data consumer.</param>
        public void UnregisterSummaryDataConsumer(Guid consumerId) =>
            this.GetStreamSummaryManagers()
                .First(ssm => ssm.ContainsConsumer(consumerId))
                .UnregisterConsumer(consumerId);

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
            this.GetOrCreateStreamSummaryManager(streamSource)
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
            this.GetOrCreateStreamSummaryManager(streamSource)
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
            this.GetOrCreateStreamSummaryManager(streamSource)
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

            var dataStoreReader = this.GetDataStoreReaderOrDefault(streamSource.StoreName, streamSource.StorePath);
            dataStoreReader.UpdateStream(streamSource, updates);

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
        /// Closes all readers on a store to allow the store to be moved or updated.
        /// </summary>
        /// <param name="storeName">The name of the store to close.</param>
        /// <param name="storePath">The path to the store to close.</param>
        public void CloseStore(string storeName, string storePath)
        {
            var dataStoreReader = this.GetDataStoreReaderOrDefault(storeName, storePath);
            dataStoreReader.Dispose();
            this.RemoveDataStoreReader(storeName, storePath);
        }

        /// <summary>
        /// Refreshes all the existing stores.
        /// </summary>
        public void Refresh()
        {
            foreach ((var storeName, var storePath) in this.GetDataStoreReaders().Select(dataStoreReader => (dataStoreReader.StoreName, dataStoreReader.StorePath)))
            {
                this.CloseStore(storeName, storePath);
            }
        }

        /// <summary>
        /// Gets the time of the nearest message to a specified time, on a specified stream.
        /// </summary>
        /// <param name="streamSource">The stream source specifying the stream of interest.</param>
        /// <param name="time">The time to find the nearest message to.</param>
        /// <param name="nearestMessageType">The type of nearest message to find.</param>
        /// <returns>The time of the nearest message, if one is found or null otherwise.</returns>
        public DateTime? GetTimeOfNearestMessage(StreamSource streamSource, DateTime time, NearestMessageType nearestMessageType) =>
            this.GetOrCreateDataStoreReader(streamSource)
                .GetTimeOfNearestMessage(streamSource, time, nearestMessageType);

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
                        Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            new MessageBoxWindow(
                                Application.Current.MainWindow,
                                "Error",
                                $"An error occurred while attempting to read stream values.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
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

            this.GetStreamSummaryManagers()
                .ForEach(streamSummaryManager =>
                {
                    // dispatch data for stream summary managers
                    streamSummaryManager.DispatchData();
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

        private void RemoveDataStoreReader(string storeName, string storePath)
        {
            var key = (storeName, storePath);

            lock (this.dataStoreReaders)
            {
                this.dataStoreReaders[key].StreamReadError -= this.DataStoreReader_StreamReadError;
                this.dataStoreReaders.Remove(key);
            }
        }

        private void DataStoreReader_StreamReadError(object sender, StreamReadErrorEventArgs e)
        {
            // Alert the visualization objects that the stream is unreadable.
            this.StreamReadError?.Invoke(this, e);
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
                    streamSummaryManager.NoRemainingConsumers += this.StreamSummaryManager_NoRemainingConsumers;
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

        private void StreamSummaryManager_NoRemainingConsumers(object sender, EventArgs e)
        {
            // If there's no remaining consumers on a stream summary manager, run a task to wait a short
            // while and then check if there's still no consumers before removing it from the collection.
            Task.Run(() => this.CheckNoRemainingStreamSummaryManagerConsumers(sender as StreamSummaryManager));
        }

        private void CheckNoRemainingStreamSummaryManagerConsumers(StreamSummaryManager streamSummaryManager)
        {
            // When we switch layouts the old and new layouts may both reference visualization objects that have the same stream source, but the visualization
            // object in the old layout needs to unregister as a consumer from the stream summary manager before the visualization object in the new layout
            // registers as a listener. In order to avoid disposing of a stream summary manager and all of its cached data, and then immediately having
            // to create it all again when the visualization object in the new layout registers as a listener, we instead wait a short while after the last
            // listener has unregistered before removing and disposing of the stream summary manager.
            Thread.Sleep(2000);

            // Create the key for the stream summary manager.
            var key = Tuple.Create(
                streamSummaryManager.StoreName,
                streamSummaryManager.StorePath,
                streamSummaryManager.StreamName,
                streamSummaryManager.StreamAdapter);

            // If the stream summary manager still has no consumers, remove it from the collection and dispose of it.
            lock (this.streamSummaryManagers)
            {
                if (this.streamSummaryManagers[key].ConsumerCount == 0)
                {
                    this.streamSummaryManagers.Remove(key);
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