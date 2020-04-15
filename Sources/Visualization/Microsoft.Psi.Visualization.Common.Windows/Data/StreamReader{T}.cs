// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Represents an object used to read streams.
    /// </summary>
    /// <typeparam name="T">The type of messages in stream.</typeparam>
    public class StreamReader<T> : IStreamReader
    {
        private readonly List<ReadRequest> readRequestsInternal;
        private readonly ReadOnlyCollection<ReadRequest> readRequests;

        /// <summary>
        /// Flag indicating whether underlying type needs disposing when removed.
        /// </summary>
        private readonly bool needsDisposing = typeof(IDisposable).IsAssignableFrom(typeof(T));

        private readonly object bufferLock;
        private List<Message<T>> dataBuffer;
        private List<IndexEntry> indexBuffer;
        private ObservableKeyedCache<DateTime, Message<T>> data;
        private ObservableKeyedCache<DateTime, IndexEntry> index;

        /// <summary>
        /// The view of the index used by instant stream readers.
        /// </summary>
        private ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView instantIndexView = null;

        /// <summary>
        /// The view range of the above instant index view.
        /// </summary>
        private NavigatorRange currentIndexViewRange = new NavigatorRange(DateTime.MinValue, DateTime.MinValue);

        /// <summary>
        /// The collection of readers supporting instant visualization objects.
        /// </summary>
        private List<EpsilonInstantStreamReader<T>> instantStreamReaders;

        /// <summary>
        /// The stream adapter to convert data from the stream into the type required by clients of the stream reader.
        /// </summary>
        private IStreamAdapter streamAdapter;

        private IPool pool;
        private bool isCanceled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReader{T}"/> class.
        /// </summary>
        /// <param name="streamName">the name of the stream to read.</param>
        /// <param name="streamAdapter">the stream adapter to convert data from the stream into the type required by clients of this stream reader.</param>
        public StreamReader(string streamName, IStreamAdapter streamAdapter)
        {
            if (string.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            this.StreamName = streamName;
            this.streamAdapter = streamAdapter;
            this.StreamAdapterType = streamAdapter?.GetType();

            this.pool = PoolManager.Instance.GetPool<T>();

            this.readRequestsInternal = new List<ReadRequest>();
            this.readRequests = new ReadOnlyCollection<ReadRequest>(this.readRequestsInternal);

            this.bufferLock = new object();
            this.dataBuffer = new List<Message<T>>(1000);
            this.indexBuffer = new List<IndexEntry>(1000);

            var itemComparer = Comparer<Message<T>>.Create((m1, m2) => m1.OriginatingTime.CompareTo(m2.OriginatingTime));
            var indexComarer = Comparer<IndexEntry>.Create((i1, i2) => i1.OriginatingTime.CompareTo(i2.OriginatingTime));

            this.data = new ObservableKeyedCache<DateTime, Message<T>>(null, itemComparer, m => m.OriginatingTime);
            this.index = new ObservableKeyedCache<DateTime, IndexEntry>(null, indexComarer, ie => ie.OriginatingTime);

            this.instantIndexView = null;

            this.instantStreamReaders = new List<EpsilonInstantStreamReader<T>>();

            if (this.needsDisposing)
            {
                this.data.CollectionChanged += this.OnCollectionChanged;
            }
        }

        /// <summary>
        /// Gets shared allocator.
        /// </summary>
        public Func<T> Allocator
        {
            get
            {
                if (this.pool == null)
                {
                    return null;
                }
                else
                {
                    return () => (T)this.pool.GetOrCreate();
                }
            }
        }

        /// <inheritdoc />
        public bool IsCanceled => this.isCanceled;

        /// <inheritdoc />
        public IReadOnlyList<ReadRequest> ReadRequests => this.readRequests;

        /// <inheritdoc />
        public Type StreamAdapterType { get; private set; }

        /// <inheritdoc />
        public string StreamName { get; private set; }

        /// <inheritdoc/>
        public bool HasInstantStreamReaders => this.instantStreamReaders.Count > 0;

        /// <inheritdoc />
        public void RegisterInstantDataTarget<TTarget>(InstantDataTarget target, TimeInterval viewRange)
        {
            this.InternalRegisterInstantDataTarget<TTarget>(target);

            // Create the index view if one does not already exist
            if (this.instantIndexView == null)
            {
                this.OnInstantViewRangeChanged(viewRange);
            }
        }

        /// <inheritdoc />
        public void UnregisterInstantDataTarget(Guid registrationToken)
        {
            this.InternalUnregisterInstantDataTarget(registrationToken);
        }

        /// <inheritdoc />
        public void UpdateInstantDataTargetEpsilon(Guid registrationToken, RelativeTimeInterval epsilon)
        {
            // Unregister and retrieve the old instant data target
            InstantDataTarget target = this.InternalUnregisterInstantDataTarget(registrationToken);

            if (target != null)
            {
                // Update the Ccursor epsilon
                target.CursorEpsilon = epsilon;

                // Create the internal register method
                MethodInfo method = this.GetType().GetMethod(nameof(this.InternalRegisterInstantDataTarget), BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo genericMethod = method.MakeGenericMethod(target.StreamAdapter.DestinationType);

                // Re-register the instant data target
                genericMethod.Invoke(this, new object[] { target });
            }
        }

        /// <inheritdoc/>
        public void OnInstantViewRangeChanged(TimeInterval viewRange)
        {
            // Check if the navigator view range exceeds the current range of the data index
            if (viewRange.Left < this.currentIndexViewRange.StartTime || viewRange.Right > this.currentIndexViewRange.EndTime)
            {
                // Set a new data index range thats extends to the left and right of the navigator view by the navigator view
                // duration so that we're not constantly needing to initiate an index read every time the navigator moves.
                TimeSpan viewDuration = viewRange.Span;
                this.currentIndexViewRange.SetRange(
                    viewRange.Left > DateTime.MinValue + viewDuration ? viewRange.Left - viewDuration : DateTime.MinValue,
                    viewRange.Right < DateTime.MaxValue - viewDuration ? viewRange.Right + viewDuration : DateTime.MaxValue);

                this.instantIndexView = this.ReadIndex(this.currentIndexViewRange.StartTime, this.currentIndexViewRange.EndTime);
            }
        }

        /// <inheritdoc />
        public void ReadInstantData(ISimpleReader reader, DateTime cursorTime)
        {
            // Forward the call to all the instant stream readers
            foreach (EpsilonInstantStreamReader<T> instantStreamReader in this.GetInstantStreamReaderList())
            {
                instantStreamReader.ReadInstantData(reader, cursorTime, this.index);
            }
        }

        /// <inheritdoc />
        public void Cancel()
        {
            this.isCanceled = true;
        }

        /// <inheritdoc />
        public void CompleteReadRequest(DateTime startTime, DateTime endTime)
        {
            lock (this.readRequestsInternal)
            {
                this.readRequestsInternal.RemoveAll(rr => rr.StartTime == startTime && rr.EndTime == endTime);
            }
        }

        /// <inheritdoc />
        public void DispatchData()
        {
            lock (this.bufferLock)
            {
                if (this.dataBuffer.Count > 0)
                {
                    this.data.AddRange(this.dataBuffer);
                    this.dataBuffer.Clear();
                }

                if (this.indexBuffer.Count > 0)
                {
                    this.index.AddRange(this.indexBuffer);
                    this.indexBuffer.Clear();
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            lock (this.bufferLock)
            {
                if (this.needsDisposing)
                {
                    this.data.CollectionChanged -= this.OnCollectionChanged;
                    foreach (var message in this.data)
                    {
                        var item = message.Data;
                        (item as IDisposable).Dispose();
                    }

                    foreach (var message in this.dataBuffer)
                    {
                        var item = message.Data;
                        (item as IDisposable).Dispose();
                    }
                }

                this.data.Clear();
                this.data = null;
                this.dataBuffer.Clear();
                this.dataBuffer = null;

                this.index.Clear();
                this.index = null;
                this.indexBuffer.Clear();
                this.indexBuffer = null;

                this.pool?.Dispose();
                this.pool = null;

                this.streamAdapter?.Dispose();
            }
        }

        /// <inheritdoc />
        public void OpenStream(ISimpleReader reader, bool readIndicesOnly)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (readIndicesOnly)
            {
                if (this.streamAdapter == null)
                {
                    reader.OpenStreamIndex<T>(this.StreamName, this.OnReceiveIndex);
                }
                else
                {
                    var genericOpenStreamIndex = typeof(ISimpleReader)
                        .GetMethod("OpenStreamIndex", new Type[] { typeof(string), typeof(Action<IndexEntry, Envelope>) })
                        .MakeGenericMethod(this.streamAdapter.SourceType);
                    var receiver = new Action<IndexEntry, Envelope>(this.OnReceiveIndex);
                    genericOpenStreamIndex.Invoke(reader, new object[] { this.StreamName, receiver });
                }
            }
            else
            {
                if (this.streamAdapter == null)
                {
                    reader.OpenStream<T>(this.StreamName, this.OnReceiveData, this.Allocator);
                }
                else
                {
                    dynamic dynStreamAdapter = this.streamAdapter;
                    dynamic dynAdaptedReceiver = dynStreamAdapter.AdaptReceiver(new Action<T, Envelope>(this.OnReceiveData));
                    reader.OpenStream(this.StreamName, dynAdaptedReceiver, dynStreamAdapter.Allocator);
                }
            }
        }

        /// <inheritdoc />
        public ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView ReadIndex(DateTime startTime, DateTime endTime)
        {
            lock (this.readRequestsInternal)
            {
                this.readRequestsInternal.AddRange(this.ComputeReadRequests(startTime, endTime, true));
            }

            return (this.index as ObservableKeyedCache<DateTime, IndexEntry>).GetView(
                ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView.ViewMode.Fixed, startTime, endTime, 0, null);
        }

        /// <inheritdoc />
        public ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView ReadStream<TItem>(
            ObservableKeyedCache<DateTime, Message<TItem>>.ObservableKeyedView.ViewMode viewMode,
            DateTime startTime,
            DateTime endTime,
            uint tailCount,
            Func<DateTime, DateTime> tailRange)
        {
            lock (this.readRequestsInternal)
            {
                this.readRequestsInternal.AddRange(this.ComputeReadRequests(startTime, endTime, false));
            }

            return (this.data as ObservableKeyedCache<DateTime, Message<TItem>>).GetView(viewMode, startTime, endTime, tailCount, tailRange);
        }

        private IList<ReadRequest> ComputeReadRequests(DateTime startTime, DateTime endTime, bool readIndicesOnly)
        {
            List<ReadRequest> newReadRequests = new List<ReadRequest>();

            // adjust read request to account for existing read requests
            IEnumerable<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>> matches = null;
            lock (this.readRequestsInternal)
            {
                matches = this.readRequestsInternal
                    .Where(rr => rr.ReadIndicesOnly == readIndicesOnly && rr.StartTime <= endTime && rr.EndTime >= startTime)
                    .Select(rr => Tuple.Create(rr.StartTime, rr.EndTime, rr.TailCount, rr.TailRange));

                this.ComputeReadRequests(newReadRequests, matches, ref startTime, ref endTime, readIndicesOnly);

                // adjust read request to account for existing views
                var views = readIndicesOnly ? this.index.ViewExtents.Where(rr => rr.Item1 <= endTime && rr.Item2 >= startTime) : this.data.ViewExtents.Where(rr => rr.Item1 <= endTime && rr.Item2 >= startTime);
                this.ComputeReadRequests(newReadRequests, views, ref startTime, ref endTime, readIndicesOnly);

                // finally add remaining range (if any) to read requests
                if (startTime < endTime)
                {
                    newReadRequests.Add(new ReadRequest(startTime, endTime, 0, null, readIndicesOnly));
                }
            }

            return newReadRequests;
        }

        private IEnumerable<ReadRequest> ComputeReadRequests(
            List<ReadRequest> newReadRequests,
            IEnumerable<Tuple<DateTime, DateTime, uint, Func<DateTime, DateTime>>> ranges,
            ref DateTime startTime,
            ref DateTime endTime,
            bool readIndicesOnly)
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
                else if (range.Item1 <= startTime)
                {
                    startTime = range.Item2;
                }

                // overlapping end
                else if (range.Item2 >= endTime)
                {
                    endTime = range.Item1;
                }

                // overlapping middle
                else if (range.Item2 > range.Item1)
                {
                    // compute read requests for first new range
                    newReadRequests.AddRange(this.ComputeReadRequests(startTime, range.Item1, readIndicesOnly));

                    // continue comptuing for second new range
                    startTime = range.Item2;
                }
            }

            return newReadRequests;
        }

        private void InternalRegisterInstantDataTarget<TTarget>(InstantDataTarget target)
        {
            // Get the instant stream reader with the required cursor epsilon
            EpsilonInstantStreamReader<T> instantStreamReader = this.GetInstantStreamReader(target.CursorEpsilon, true);

            // Register the instant visualization object with the instant stream reader
            instantStreamReader.RegisterInstantDataTarget<TTarget>(target);
        }

        private InstantDataTarget InternalUnregisterInstantDataTarget(Guid registrationToken)
        {
            lock (this.instantStreamReaders)
            {
                for (int index = this.instantStreamReaders.Count - 1; index >= 0; index--)
                {
                    // Unregister the target from the instant stream reader
                    InstantDataTarget target = this.instantStreamReaders[index].UnregisterInstantDataTarget(registrationToken);

                    if (target != null)
                    {
                        // If the instant stream reader now has no data providers, remove it from the collection
                        if (!this.instantStreamReaders[index].HasAdaptingDataProviders)
                        {
                            this.instantStreamReaders.RemoveAt(index);
                        }

                        // If there's no instant stream readers, remove the index view
                        if (this.instantStreamReaders.Count <= 0)
                        {
                            this.instantIndexView = null;
                        }

                        return target;
                    }
                }

                return null;
            }
        }

        private List<EpsilonInstantStreamReader<T>> GetInstantStreamReaderList()
        {
            lock (this.instantStreamReaders)
            {
                return this.instantStreamReaders.ToList();
            }
        }

        private EpsilonInstantStreamReader<T> GetInstantStreamReader(RelativeTimeInterval cursorEpsilon, bool createIfNecessary)
        {
            // Check if we have a registration for this instant stream reader
            lock (this.instantStreamReaders)
            {
                EpsilonInstantStreamReader<T> instantStreamReader = this.instantStreamReaders.FirstOrDefault(isr => isr.CursorEpsilon.Equals(cursorEpsilon));
                if (instantStreamReader == default)
                {
                    if (createIfNecessary)
                    {
                        // Create the instant stream reader
                        instantStreamReader = new EpsilonInstantStreamReader<T>(cursorEpsilon);
                        this.instantStreamReaders.Add(instantStreamReader);
                    }
                    else
                    {
                        throw new ArgumentException("The instant stream reader is not registered.");
                    }
                }

                return instantStreamReader;
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // release any removed elements
            if (this.needsDisposing && e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Message<T> item in e.OldItems)
                {
                    (item.Data as IDisposable).Dispose();
                }
            }
        }

        private void OnReceiveData(T data, Envelope env)
        {
            if (!this.IsCanceled)
            {
                lock (this.bufferLock)
                {
                    this.dataBuffer.Add(new Message<T>(data, env.OriginatingTime, env.Time, env.SourceId, env.SequenceId));
                }
            }
        }

        private void OnReceiveIndex(IndexEntry index, Envelope env)
        {
            if (!this.IsCanceled)
            {
                lock (this.bufferLock)
                {
                    this.indexBuffer.Add(index);
                }
            }
        }
    }
}
