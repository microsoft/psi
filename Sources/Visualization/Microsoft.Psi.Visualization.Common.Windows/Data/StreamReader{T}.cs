// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;

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
        private IPool pool;
        private bool isCanceled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReader{T}"/> class.
        /// </summary>
        /// <param name="streamBinding">Stream binding used to indentify stream.</param>
        public StreamReader(StreamBinding streamBinding)
        {
            this.StreamBinding = streamBinding;
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
        public Type StreamAdapterType => this.StreamBinding.StreamAdapterType;

        /// <inheritdoc />
        public StreamBinding StreamBinding { get; }

        /// <inheritdoc />
        public string StreamName => this.StreamBinding.StreamName;

        /// <inheritdoc />
        public string StoreName => this.StreamBinding.StoreName;

        /// <inheritdoc />
        public string StorePath => this.StreamBinding.StorePath;

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

                this.StreamBinding.StreamAdapter?.Dispose();
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
                if (this.StreamBinding.StreamAdapter == null)
                {
                    reader.OpenStreamIndex<T>(this.StreamName, this.OnReceiveIndex);
                }
                else
                {
                    var genericOpenStreamIndex = typeof(ISimpleReader)
                        .GetMethod("OpenStreamIndex", new Type[] { typeof(string), typeof(Action<IndexEntry, Envelope>) })
                        .MakeGenericMethod(this.StreamBinding.StreamAdapter.SourceType);
                    var receiver = new Action<IndexEntry, Envelope>(this.OnReceiveIndex);
                    genericOpenStreamIndex.Invoke(reader, new object[] { this.StreamName, receiver });
                }
            }
            else
            {
                if (this.StreamBinding.StreamAdapter == null)
                {
                    reader.OpenStream<T>(this.StreamName, this.OnReceiveData, this.Allocator);
                }
                else
                {
                    dynamic dynStreamAdapater = this.StreamBinding.StreamAdapter;
                    dynamic dynAdaptedReciever = dynStreamAdapater.AdaptReceiver(new Action<T, Envelope>(this.OnReceiveData));
                    reader.OpenStream(this.StreamName, dynAdaptedReciever, dynStreamAdapater.Allocator);
                }
            }
        }

        /// <inheritdoc />
        public TDest Read<TDest>(ISimpleReader reader, IndexEntry indexEntry)
        {
            if (this.StreamBinding.StreamAdapter == null)
            {
                return reader.Read<TDest>(indexEntry);
            }
            else
            {
                var genericRead = typeof(ISimpleReader)
                    .GetMethod("Read", new Type[] { typeof(IndexEntry) })
                    .MakeGenericMethod(this.StreamBinding.StreamAdapter.SourceType);
                var src = genericRead.Invoke(reader, new object[] { indexEntry });
                var adaptData = typeof(StreamAdapter<,>)
                    .MakeGenericType(this.StreamBinding.StreamAdapter.SourceType, this.StreamBinding.StreamAdapter.DestinationType)
                    .GetMethod("AdaptData");
                return (TDest)adaptData.Invoke(this.StreamBinding.StreamAdapter, new object[] { src } );
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
