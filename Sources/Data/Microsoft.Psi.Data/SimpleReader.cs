// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Reads messages from a multi-stream store.
    /// Unlike StoreReader, this class calls delegates instead of publishing to streams,
    /// so it is unconstrained by the ordering limitations imposed by streams
    /// and can be re-used for seeking back and forth.
    /// The class is intended to be used by the UI.
    /// </summary>
    public class SimpleReader : ISimpleReader, IDisposable
    {
        private static readonly HashSet<int> OpenedStreamIds = new HashSet<int>();
        private readonly Dictionary<int, Action<BufferReader, Envelope>> outputs = new Dictionary<int, Action<BufferReader, Envelope>>();
        private readonly Dictionary<int, Action<IndexEntry, Envelope>> indexOutputs = new Dictionary<int, Action<IndexEntry, Envelope>>();
        private StoreReader reader;
        private SerializationContext context;
        private KnownSerializers serializers;
        private byte[] buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store</param>
        /// <param name="serializers">Optional set of serialization configuration (known types, serializers, known assemblies)</param>
        public SimpleReader(string name, string path, KnownSerializers serializers = null)
        {
            this.serializers = serializers;
            this.reader = new StoreReader(name, path, this.LoadMetadata);
            this.context = new SerializationContext(this.serializers);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleReader"/> class without re-loading the metadata and index files.
        /// The new reader maintains its own cursor into the data file and can be used in parallel with the one it was created from.
        /// </summary>
        /// <param name="other">An existing reader.</param>
        public SimpleReader(SimpleReader other)
        {
            this.serializers = other.serializers;
            this.reader = new StoreReader(other.reader); // copy constructor
            this.context = new SerializationContext(this.serializers);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleReader"/> class.
        /// This version expects to have Open called after construction
        /// </summary>
        public SimpleReader()
        {
        }

        /// <inheritdoc />
        public string Name => this.reader.Name;

        /// <inheritdoc />
        public string Path => this.reader.Path;

        /// <summary>
        /// Gets the set of serializers used to read and deserialize the messages in this store
        /// </summary>
        public KnownSerializers Serializers => this.serializers;

        /// <summary>
        /// Gets the set of logical storage streams in this store.
        /// </summary>
        public IEnumerable<IStreamMetadata> AvailableStreams => this.reader.AvailableStreams;

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this store, across all logical streams.
        /// </summary>
        public TimeInterval ActiveTimeRange => this.reader.ActiveTimeInterval;

        /// <inheritdoc />
        public TimeInterval OriginatingTimeRange() => this.reader.OriginatingTimeInterval;

        /// <inheritdoc />
        public void OpenStore(string name, string path, KnownSerializers serializers = null)
        {
            this.reader = new StoreReader(name, path, this.LoadMetadata);
            this.context = new SerializationContext(this.serializers);
        }

        /// <summary>
        /// Closes the reader and associated files.
        /// </summary>
        public void Dispose()
        {
            this.reader.Dispose();
        }

        /// <inheritdoc />
        public ISimpleReader OpenNew()
        {
            return new SimpleReader(this);
        }

        /// <summary>
        /// Returns a metadata descriptor for the specified storage stream.
        /// </summary>
        /// <param name="streamName">The name of the storage stream.</param>
        /// <returns>The metadata describing the specified stream.</returns>
        public PsiStreamMetadata GetMetadata(string streamName) => this.reader.GetMetadata(streamName);

        /// <summary>
        /// Checks whether the specified storage stream exist in this store.
        /// </summary>
        /// <param name="streamName">The name of the storage stream to look for.</param>
        /// <returns>True if a storage stream with the specified name exists, false otherwise</returns>
        public bool Contains(string streamName) => this.reader.Contains(streamName);

        /// <inheritdoc />
        public IStreamMetadata OpenStream<T>(string streamName, Action<T, Envelope> target, Func<T> allocator = null)
        {
            var meta = this.reader.OpenStream(streamName); // this checks for duplicates
            this.OpenStream<T>(meta, target, allocator);
            return meta;
        }

        /// <summary>
        /// Opens the specified logical storage stream for reading.
        /// </summary>
        /// <typeparam name="T">The type of messages in stream.</typeparam>
        /// <param name="streamId">The id of the storage stream to open.</param>
        /// <param name="target">The function to call for every message in this storage stream.</param>
        /// <param name="allocator">An optional allocator of messages.</param>
        /// <returns>The metadata describing the opened storage stream.</returns>
        public IStreamMetadata OpenStream<T>(int streamId, Action<T, Envelope> target, Func<T> allocator = null)
        {
            var meta = this.reader.OpenStream(streamId);
            this.OpenStream<T>(meta, target, allocator);
            return meta;
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStreamIndex<T>(string streamName, Action<IndexEntry, Envelope> target)
        {
            var meta = this.reader.OpenStream(streamName); // this checks for duplicates
            OpenedStreamIds.Add(meta.Id);
            this.indexOutputs[meta.Id] = target;
            return meta;
        }

        /// <summary>
        /// Opens the specified logical storage stream for reading, in index form.
        /// That is, only index entries are provided to the target delegate.
        /// </summary>
        /// <typeparam name="T">The type of messages in stream.</typeparam>
        /// <param name="streamId">The id of the storage stream to open.</param>
        /// <param name="target">The function to call with the index of every message in this storage stream.</param>
        /// <returns>The metadata describing the opened storage stream.</returns>
        public IStreamMetadata OpenStreamIndex<T>(int streamId, Action<IndexEntry, Envelope> target)
        {
            var meta = this.reader.OpenStream(streamId);
            OpenedStreamIds.Add(meta.Id);
            this.indexOutputs[streamId] = target;
            return meta;
        }

        /// <summary>
        /// Closes all storage streams.
        /// </summary>
        public void CloseAllStreams()
        {
            this.indexOutputs.Clear();
            this.outputs.Clear();
            this.reader.CloseAllStreams();
        }

        /// <inheritdoc />
        public void ReadAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default(CancellationToken))
        {
            var result = true;
            Envelope e;
            this.reader.Seek(descriptor.Interval, descriptor.UseOriginatingTime);
            while (result || this.reader.IsMoreDataExpected())
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    return;
                }

                result = this.reader.MoveNext(out e);
                if (result)
                {
                    if (this.indexOutputs.ContainsKey(e.SourceId))
                    {
                        var indexEntry = this.reader.ReadIndex();
                        this.indexOutputs[e.SourceId](indexEntry, e);
                    }
                    else
                    {
                        int count = this.reader.Read(ref this.buffer);
                        var bufferReader = new BufferReader(this.buffer, count);
                        this.outputs[e.SourceId](bufferReader, e);
                    }
                }
            }
        }

        /// <inheritdoc />
        public T Read<T>(IndexEntry indexEntry)
        {
            var target = default(T);
            this.Read(indexEntry, ref target);
            return target;
        }

        /// <inheritdoc />
        public void Read<T>(IndexEntry indexEntry, ref T objectToReuse)
        {
            int count = this.reader.ReadAt(indexEntry, ref this.buffer);
            var bufferReader = new BufferReader(this.buffer, count);
            var handler = this.serializers.GetHandler<T>();
            objectToReuse = this.Deserialize<T>(handler, bufferReader, objectToReuse);
        }

        private void OpenStream<T>(PsiStreamMetadata meta, Action<T, Envelope> target, Func<T> allocator = null)
        {
            OpenedStreamIds.Add(meta.Id);
            var handler = this.serializers.GetHandler<T>();
            this.outputs[meta.Id] = (br, e) => target(this.Deserialize<T>(handler, br, (allocator == null) ? default(T) : allocator()), e);
        }

        private T Deserialize<T>(SerializationHandler<T> handler, BufferReader br, T objectToReuse)
        {
            try
            {
                handler.Deserialize(br, ref objectToReuse, this.context);
            }
            catch
            {
                this.reader.EnsureMetadataUpdate();
                handler.Deserialize(br, ref objectToReuse, this.context);
            }

            this.context.Reset();
            return objectToReuse;
        }

        private void LoadMetadata(IEnumerable<Metadata> metadata, RuntimeInfo runtimeVersion)
        {
            if (this.serializers == null)
            {
                this.serializers = new KnownSerializers(runtimeVersion);
            }

            this.serializers.RegisterMetadata(metadata);
        }
    }
}
