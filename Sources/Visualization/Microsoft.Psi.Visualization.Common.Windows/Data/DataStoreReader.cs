// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;

    /// <summary>
    /// Represents an object used to read data stores. Reads data stores by reading their underlying streams.
    /// Attempts to batch reads through a data store where possible.
    /// </summary>
    public class DataStoreReader : IDisposable
    {
        private ISimpleReader simpleReader;
        private List<ExecutionContext> executionContexts;
        private List<IStreamReader> streamReaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreReader"/> class.
        /// </summary>
        /// <param name="storeName">Store name to read.</param>
        /// <param name="storePath">Store path to read.</param>
        /// <param name="simpleReaderType">Simple reader type.</param>
        public DataStoreReader(string storeName, string storePath, Type simpleReaderType)
        {
            this.simpleReader = (ISimpleReader)simpleReaderType.GetConstructor(new Type[] { }).Invoke(new object[] { });
            this.simpleReader.OpenStore(storeName, storePath);
            this.executionContexts = new List<ExecutionContext>();
            this.streamReaders = new List<IStreamReader>();
        }

        /// <summary>
        /// Gets the store name to read.
        /// </summary>
        public string StoreName => this.simpleReader.Name;

        /// <summary>
        /// Gets the store path to read.
        /// </summary>
        public string StorePath => this.simpleReader.Path;

        /// <summary>
        /// Gets a new instance of the underlying simple reader. The instance must be disposed by the caller.
        /// </summary>
        /// <returns>The simple reader.</returns>
        public ISimpleReader GetReader()
        {
            return this.simpleReader.OpenNew();
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
            using (ISimpleReader reader = this.simpleReader.OpenNew())
            {
                return this.GetStreamReader<T>(streamBinding, true).Read<T>(reader, indexEntry);
            }
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
            return this.GetStreamReader<T>(streamBinding, true).ReadIndex(startTime, endTime);
        }

        /// <summary>
        /// Creates a view of the messages identified by the matching parameters and asynchronously fills it in.
        /// View mode can be one of three values:
        ///     Fixed - fixed range based on start and end times
        ///     TailCount - sliding dynamic range that includes the tail of the underlying data based on quantity
        ///     TailRange - sliding dynamic range that includes the tail of the underlying data based on function
        /// </summary>
        /// <typeparam name="T">The type of the message to read.</typeparam>
        /// <param name="streamBinding">The stream binding inidicating which stream to read from.</param>
        /// <param name="viewMode">Mode the view will be created in</param>
        /// <param name="startTime">Start time of messages to read.</param>
        /// <param name="endTime">End time of messages to read.</param>
        /// <param name="tailCount">Number of messages to included in tail.</param>
        /// <param name="tailRange">Function to determine range included in tail.</param>
        /// <returns>Observable view of data.</returns>
        public ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView ReadStream<T>(
            StreamBinding streamBinding,
            ObservableKeyedCache<DateTime, Message<T>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            return this.GetStreamReader<T>(streamBinding, false).ReadStream<T>(viewMode, startTime, endTime, tailCount, tailRange);
        }

        /// <summary>
        /// Periodically called by the <see cref="DataManager"/> on the UI thread to give data store readers time to process read requests.
        /// </summary>
        public void Run()
        {
            lock (this.executionContexts)
            {
                // cleanup completed execution contexts
                var completedEcs = new List<ExecutionContext>();
                foreach (var ec in
                    this.executionContexts.Where(ec => ec.ReadAllTask != null && (ec.ReadAllTask.IsCanceled || ec.ReadAllTask.IsCompleted || ec.ReadAllTask.IsFaulted)))
                {
                    ec.Reader.Dispose();
                    ec.ReadAllTask.Dispose();
                    ec.ReadAllTokenSource.Dispose();
                    completedEcs.Add(ec);
                }

                // removed completed execution contexts
                completedEcs.ForEach(cec => this.executionContexts.Remove(cec));
            }

            lock (this.streamReaders)
            {
                IEnumerable<IGrouping<Tuple<DateTime, DateTime>, Tuple<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>, IStreamReader>>> groups = null;

                // NOTE: We might need to refactor this to avoid changing ReadRequests while we are enumerating over them.
                //       One approach might be adding a back pointer from the ReadRequest to the StreamReader so that the ReadRequest can lock the StreamReader
                // group StreamReaders by start and end time of read requests - a StreamReader can be included more than once if it has a disjointed read requests
                groups = this.streamReaders
                    .Select(sr => sr.ReadRequests.Select(rr => Tuple.Create(rr, sr)))
                    .SelectMany(rr2sr => rr2sr)
                    .GroupBy(rr2sr => Tuple.Create(rr2sr.Item1.Item1, rr2sr.Item1.Item2));

                // walk groups of matching start and end time read requests
                foreach (var group in groups)
                {
                    // setup execution context (needed for cleanup)
                    var readAllTokenSource = new CancellationTokenSource();
                    var reader = this.simpleReader.OpenNew();
                    ExecutionContext executionContext = new ExecutionContext() { Reader = reader, ReadAllTokenSource = readAllTokenSource };

                    // open each stream that has a match, and close read request
                    foreach (var rr2sr in group)
                    {
                        var streamReader = rr2sr.Item2;
                        streamReader.OpenStream(executionContext.Reader);
                        streamReader.CompleteReadRequest(rr2sr.Item1.Item1, rr2sr.Item1.Item2);
                    }

                    // create new task
                    executionContext.ReadAllTask = Task.Factory.StartNew(() =>
                    {
                        // read all of the data
                        executionContext.Reader.ReadAll(new ReplayDescriptor(group.Key.Item1, group.Key.Item2, true), executionContext.ReadAllTokenSource.Token);
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
        public void DispatchData()
        {
            this.streamReaders.ForEach(sr => sr.DispatchData());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // cancel all stream readers
            this.streamReaders?.ForEach(sr => sr.Cancel());

            // dispose and clean up execution contexts
            this.executionContexts.ForEach(ec =>
            {
                try
                {
                    ec.ReadAllTokenSource.Cancel();
                    ec.ReadAllTask.Wait();
                }
                catch (AggregateException)
                {
                }

                ec.Reader.Dispose();
                ec.ReadAllTask.Dispose();
                ec.ReadAllTokenSource.Dispose();
            });

            this.executionContexts.Clear();
            this.executionContexts = null;

            // dispose all stream readers
            this.streamReaders?.ForEach(sr => sr.Dispose());
            this.streamReaders?.Clear();
            this.streamReaders = null;

            // dispose of simple reader
            this.simpleReader?.Dispose();
            this.simpleReader = null;
        }

        private IStreamReader GetStreamReader<T>(StreamBinding streamBinding, bool useIndex)
        {
            var streamReader = this.streamReaders.Find(sr => sr.StreamName == streamBinding.StreamName && sr.StreamAdapterType == streamBinding.StreamAdapterType);
            if (streamReader == null)
            {
                streamReader = new StreamReader<T>(streamBinding, useIndex);
                this.streamReaders.Add(streamReader);
            }

            return streamReader;
        }

        private struct ExecutionContext
        {
            public ISimpleReader Reader;
            public Task ReadAllTask;
            public CancellationTokenSource ReadAllTokenSource;
        }
    }
}
