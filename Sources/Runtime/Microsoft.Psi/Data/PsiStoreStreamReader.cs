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
    /// Implements a reader of multiple streams of typed messages from a single store.
    /// </summary>
    public sealed class PsiStoreStreamReader : IStreamReader
    {
        private readonly Dictionary<int, List<Delegate>> targets = new Dictionary<int, List<Delegate>>();
        private readonly Dictionary<int, Action<BufferReader, Envelope>> outputs = new Dictionary<int, Action<BufferReader, Envelope>>();
        private readonly Dictionary<int, Action<IndexEntry, Envelope>> indexOutputs = new Dictionary<int, Action<IndexEntry, Envelope>>();

        private SerializationContext context;
        private byte[] buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStoreStreamReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        public PsiStoreStreamReader(string name, string path)
        {
            this.PsiStoreReader = new PsiStoreReader(name, path, this.LoadMetadata);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStoreStreamReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="defaultStartTime">Default start time (unused).</param>
        public PsiStoreStreamReader(string name, string path, DateTime defaultStartTime)
            : this(name, path)
        {
        }

        private PsiStoreStreamReader(PsiStoreStreamReader other)
        {
            this.PsiStoreReader = new PsiStoreReader(other.PsiStoreReader); // copy constructor
            this.context = new SerializationContext(other.context?.Serializers);
        }

        /// <inheritdoc />
        public IEnumerable<IStreamMetadata> AvailableStreams => this.PsiStoreReader.AvailableStreams;

        /// <inheritdoc />
        public string Name => this.PsiStoreReader.Name;

        /// <inheritdoc />
        public string Path => this.PsiStoreReader.Path;

        /// <inheritdoc />
        public TimeInterval MessageCreationTimeInterval => this.PsiStoreReader.MessageCreationTimeInterval;

        /// <inheritdoc />
        public TimeInterval MessageOriginatingTimeInterval => this.PsiStoreReader.MessageOriginatingTimeInterval;

        /// <summary>
        /// Gets underlying PsiStoreReader (internal only, not part of IStreamReader interface).
        /// </summary>
        internal PsiStoreReader PsiStoreReader { get; }

        /// <inheritdoc />
        public void Seek(TimeInterval interval, bool useOriginatingTime = false)
        {
            this.PsiStoreReader.Seek(interval, useOriginatingTime);
        }

        /// <inheritdoc />
        public bool MoveNext(out Envelope envelope)
        {
            if (this.PsiStoreReader.MoveNext(out envelope))
            {
                if (this.indexOutputs.ContainsKey(envelope.SourceId))
                {
                    var indexEntry = this.PsiStoreReader.ReadIndex();
                    this.indexOutputs[envelope.SourceId](indexEntry, envelope);
                }
                else
                {
                    int count = this.PsiStoreReader.Read(ref this.buffer);
                    var bufferReader = new BufferReader(this.buffer, count);
                    this.outputs[envelope.SourceId](bufferReader, envelope);
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool IsLive()
        {
            return this.PsiStoreReader.IsMoreDataExpected();
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStream<T>(string name, Action<T, Envelope> target, Func<T> allocator = null)
        {
            var meta = this.PsiStoreReader.OpenStream(name); // this checks for duplicates
            this.OpenStream<T>(meta, target, allocator);
            return meta;
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStreamIndex<T>(string streamName, Action<Func<IStreamReader, T>, Envelope> target)
        {
            var meta = this.PsiStoreReader.OpenStream(streamName); // this checks for duplicates

            // Target `indexOutputs` are later called when data is read by MoveNext or ReadAll (see InvokeTargets).
            this.indexOutputs[meta.Id] = new Action<IndexEntry, Envelope>((indexEntry, envelope) =>
            {
                // Index targets are given the message Envelope and a Func by which to retrieve the message data.
                // This Func may be held as a kind of "index" later called to retrieve the data. It may be called,
                // given the current IStreamReader or a new `reader` instance against the same store.
                // The Func is a closure over the `indexEntry` needed for retrieval by `Read<T>(...)`
                // but this implementation detail remain opaque to users of the reader.
                target(new Func<IStreamReader, T>(reader => ((PsiStoreStreamReader)reader).Read<T>(indexEntry)), envelope);
            });
            return meta;
        }

        /// <inheritdoc />
        public IStreamMetadata GetStreamMetadata(string name)
        {
            return this.PsiStoreReader.GetMetadata(name);
        }

        /// <inheritdoc />
        public T GetSupplementalMetadata<T>(string streamName)
        {
            var meta = this.PsiStoreReader.GetMetadata(streamName);
            return meta.GetSupplementalMetadata<T>(this.context.Serializers);
        }

        /// <inheritdoc />
        public bool ContainsStream(string name)
        {
            return this.PsiStoreReader.Contains(name);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.PsiStoreReader.Dispose();
        }

        /// <inheritdoc />
        public IStreamReader OpenNew()
        {
            return new PsiStoreStreamReader(this);
        }

        /// <inheritdoc />
        public void ReadAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default)
        {
            var result = true;
            Envelope e;
            this.PsiStoreReader.Seek(descriptor.Interval, true);
            while (result || this.PsiStoreReader.IsMoreDataExpected())
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    return;
                }

                result = this.PsiStoreReader.MoveNext(out e);
                if (result)
                {
                    if (this.indexOutputs.ContainsKey(e.SourceId))
                    {
                        var indexEntry = this.PsiStoreReader.ReadIndex();
                        this.indexOutputs[e.SourceId](indexEntry, e);
                    }
                    else
                    {
                        int count = this.PsiStoreReader.Read(ref this.buffer);
                        var bufferReader = new BufferReader(this.buffer, count);
                        this.outputs[e.SourceId](bufferReader, e);
                    }
                }
            }
        }

        internal KnownSerializers GetSerializers()
        {
            return this.context.Serializers;
        }

        /// <summary>
        /// Read message data at the given index.
        /// </summary>
        /// <typeparam name="T">The type of message data.</typeparam>
        /// <param name="indexEntry">Index entry describing the location of a particular message.</param>
        /// <returns>Message data.</returns>
        private T Read<T>(IndexEntry indexEntry)
        {
            var target = default(T);
            int count = this.PsiStoreReader.ReadAt(indexEntry, ref this.buffer);
            var bufferReader = new BufferReader(this.buffer, count);
            var handler = this.context.Serializers.GetHandler<T>();
            target = this.Deserialize<T>(handler, bufferReader, default(Envelope) /* only used by raw */, false, false, target, null /* only used by dynamic */, null /* only used by dynamic */);
            return target;
        }

        /// <summary>
        /// Initializes the serialization subsystem with the metadata from the store.
        /// </summary>
        /// <param name="metadata">The collection of metadata entries from the store catalog.</param>
        /// <param name="runtimeVersion">The version of the runtime that produced the store.</param>
        private void LoadMetadata(IEnumerable<Metadata> metadata, RuntimeInfo runtimeVersion)
        {
            if (this.context == null)
            {
                this.context = new SerializationContext(new KnownSerializers(runtimeVersion));
            }

            this.context.Serializers.RegisterMetadata(metadata);
        }

        private void OpenStream<T>(IStreamMetadata meta, Action<T, Envelope> target, Func<T> allocator = null)
        {
            // Get the deserialization handler for this stream type
            var handler = this.context.Serializers.GetHandler<T>();

            var isDynamic = typeof(T).FullName == typeof(object).FullName;
            var isRaw = typeof(T).FullName == typeof(Message<BufferReader>).FullName;

            if (!isDynamic && !isRaw)
            {
                // check that the requested type matches the stream type
                var streamType = meta.TypeName;
                var handlerType = handler.Name;
                if (streamType != handlerType)
                {
                    // check if the handler is able to handle the stream type
                    if (handlerType != streamType)
                    {
                        if (this.context.Serializers.Schemas.TryGetValue(streamType, out var streamTypeSchema) &&
                            this.context.Serializers.Schemas.TryGetValue(handlerType, out var handlerTypeSchema))
                        {
                            // validate compatibility - will throw if types are incompatible
                            handlerTypeSchema.ValidateCompatibleWith(streamTypeSchema);
                        }
                    }
                }
            }

            // If there's no list of targets for this stream, create it now
            if (!this.targets.ContainsKey(meta.Id))
            {
                this.targets[meta.Id] = new List<Delegate>();
            }

            // Add the target to the list to call when this stream has new data
            this.targets[meta.Id].Add(target);

            // Update the code to execute when this stream receives new data
            this.outputs[meta.Id] = (br, e) =>
            {
                // Deserialize the data
                var data = this.Deserialize<T>(handler, br, e, isDynamic, isRaw, (allocator == null) ? default(T) : allocator(), meta.TypeName, this.context.Serializers.Schemas);

                // Call each of the targets
                foreach (Delegate action in this.targets[meta.Id])
                {
                    (action as Action<T, Envelope>)(data, e);
                }
            };
        }

        private T Deserialize<T>(SerializationHandler<T> handler, BufferReader br, Envelope env, bool isDynamic, bool isRaw, T objectToReuse, string typeName, IDictionary<string, TypeSchema> schemas)
        {
            if (isDynamic)
            {
                var deserializer = new DynamicMessageDeserializer(typeName, schemas);
                objectToReuse = deserializer.Deserialize(br);
            }
            else if (isRaw)
            {
                objectToReuse = (T)(object)Message.Create(br, env);
            }
            else
            {
                int currentPosition = br.Position;
                try
                {
                    handler.Deserialize(br, ref objectToReuse, this.context);
                }
                catch
                {
                    this.PsiStoreReader.EnsureMetadataUpdate();
                    br.Seek(currentPosition);
                    handler.Deserialize(br, ref objectToReuse, this.context);
                }
            }

            this.context.Reset();
            return objectToReuse;
        }
    }
}
