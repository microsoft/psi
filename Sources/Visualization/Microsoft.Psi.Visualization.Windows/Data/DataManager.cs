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
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Windows;

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

        /// <summary>
        /// The collection of data store readers.  The keys are a tuple of store name and store path.
        /// </summary>
        private Dictionary<(string storeName, string storePath), DataStoreReader> dataStoreReaders = new Dictionary<(string storeName, string storePath), DataStoreReader>();

        private StreamSummaryManagers streamSummaryManagers = new StreamSummaryManagers();

        /// <summary>
        /// The task that identifies the current read operation for instant
        /// data, or null if no operation is currently in progress.
        /// </summary>
        private Task instantDataReadTask = null;

        /// <summary>
        /// The cursor time which should be used for the next read of instant data after the
        /// current read operation finishes, or null if there are no read requests outstanding.
        /// </summary>
        private DateTime? nextInstantReadTaskCursorTime;

        /// <summary>
        /// The lock used to control access to nextInstantReadTaskCursorTime.
        /// </summary>
        private object instantReadLock = new object();

        private DispatcherTimer dataDispatchTimer;
        private bool disposed;

        private DataManager()
        {
            this.disposed = false;
            this.nextInstantReadTaskCursorTime = null;
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

        /// <summary>
        /// Event that fires when a data store's dirty/clean status has changed.
        /// </summary>
        public event EventHandler<DataStoreStatusChangedEventArgs> DataStoreStatusChanged;

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Registers an instant data target to be notified when new data for a stream is available.
        /// </summary>
        /// <typeparam name="TTarget">The type of data the target requires.</typeparam>
        /// <param name="streamSource">Information about the stream source and the required stream adapter.</param>
        /// <param name="cursorEpsilon">The epsilon window to use when reading data at a given time.</param>
        /// <param name="callback">The method to call when new data is available.</param>
        /// <param name="viewRange">The initial time range over which data is expected.</param>
        /// <returns>A registration token that must be used by the target to unregister from updates or to modify the read epsilon.</returns>
        public Guid RegisterInstantDataTarget<TTarget>(StreamSource streamSource, RelativeTimeInterval cursorEpsilon, Action<object, StreamCacheEntry> callback, TimeInterval viewRange)
        {
            // Create the instant data target
            InstantDataTarget target = new InstantDataTarget(
                streamSource.StreamName,
                streamSource.StreamAdapter != null ? streamSource.StreamAdapter : new PassthroughAdapter<TTarget>(),
                cursorEpsilon,
                callback);

            // Get the appropriate data store reader
            DataStoreReader dataStoreReader = this.GetOrCreateDataStoreReader(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType);

            // Create the registration method
            MethodInfo method = typeof(DataStoreReader).GetMethod(nameof(DataStoreReader.GetOrCreateStreamCache), BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo genericMethod = method.MakeGenericMethod(target.StreamAdapter.SourceType, target.StreamAdapter.DestinationType);

            // Register the target the data store reader
            genericMethod.Invoke(dataStoreReader, new object[] { target, viewRange });

            // Pass the registration token back to the caller so he can use it to unregister later
            return target.RegistrationToken;
        }

        /// <summary>
        /// Unregisters instant data target from being notified when the current value of a stream changes.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        public void UnregisterInstantDataTarget(Guid registrationToken)
        {
            foreach (DataStoreReader dataStoreReader in this.dataStoreReaders.Values.ToList())
            {
                dataStoreReader.UnregisterInstantDataTarget(registrationToken);
            }
        }

        /// <summary>
        /// Updates the cursor epsilon for a registered instant visualization object.
        /// </summary>
        /// <param name="registrationToken">The registration token that the target was given when it was initially registered.</param>
        /// <param name="epsilon">A relative time interval specifying the window around a message time that may be considered a match.</param>
        public void UpdateInstantDataTargetEpsilon(Guid registrationToken, RelativeTimeInterval epsilon)
        {
            foreach (DataStoreReader dataStoreReader in this.dataStoreReaders.Values.ToList())
            {
                dataStoreReader.UpdateInstantDataTargetEpsilon(registrationToken, epsilon);
            }
        }

        /// <summary>
        /// Reads the data for all instant data targets and calls them back
        /// when the data is ready. This method is only called by the UI thread.
        /// </summary>
        /// <param name="cursorTime">The time of the visualization container's cursor.</param>
        public void ReadInstantData(DateTime cursorTime)
        {
            // Update the cursor time where the next read should occur
            lock (this.instantReadLock)
            {
                this.nextInstantReadTaskCursorTime = cursorTime;
            }

            // If the instant read task is not running, start it.
            if ((this.instantDataReadTask == null) || this.instantDataReadTask.IsCompleted)
            {
                this.instantDataReadTask = Task.Run(this.RunReadInstantDataTask);
            }
        }

        /// <summary>
        /// Notifies the data manager that the possible range of data that may be read has changed.
        /// </summary>
        /// <param name="viewRange">The new view range of the navigator.</param>
        public void OnInstantViewRangeChanged(TimeInterval viewRange)
        {
            foreach (DataStoreReader dataStoreReader in this.GetDataStoreReaderList())
            {
                dataStoreReader.OnInstantViewRangeChanged(viewRange);
            }
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching start and end times and asynchronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(StreamSource streamSource, DateTime startTime, DateTime endTime)
        {
            if (endTime < startTime)
            {
                throw new ArgumentException("End time must be greater than or equal to start time.", nameof(endTime));
            }

            var dataStoreReader = this.GetOrCreateDataStoreReader(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType);
            return dataStoreReader.ReadStream(streamSource, ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode.Fixed, startTime, endTime, 0, null);
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching tail count and asynchronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(StreamSource streamSource, uint tailCount)
        {
            if (tailCount == 0)
            {
                throw new ArgumentException("Tail count must be greater than 0", nameof(tailCount));
            }

            var dataStoreReader = this.GetOrCreateDataStoreReader(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType);
            return dataStoreReader.ReadStream(streamSource, ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode.TailCount, DateTime.MinValue, DateTime.MaxValue, tailCount, null);
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching tail range and asynchronously fills it in.
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(StreamSource streamSource, Func<DateTime, DateTime> tailRange)
        {
            if (tailRange == null)
            {
                throw new ArgumentNullException(nameof(tailRange));
            }

            var dataStoreReader = this.GetOrCreateDataStoreReader(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType);
            return dataStoreReader.ReadStream(streamSource, ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode.TailRange, DateTime.MinValue, DateTime.MaxValue, 0, tailRange);
        }

        /// <summary>
        /// Registers a consumer of summary data.
        /// </summary>
        /// <param name="streamSource">A stream source that indicates the store and stream summary data that the client consumes.</param>
        /// <returns>A unique consumer id that should be provided when the consumer unregisters from the stream summary manager.</returns>
        public Guid RegisterSummaryDataConsumer(StreamSource streamSource)
        {
            lock (this.streamSummaryManagers)
            {
                return this.GetOrCreateStreamSummaryManager(streamSource).RegisterConsumer();
            }
        }

        /// <summary>
        /// Unregisters a consumer of summary data.
        /// </summary>
        /// <param name="consumerId">The id that was returned to the consumer when it registered as a summary data consumer.</param>
        public void UnregisterSummaryDataConsumer(Guid consumerId)
        {
            lock (this.streamSummaryManagers)
            {
                this.streamSummaryManagers.FirstOrDefault(ssm => ssm.ContainsConsumer(consumerId)).UnregisterConsumer(consumerId);
            }
        }

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="startTime">The start time of the view range.</param>
        /// <param name="endTime">The end time of the view range.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(StreamSource streamSource, DateTime startTime, DateTime endTime, TimeSpan interval)
        {
            var viewMode = ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode.Fixed;
            lock (this.streamSummaryManagers)
            {
                return this.GetStreamSummaryManager(streamSource).ReadSummary(streamSource, viewMode, startTime, endTime, interval, 0, null);
            }
        }

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="tailCount">Number of items to include in view.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(StreamSource streamSource, TimeSpan interval, uint tailCount)
        {
            var viewMode = ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode.TailCount;
            lock (this.streamSummaryManagers)
            {
                return this.GetStreamSummaryManager(streamSource).ReadSummary(streamSource, viewMode, DateTime.MinValue, DateTime.MaxValue, interval, tailCount, null);
            }
        }

        /// <summary>
        /// Gets a view over the specified time range of the cached summary data.
        /// </summary>
        /// <typeparam name="T">The summary data type.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="interval">The time interval each summary value should cover.</param>
        /// <param name="tailRange">Tail duration function. Computes the view range start time given an end time. Applies to live view mode only.</param>
        /// <returns>A view over the cached summary data that covers the specified time range.</returns>
        public ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView ReadSummary<T>(StreamSource streamSource, TimeSpan interval, Func<DateTime, DateTime> tailRange)
        {
            var viewMode = ObservableKeyedCache<DateTime, IntervalData<T>>.ObservableKeyedView.ViewMode.TailRange;
            lock (this.streamSummaryManagers)
            {
                return this.GetStreamSummaryManager(streamSource).ReadSummary(streamSource, viewMode, DateTime.MinValue, DateTime.MaxValue, interval, 0, tailRange);
            }
        }

        /// <summary>
        /// Gets the supplemental metadata for a stream.
        /// </summary>
        /// <typeparam name="TSupplementalMetadata">The type of the supplemental metadata.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <returns>The supplemental metadata for the stream.</returns>
        public TSupplementalMetadata GetSupplementalMetadata<TSupplementalMetadata>(StreamSource streamSource)
        {
            DataStoreReader dataStoreReader = this.GetOrCreateDataStoreReader(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType);
            return dataStoreReader.GetSupplementalMetadata<TSupplementalMetadata>(streamSource);
        }

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

            DataStoreReader dataStoreReader = this.GetDataStoreReader(streamSource.StoreName, streamSource.StorePath);
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
            DataStoreReader dataStoreReader = this.GetDataStoreReader(storeName, storePath);
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
            DataStoreReader dataStoreReader = this.GetDataStoreReader(storeName, storePath);
            if (dataStoreReader != null)
            {
                dataStoreReader.Dispose();
                this.RemoveDataStoreReader(storeName, storePath);
            }
        }

        /// <summary>
        /// Gets originating time of the message in a stream that's closest to a given time.
        /// </summary>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="time">The time for which to return the message with the closest originating time.</param>
        /// <returns>The originating time of the message closest to time.</returns>
        public DateTime? GetOriginatingTimeOfNearestInstantMessage(StreamSource streamSource, DateTime time)
        {
            return this.GetOrCreateDataStoreReader(streamSource.StoreName, streamSource.StorePath, streamSource.StreamReaderType).GetOriginatingTimeOfNearestInstantMessage(streamSource, time);
        }

        /// <summary>
        /// Runs a task to read instant data, and continues to run read tasks until there are no read requests left.
        /// </summary>
        private void RunReadInstantDataTask()
        {
            DateTime? taskCursorTime;

            while (true)
            {
                // Get the cursor time for the next read request and then null it out
                lock (this.instantReadLock)
                {
                    taskCursorTime = this.nextInstantReadTaskCursorTime;
                    this.nextInstantReadTaskCursorTime = null;
                }

                // If there's a cursor time, initiate an instant read request on each data store reader, otherwise we're done.
                if (taskCursorTime.HasValue)
                {
                    try
                    {
                        Parallel.ForEach(this.GetDataStoreReaderList(), dataStoreReader => dataStoreReader.ReadInstantData(taskCursorTime.Value));
                    }
                    catch (Exception ex)
                    {
                        new MessageBoxWindow(
                            Application.Current.MainWindow,
                            "Instant Data Push Error",
                            $"An error occurred while attempting to push instant data to the visualization objects{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                            "Close",
                            null).ShowDialog();
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
            foreach (var dataStoreReader in this.GetDataStoreReaderList())
            {
                // run data store readers
                dataStoreReader.Run();

                // dispatch data for data stream readers
                dataStoreReader.DispatchData();
            }

            List<StreamSummaryManager> streamSummaryManagerList;
            lock (this.streamSummaryManagers)
            {
                streamSummaryManagerList = this.streamSummaryManagers.ToList();
            }

            foreach (var streamSummaryManager in streamSummaryManagerList)
            {
                // dispatch data for stream summary managers
                streamSummaryManager.DispatchData();
            }
        }

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

            lock (this.dataStoreReaders)
            {
                ValueTuple<string, string> key = (storeName, storePath);

                if (this.dataStoreReaders.ContainsKey(key))
                {
                    return this.dataStoreReaders[key];
                }
                else
                {
                    DataStoreReader dataStoreReader = new DataStoreReader(storeName, storePath, streamReaderType);
                    this.dataStoreReaders[key] = dataStoreReader;
                    return dataStoreReader;
                }
            }
        }

        private DataStoreReader GetDataStoreReader(string storeName, string storePath)
        {
            ValueTuple<string, string> key = (storeName, storePath);

            lock (this.dataStoreReaders)
            {
                if (this.dataStoreReaders.ContainsKey(key))
                {
                    return this.dataStoreReaders[key];
                }
            }

            return null;
        }

        private void RemoveDataStoreReader(string storeName, string storePath)
        {
            ValueTuple<string, string> key = (storeName, storePath);
            this.dataStoreReaders.Remove(key);
        }

        private List<DataStoreReader> GetDataStoreReaderList()
        {
            // Locks the data store reader collection and then extracts the list.
            lock (this.dataStoreReaders)
            {
                return this.dataStoreReaders.Values.ToList();
            }
        }

        private StreamSummaryManager GetStreamSummaryManager(StreamSource streamSource)
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

        private StreamSummaryManager GetOrCreateStreamSummaryManager(StreamSource streamSource)
        {
            StreamSummaryManager streamSummaryManager = this.GetStreamSummaryManager(streamSource);

            if (streamSummaryManager == null)
            {
                streamSummaryManager = new StreamSummaryManager(streamSource);
                streamSummaryManager.NoRemainingConsumers += this.StreamSummaryManager_NoRemainingConsumers;
                this.streamSummaryManagers.Add(streamSummaryManager);
            }

            return streamSummaryManager;
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