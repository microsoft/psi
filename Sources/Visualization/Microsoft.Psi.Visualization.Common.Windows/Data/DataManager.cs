// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Adapters;
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

        /// <summary>
        /// The collection of data store readers.  The keys are a tuple of store name and store path.
        /// </summary>
        private Dictionary<(string storeName, string storePath, string simpleReaderTypeName), DataStoreReader> dataStoreReaders = new Dictionary<(string storeName, string storePath, string simpleReaderTypeName), DataStoreReader>();

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

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Registers an instant data target to be notified when new data for a stream is available.
        /// </summary>
        /// <typeparam name="TTarget">The type of data the target requires.</typeparam>
        /// <param name="streamBinding">Information about the stream source and the required stream adapter.</param>
        /// <param name="cursorEpsilon">The epsilon window to use when reading data at a given time.</param>
        /// <param name="callback">The method to call when new data is available.</param>
        /// <param name="viewRange">The initial time range over which data is expectd.</param>
        /// <returns>A registration token that must be used by the target to unregister from updates or to modify the read epsilon.</returns>
        public Guid RegisterInstantDataTarget<TTarget>(StreamBinding streamBinding, RelativeTimeInterval cursorEpsilon, Action<object, IndexEntry> callback, TimeInterval viewRange)
        {
            // Create the instant data target
            InstantDataTarget target = new InstantDataTarget(
                streamBinding.StreamName,
                streamBinding.StreamAdapter != null ? streamBinding.StreamAdapter : new PassthroughAdapter<TTarget>(),
                cursorEpsilon,
                callback);

            // Get the appropriate data store reader
            DataStoreReader dataStoreReader = this.FindDataStoreReader(streamBinding);

            // Create the registration method
            MethodInfo method = typeof(DataStoreReader).GetMethod(nameof(DataStoreReader.RegisterInstantDataTarget), BindingFlags.NonPublic | BindingFlags.Instance);
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
        /// Notifies the data manager that the possible range of data that mey be read has changed.
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

            var dataStoreReader = this.FindDataStoreReader(streamBinding);
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

            var dataStoreReader = this.FindDataStoreReader(streamBinding);
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

            var dataStoreReader = this.FindDataStoreReader(streamBinding);
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
                    Parallel.ForEach(this.GetDataStoreReaderList(), dataStoreReader => dataStoreReader.ReadInstantData(taskCursorTime.Value));
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

        private DataStoreReader FindDataStoreReader(StreamBinding streamBinding)
        {
            if (streamBinding == null)
            {
                throw new ArgumentNullException(nameof(streamBinding));
            }

            lock (this.dataStoreReaders)
            {
                ValueTuple<string, string, string> key = (streamBinding.StoreName, streamBinding.StorePath, streamBinding.SimpleReaderTypeName);

                if (this.dataStoreReaders.ContainsKey(key))
                {
                    return this.dataStoreReaders[key];
                }
                else
                {
                    DataStoreReader dataStoreReader = new DataStoreReader(streamBinding.StoreName, streamBinding.StorePath, streamBinding.SimpleReaderType);
                    this.dataStoreReaders[key] = dataStoreReader;
                    return dataStoreReader;
                }
            }
        }

        private List<DataStoreReader> GetDataStoreReaderList()
        {
            // Locks the data store reader collection and then extracts the list.
            lock (this.dataStoreReaders)
            {
                return this.dataStoreReaders.Values.ToList();
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

        private class StreamSummaryManagers : KeyedCollection<Tuple<string, string, string, Type>, StreamSummaryManager>
        {
            protected override Tuple<string, string, string, Type> GetKeyForItem(StreamSummaryManager streamSummaryManager)
            {
                return Tuple.Create(streamSummaryManager.StoreName, streamSummaryManager.StorePath, streamSummaryManager.StreamName, streamSummaryManager.StreamAdapterType);
            }
        }
    }
}