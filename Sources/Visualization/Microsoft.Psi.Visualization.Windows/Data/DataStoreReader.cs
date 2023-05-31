// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Implements an object used to read stream data from a specific data store.
    /// </summary>
    /// <remarks>
    /// The <see cref="DataStoreReader"/> attempts to batch multiple reads for the same streams
    /// together and improve the efficiency of data access when multiple consumers try to
    /// retrive the same data.
    /// </remarks>
    public class DataStoreReader : IDisposable
    {
        /// <summary>
        /// The list of streams that have been identified as unreadable, probably due to the format of the
        /// message on disk not matching the current format of the data object they are deserialized from.
        /// </summary>
        private readonly List<string> unreadableStreams = new ();
        private readonly List<IStreamDataProvider> streamDataProviders = new ();

        private IStreamReader streamReader;
        private List<ExecutionContext> executionContexts;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreReader"/> class.
        /// </summary>
        /// <param name="storeName">Store name to read.</param>
        /// <param name="storePath">Store path to read.</param>
        /// <param name="streamReaderType">Stream reader type.</param>
        internal DataStoreReader(string storeName, string storePath, Type streamReaderType)
        {
            this.StoreName = storeName;
            this.StorePath = storePath;
            this.streamReader = StreamReader.Create(storeName, storePath, streamReaderType);
            this.executionContexts = new List<ExecutionContext>();
        }

        /// <summary>
        /// Event that fires when a data store reader has no more subscribers and can be removed.
        /// </summary>
        public event EventHandler NoRemainingSubscribers;

        /// <summary>
        /// Event that fires when a stream is unable to be read from.
        /// </summary>
        public event EventHandler<StreamReadErrorEventArgs> StreamReadError;

        /// <summary>
        /// Gets a value indicating whether the data store reader has any subscribers.
        /// </summary>
        public bool HasSubscribers => this.streamDataProviders.Count > 0;

        /// <summary>
        /// Gets the store name.
        /// </summary>
        internal string StoreName { get; }

        /// <summary>
        /// Gets the store path.
        /// </summary>
        internal string StorePath { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            // stop all stream data providers from publishing data
            this.streamDataProviders?.ForEach(sdp => sdp.Stop());

            // dispose and clean up execution contexts
            this.executionContexts.ForEach(ec =>
            {
                try
                {
                    ec.ReadTaskCancellationTokenSource.Cancel();
                    ec.ReadTask.Wait();
                }
                catch (AggregateException)
                {
                }

                ec.StreamReader.Dispose();
                ec.ReadTask.Dispose();
                ec.ReadTaskCancellationTokenSource.Dispose();
            });

            this.executionContexts.Clear();
            this.executionContexts = null;

            // dispose all stream data providers
            this.streamDataProviders?.ForEach(sdp => sdp.Dispose());
            this.streamDataProviders?.Clear();

            // dispose of stream reader
            this.streamReader?.Dispose();
            this.streamReader = null;
        }

        /// <summary>
        /// Gets the time of the nearest message to a specified time, on a specified stream.
        /// </summary>
        /// <param name="streamSource">The stream source specifying the stream of interest.</param>
        /// <param name="time">The time to find the nearest message to.</param>
        /// <param name="nearestType">The type of nearest message to find.</param>
        /// <returns>The time of the nearest message, if one is found or null otherwise.</returns>
        internal DateTime? GetTimeOfNearestMessage(StreamSource streamSource, DateTime time, NearestType nearestType) =>
            this.GetStreamProviderOrDefault(streamSource.StreamName)
                .GetTimeOfNearestMessage(time, nearestType);

        /// <summary>
        /// Gets or creates a stream interval provider for a specified stream source.
        /// </summary>
        /// <typeparam name="T">The type of messages in the stream.</typeparam>
        /// <param name="streamSource">The stream source.</param>
        /// <returns>The stream interval provider.</returns>
        internal IStreamIntervalProvider GetOrCreateStreamIntervalProvider<T>(StreamSource streamSource)
        {
            var streamIntervalProvider = this.GetStreamIntervalProviderOrDefault(streamSource.StreamName, streamSource.StreamAdapter);

            if (streamIntervalProvider == null)
            {
                streamIntervalProvider = new StreamIntervalProvider<T>(streamSource);
                streamIntervalProvider.NoRemainingSubscribers += this.StreamDataProvider_NoRemainingSubscribers;
                (streamIntervalProvider as StreamDataProvider<T>).StreamReadError += this.OnStreamReadError;
                lock (this.streamDataProviders)
                {
                    this.streamDataProviders.Add(streamIntervalProvider);
                }
            }

            return streamIntervalProvider;
        }

        /// <summary>
        /// Gets or creates a stream value provider for a specified stream source.
        /// </summary>
        /// <typeparam name="T">The type of messages in the stream.</typeparam>
        /// <param name="streamSource">The stream source.</param>
        /// <returns>The stream value provider.</returns>
        internal IStreamValueProvider GetOrCreateStreamValueProvider<T>(StreamSource streamSource)
        {
            var streamValueProvider = this.GetStreamValueProviderOrDefault(streamSource.StreamName);

            if (streamValueProvider == null)
            {
                streamValueProvider = new StreamValueProvider<T>(streamSource);
                streamValueProvider.NoRemainingSubscribers += this.StreamDataProvider_NoRemainingSubscribers;
                (streamValueProvider as StreamDataProvider<T>).StreamReadError += this.OnStreamReadError;
                lock (this.streamDataProviders)
                {
                    this.streamDataProviders.Add(streamValueProvider);
                }
            }

            return streamValueProvider;
        }

        /// <summary>
        /// Unregisters a subscriber to interval data.
        /// </summary>
        /// <param name="subscriberId">The subscriber id token that was returned to the subscriber when it initially subscribed.</param>
        internal void UnregisterStreamIntervalSubscriber(Guid subscriberId) =>
            this.GetStreamIntervalProviders()
                .ForEach(sip => sip.UnregisterStreamIntervalSubscriber(subscriberId));

        /// <summary>
        /// Unregisters a stream value subscriber.
        /// </summary>
        /// <typeparam name="TData">The type of data expected by the stream value subscriber.</typeparam>
        /// <param name="subscriberId">The subscriber id that the target was given when it was initially registered.</param>
        internal void UnregisterStreamValueSubscriber<TData>(Guid subscriberId) =>
            this.GetStreamValueProviders()
                .ForEach(svp => svp.UnregisterStreamValueSubscriber<TData>(subscriberId));

        /// <summary>
        /// Sets the caching interval for all stream value providers in the data store reader.
        /// </summary>
        /// <param name="timeInterval">The time interval to cache.</param>
        internal void SetStreamValueProvidersCacheInterval(TimeInterval timeInterval) =>
            this.GetStreamValueProviders()
                .ForEach(svp => svp.SetCacheInterval(timeInterval));

        /// <summary>
        /// Checks if a stream is known to be unreadable.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>True if the stream is currently considered readable, otherwise false.</returns>
        internal bool StreamIsUnreadable(string streamName)
        {
            lock (this.unreadableStreams)
            {
                return this.unreadableStreams.Contains(streamName);
            }
        }

        /// <summary>
        /// Instructs all stream value providers to read data at the specified time and publish it to all registered stream value subscribers.
        /// </summary>
        /// <param name="dateTime">The time for the value to read and publish.</param>
        internal void ReadAndPublishStreamValues(DateTime dateTime)
        {
            using IStreamReader reader = this.streamReader.OpenNew();
            foreach (var streamValueProvider in this.GetStreamValueProviders())
            {
                if (!this.StreamIsUnreadable(streamValueProvider.StreamName))
                {
                    try
                    {
                        // If a serialization exception is thrown, it will be because the format of the data has changed
                        // since the store we're reading from was created.  This may be surfaced either as a direct
                        // SerializationException or will be wrapped in a TargetInvocationException.  In both cases,
                        // we call StreamReadError which will mark the stream as not readable so that DataManager will
                        // no longer attempt to read from it, and also fire an event that the visualization object will
                        // catch and then use to construct an error message explaining which fields have changed in the data.
                        streamValueProvider.ReadAndPublishStreamValue(reader, dateTime);
                    }
                    catch (SerializationException ex)
                    {
                        this.OnStreamReadError(this, new StreamReadErrorEventArgs() { StreamName = streamValueProvider.StreamName, Exception = ex });
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (ex.InnerException is SerializationException serializationException)
                        {
                            this.OnStreamReadError(this, new StreamReadErrorEventArgs() { StreamName = streamValueProvider.StreamName, Exception = serializationException });
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the supplemental metadata for a stream.
        /// </summary>
        /// <typeparam name="TSupplementalMetadata">The type of the supplemental metadata.</typeparam>
        /// <param name="streamName">The stream name.</param>
        /// <returns>The supplemental metadata for the specified stream.</returns>
        internal TSupplementalMetadata GetSupplementalMetadata<TSupplementalMetadata>(string streamName) =>
            this.streamReader.GetSupplementalMetadata<TSupplementalMetadata>(streamName);

        /// <summary>
        /// Performs a series of updates to the messages in a stream.
        /// </summary>
        /// <typeparam name="T">The type of the messages in the stream.</typeparam>
        /// <param name="streamSource">The stream source indicating which stream to read from.</param>
        /// <param name="updates">A collection of updates to perform.</param>
        internal void UpdateStream<T>(StreamSource streamSource, IEnumerable<StreamUpdate<T>> updates) =>
            this.GetStreamIntervalProviderOrDefault(streamSource.StreamName, streamSource.StreamAdapter)
                .UpdateStream<T>(updates);

        /// <summary>
        /// Saves all updates to all changed streams in the store to disk.
        /// </summary>
        /// <param name="progress">A progress interface to report back to.</param>
        /// <returns>The list of the names of streams that had updates saved.</returns>
        internal string[] SaveChanges(IProgress<double> progress)
        {
            var updates = new Dictionary<string, IEnumerable<(bool, dynamic, DateTime)>>();

            // Get all the changes from all the stream readers that have them.
            foreach (var streamIntervalProvider in this.GetStreamIntervalProviders())
            {
                if (streamIntervalProvider.HasUncommittedUpdates)
                {
                    updates[streamIntervalProvider.StreamName] = streamIntervalProvider.GetUncommittedUpdates();
                }
            }

            // Edit the store in place
            PsiStore.EditInPlace((this.StoreName, this.StorePath), updates, null, true, progress);

            // return the list of streams that changed
            return updates.Keys.ToArray();
        }

        /// <summary>
        /// Periodically called by the <see cref="DataManager"/> on the UI thread to give data store readers time to process read requests.
        /// </summary>
        internal void Run()
        {
            lock (this.executionContexts)
            {
                // cleanup completed execution contexts
                var completedExecutionContexts = new List<ExecutionContext>();
                foreach (var executionContext in this.executionContexts.Where(
                    ec => ec.ReadTask != null && (ec.ReadTask.IsCanceled || ec.ReadTask.IsCompleted || ec.ReadTask.IsFaulted)))
                {
                    executionContext.StreamReader.Dispose();
                    executionContext.ReadTask.Dispose();
                    executionContext.ReadTaskCancellationTokenSource.Dispose();
                    completedExecutionContexts.Add(executionContext);
                }

                // removed completed execution contexts
                completedExecutionContexts.ForEach(cec => this.executionContexts.Remove(cec));
            }

            lock (this.streamDataProviders)
            {
                IEnumerable<IGrouping<Tuple<DateTime, DateTime>, Tuple<ReadRequest, IStreamDataProvider>>> groups = null;

                // NOTE: We might need to refactor this to avoid changing ReadRequests while we are enumerating over them.
                //       One approach might be adding a back pointer from the ReadRequest to the StreamReader so that the ReadRequest can lock the StreamReader
                // group StreamReaders by start and end time of read requests - a StreamReader can be included more than once if it has a disjointed read requests
                groups = this.streamDataProviders
                    .Select(streamProvider => streamProvider.ReadRequests.Select(rr => Tuple.Create(rr, streamProvider)))
                    .SelectMany(rr2sr => rr2sr)
                    .GroupBy(rr2sr => Tuple.Create(rr2sr.Item1.StartTime, rr2sr.Item1.EndTime));

                // walk groups of matching start and end time read requests
                foreach (var group in groups)
                {
                    // setup the execution context (needed for cleanup)
                    var executionContext = new ExecutionContext()
                    {
                        StreamReader = this.streamReader.OpenNew(),
                        ReadTaskCancellationTokenSource = new CancellationTokenSource(),
                    };

                    // open each stream that has a match, and close read request
                    foreach (var (readRequest, streamDataProvider) in group)
                    {
                        streamDataProvider.OpenStream(executionContext.StreamReader);
                        streamDataProvider.RemoveReadRequest(readRequest.StartTime, readRequest.EndTime);
                    }

                    // create new task
                    executionContext.ReadTask = Task.Factory.StartNew(() =>
                    {
                        // read all of the data
                        executionContext.StreamReader.ReadAll(
                            new ReplayDescriptor(group.Key.Item1, group.Key.Item2, true),
                            executionContext.ReadTaskCancellationTokenSource.Token);
                    });

                    // save execution context for cleanup
                    lock (this.executionContexts)
                    {
                        this.executionContexts.Add(executionContext);
                    }
                }
            }
        }

        /// <summary>
        /// Periodically called by the <see cref="DataManager"/> on the UI thread to give data store readers time dispatch data from internal buffers to views.
        /// </summary>
        internal void DispatchData() =>
            this.GetStreamProviders()
                .ForEach(streamProvider => streamProvider.DispatchData());

        /// <summary>
        /// Gets the list of stream providers.
        /// </summary>
        /// <returns>The list of stream providers.</returns>
        private List<IStreamDataProvider> GetStreamProviders()
        {
            lock (this.streamDataProviders)
            {
                return this.streamDataProviders.ToList();
            }
        }

        /// <summary>
        /// Gets a stream provider for a specified stream name.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <returns>The stream provider.</returns>
        private IStreamDataProvider GetStreamProviderOrDefault(string streamName) =>
            this.GetStreamProviders()
                .FirstOrDefault(sc => sc.StreamName == streamName);

        /// <summary>
        /// Gets the list of stream interval providers.
        /// </summary>
        /// <returns>The list of stream interval providers.</returns>
        private List<IStreamIntervalProvider> GetStreamIntervalProviders()
        {
            lock (this.streamDataProviders)
            {
                return this.streamDataProviders.OfType<IStreamIntervalProvider>().ToList();
            }
        }

        /// <summary>
        /// Gets the stream interval provider based on a specified stream name and adapter.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <param name="streamAdapter">The stream adapter.</param>
        /// <returns>The stream interval provider.</returns>
        private IStreamIntervalProvider GetStreamIntervalProviderOrDefault(string streamName, IStreamAdapter streamAdapter) =>
            this.GetStreamIntervalProviders()
                .FirstOrDefault(sc => sc.StreamName == streamName && Equals(sc.StreamAdapter, streamAdapter));

        /// <summary>
        /// Gets the list of stream value providers.
        /// </summary>
        /// <returns>The list of stream value providers.</returns>
        private List<IStreamValueProvider> GetStreamValueProviders()
        {
            lock (this.streamDataProviders)
            {
                return this.streamDataProviders.OfType<IStreamValueProvider>().ToList();
            }
        }

        /// <summary>
        /// Gets a stream value provider for a specified stream source.
        /// </summary>
        /// <param name="streamName">The stream name.</param>
        /// <returns>The stream value provider.</returns>
        private IStreamValueProvider GetStreamValueProviderOrDefault(string streamName) =>
            this.GetStreamValueProviders().FirstOrDefault(sc => sc.StreamName == streamName);

        private void StreamDataProvider_NoRemainingSubscribers(object sender, EventArgs e)
        {
            var streamDataProvider = sender as IStreamDataProvider;

            lock (this.streamDataProviders)
            {
                if (this.streamDataProviders.Contains(streamDataProvider) && !streamDataProvider.HasSubscribers)
                {
                    this.streamDataProviders.Remove(streamDataProvider);
                    streamDataProvider.NoRemainingSubscribers -= this.StreamDataProvider_NoRemainingSubscribers;
                    streamDataProvider.Dispose();
                }
            }

            // If we have no more stream data providers, notify the data manager
            if (this.streamDataProviders.Count == 0)
            {
                this.NoRemainingSubscribers?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnStreamReadError(object sender, StreamReadErrorEventArgs e)
        {
            // Add the stream to the list of known unreadable streams if it's not already there.
            lock (this.unreadableStreams)
            {
                if (!this.unreadableStreams.Contains(e.StreamName))
                {
                    this.unreadableStreams.Add(e.StreamName);
                }
            }

            // Add the store name and path and propagate the message to the data manager
            e.StoreName = this.StoreName;
            e.StorePath = this.StorePath;

            this.StreamReadError?.Invoke(this, e);
        }

        private struct ExecutionContext
        {
            public IStreamReader StreamReader;
            public Task ReadTask;
            public CancellationTokenSource ReadTaskCancellationTokenSource;
        }
    }
}
