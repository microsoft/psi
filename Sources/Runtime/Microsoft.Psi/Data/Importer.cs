// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Reads messages from a multi-stream store, either at the full speed allowed by available resources
    /// or at the desired rate specified by the <see cref="Pipeline"/>.
    /// Instances of this class can be created using the <see cref="Store.Open"/> method.
    /// The store metadata is available immediately after open (before the pipeline is running) via the <see cref="AvailableStreams"/> property.
    /// </summary>
    public sealed class Importer : IStartable, IDisposable
    {
        private readonly Receiver<bool> loopBack;
        private readonly Emitter<bool> next;
        private readonly Emitter<Message<BufferReader>> output;
        private readonly StoreReader reader;
        private readonly Splitter<Message<BufferReader>, int> splitter;
        private readonly Dictionary<string, object> streams = new Dictionary<string, object>();
        private readonly Pipeline pipeline;
        private Action onCompleted;
        private bool stopping;
        private byte[] buffer;
        private KnownSerializers serializers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Importer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline that owns this instance.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to open a volatile data store</param>
        public Importer(Pipeline pipeline, string name, string path)
        {
            this.pipeline = pipeline;
            this.reader = new StoreReader(name, path, this.LoadMetadata);
            if (this.reader.ActiveTimeInterval != null && !this.reader.ActiveTimeInterval.IsEmpty && this.reader.ActiveTimeInterval.IsFinite)
            {
                pipeline.ProposeReplayTime(this.reader.ActiveTimeInterval, this.reader.OriginatingTimeInterval);
            }

            this.next = pipeline.CreateEmitter<bool>(this, nameof(this.next));
            this.loopBack = pipeline.CreateReceiver<bool>(this, this.Next, nameof(this.loopBack));
            this.next.PipeTo(this.loopBack, DeliveryPolicy.LatestMessage);
            this.splitter = new Splitter<Message<BufferReader>, int>(pipeline, (msg, e) => msg.Envelope.SourceId);
            this.output = pipeline.CreateEmitter<Message<BufferReader>>(this, nameof(this.output));
            this.output.PipeTo(this.splitter.In, DeliveryPolicy.Immediate);
        }

        /// <summary>
        /// Gets the name of the store.
        /// </summary>
        public string Name => this.reader.Name;

        /// <summary>
        /// Gets the path of the store, or null if this is a volatile store.
        /// </summary>
        public string Path => this.reader.Path;

        /// <summary>
        /// Gets the set of types that this Importer can deserialize.
        /// Types can be added or re-mapped using the <see cref="KnownSerializers.Register{T}(string)"/> method.
        /// </summary>
        public KnownSerializers Serializers => this.serializers;

        /// <summary>
        /// Gets the metadata of all the storage streams in this store.
        /// </summary>
        public IEnumerable<PsiStreamMetadata> AvailableStreams => this.reader.AvailableStreams;

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this store, across all storage streams.
        /// </summary>
        public TimeInterval ActiveTimeInterval => this.reader.ActiveTimeInterval;

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this store, across all storage streams.
        /// </summary>
        public TimeInterval OriginatingTimeInterval => this.reader.OriginatingTimeInterval;

        /// <summary>
        /// Gets the version of the Psi runtime that generated this store.
        /// </summary>
        public RuntimeInfo SourceRuntimeInfo => this.reader.RuntimeVersion;

        /// <inheritdoc />
        void IStartable.Start(Action onCompleted, ReplayDescriptor descriptor)
        {
            this.onCompleted = onCompleted;
            this.reader.Seek(descriptor.Interval, descriptor.UseOriginatingTime);
            this.next.Post(true, descriptor.Start);
        }

        /// <inheritdoc />
        void IStartable.Stop()
        {
            this.stopping = true;
        }

        /// <summary>
        /// Closes the store and disposes of the current instance.
        /// </summary>
        public void Dispose()
        {
            this.reader.Dispose();
        }

        /// <summary>
        /// Returns the metadata for a specified storage stream.
        /// </summary>
        /// <param name="streamName">The name of the storage stream</param>
        /// <returns>The metadata associated with the storage stream</returns>
        public PsiStreamMetadata GetMetadata(string streamName) => this.reader.GetMetadata(streamName);

        /// <summary>
        /// Indicates whether the store contains the specified storage stream.
        /// </summary>
        /// <param name="streamName">The name of the storage stream</param>
        /// <returns>True if the store contains a storage stream with the specified name, false otherwise.</returns>
        public bool Contains(string streamName) => this.reader.Contains(streamName);

        /// <summary>
        /// Copies the specified storage stream to an exporter without deserializing the data.
        /// </summary>
        /// <param name="streamName">The name of the storage stream to copy</param>
        /// <param name="writer">The store to copy to</param>
        /// <param name="policy">An optional delivery policy</param>
        public void CopyStream(string streamName, Exporter writer, DeliveryPolicy policy = null)
        {
            var meta = this.reader.GetMetadata(streamName);
            this.reader.OpenStream(meta); // this checks for duplicates but bypasses type checks

            // create the copy pipeline
            var splitterOut = this.splitter.Add(meta.Id);
            writer.Write(splitterOut, meta, policy);
        }

        /// <summary>
        /// Opens a store by creating a dummy object of type T that exposes emitters
        /// that match to each stream in the store. This is useful for reading back
        /// stores that were previously created via
        ///     Exporter.Write(T sensor, WhichStreams streamsToEmit, string[] streamNames)
        /// which is useful for saving streams from a sensor and then calling this
        /// method to play them back as if they were an attached device.
        /// </summary>
        /// <typeparam name="T">Type of object through which to expose the streams as emitters</typeparam>
        /// <returns>Newly constructed object of type T</returns>
        public T OpenAs<T>()
        {
            var target = (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
            var objType = typeof(T);
            var objProps = objType.GetProperties();
            foreach (var f in objProps)
            {
                var objPropType = f.PropertyType;
                if (objPropType.GetInterface(nameof(IEmitter)) != null)
                {
                    if (this.Contains(f.Name))
                    {
                        var openStream = typeof(Importer).GetMethod(nameof(Importer.OpenStream)).MakeGenericMethod(objPropType.GetGenericArguments());
                        object[] storeArgs = { f.Name, default(T) };
                        var producer = openStream.Invoke(this, storeArgs);
                        f.SetValue(target, producer);
                    }
                }
            }

            return target;
        }

        /// <summary>
        /// Opens the specified storage stream for reading and returns a stream instance that can be used to consume the messages.
        /// The returned stream will publish data read from the store once the pipeline is running.
        /// </summary>
        /// <typeparam name="T">The expected type of the storage stream to open.
        /// This type will be used to deserialize the stream messages.</typeparam>
        /// <param name="streamName">The name of the storage stream to open</param>
        /// <param name="reusableInstance">An optional instance to reuse (as a buffer) when deserializing the data</param>
        /// <returns>A stream that publishes the data read from the store.</returns>
        public IProducer<T> OpenStream<T>(string streamName, T reusableInstance = default(T))
        {
            if (this.streams.TryGetValue(streamName, out object stream))
            {
                return (IProducer<T>)stream; // if the types don't match, invalid cast exception is the appropriate error
            }

            var meta = this.reader.OpenStream(streamName);

            // register this stream with the store catalog
            this.pipeline.ConfigurationStore.Set(Store.StreamMetadataNamespace, streamName, meta);

            // create the deserialization sub-pipeline (and validate that we can deserialize this stream)
            var splitterOut = this.splitter.Add(meta.Id);
            var deserializer = new DeserializerComponent<T>(this.pipeline, this.serializers, reusableInstance);
            deserializer.Out.Name = streamName;
            splitterOut.PipeTo(deserializer, DeliveryPolicy.Immediate);
            this.streams[streamName] = deserializer.Out;
            return deserializer.Out;
        }

        /// <summary>
        /// Attempts to move the reader to the next message (across all logical storage streams).
        /// </summary>
        /// <param name="moreDataPromised">Indicates whether an absence of messages should be reported as the end of the store</param>
        /// <param name="env">The envelope of the last message we read</param>
        private void Next(bool moreDataPromised, Envelope env)
        {
            if (this.stopping)
            {
                return;
            }

            Envelope e;
            var result = this.reader.MoveNext(out e);
            if (result)
            {
                int count = this.reader.Read(ref this.buffer);
                var bufferReader = new BufferReader(this.buffer, count);

                // we want messages to be scheduled and delivered based on their original creation time, not originating time
                // the check below is just to ensure we don't fail because of some timing issue when writing the data (since there is no ordering guarantee across streams)
                // note that we are posting a message of a message, and once the outer message is stripped by the splitter, the inner message will still have the correct originating time
                var nextTime = (env.OriginatingTime > e.Time) ? env.OriginatingTime : e.Time;
                this.output.Post(Message.Create(bufferReader, e), nextTime);
                this.next.Post(true, nextTime);
            }
            else
            {
                // retry at least once, even if there is no active writer
                bool willHaveMoreData = this.reader.IsMoreDataExpected();
                if (willHaveMoreData || moreDataPromised)
                {
                    this.next.Post(willHaveMoreData, env.OriginatingTime);
                }
                else
                {
                    this.onCompleted();
                }
            }
        }

        /// <summary>
        /// Initializes the serialization subsystem with the metadata from the store
        /// </summary>
        /// <param name="metadata">The collection of metadata entries from the store catalog</param>
        /// <param name="runtimeVersion">The version of the runtime that produced the store.</param>
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
