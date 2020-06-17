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
    public sealed class Importer : Subpipeline, IDisposable
    {
        private readonly StoreReader reader;
        private readonly MessageImporter msgImporter;
        private readonly Splitter<Message<BufferReader>, int> splitter;
        private readonly Dictionary<string, object> streams = new Dictionary<string, object>();
        private readonly Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="Importer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline that owns this instance.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to open a volatile data store.</param>
        public Importer(Pipeline pipeline, string name, string path)
            : base(pipeline, $"{nameof(Importer)}[{name}]")
        {
            this.reader = new StoreReader(name, path, this.LoadMetadata);
            this.pipeline = pipeline;
            this.msgImporter = new MessageImporter(this, this.reader);
            this.splitter = new Splitter<Message<BufferReader>, int>(this, (msg, e) => msg.Envelope.SourceId);
            this.msgImporter.PipeTo(this.splitter.In, DeliveryPolicy.SynchronousOrThrottle);
        }

        /// <summary>
        /// Gets the path of the store, or null if this is a volatile store.
        /// </summary>
        public string Path => this.reader.Path;

        /// <summary>
        /// Gets the set of types that this Importer can deserialize.
        /// Types can be added or re-mapped using the <see cref="KnownSerializers.Register{T}(string)"/> method.
        /// </summary>
        public KnownSerializers Serializers { get; private set; }

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

        /// <summary>
        /// Closes the store and disposes of the current instance.
        /// </summary>
        public override void Dispose()
        {
            this.reader.Dispose();
        }

        /// <summary>
        /// Returns the metadata for a specified storage stream.
        /// </summary>
        /// <param name="streamName">The name of the storage stream.</param>
        /// <returns>The metadata associated with the storage stream.</returns>
        public PsiStreamMetadata GetMetadata(string streamName) => this.reader.GetMetadata(streamName);

        /// <summary>
        /// Returns the supplemental metadata for a specified storage stream.
        /// </summary>
        /// <typeparam name="T">Type of supplemental metadata.</typeparam>
        /// <param name="streamName">The name of the storage stream.</param>
        /// <returns>The metadata associated with the storage stream.</returns>
        public T GetSupplementalMetadata<T>(string streamName)
        {
            var meta = this.reader.GetMetadata(streamName);
            return meta.GetSupplementalMetadata<T>(this.Serializers);
        }

        /// <summary>
        /// Indicates whether the store contains the specified storage stream.
        /// </summary>
        /// <param name="streamName">The name of the storage stream.</param>
        /// <returns>True if the store contains a storage stream with the specified name, false otherwise.</returns>
        public bool Contains(string streamName) => this.reader.Contains(streamName);

        /// <summary>
        /// Copies the specified storage stream to an exporter without deserializing the data.
        /// </summary>
        /// <param name="streamName">The name of the storage stream to copy.</param>
        /// <param name="writer">The store to copy to.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        public void CopyStream(string streamName, Exporter writer, DeliveryPolicy<Message<BufferReader>> deliveryPolicy = null)
        {
            // create the copy pipeline
            var meta = this.reader.GetMetadata(streamName);
            var raw = this.OpenRawStream(meta);
            writer.Write(raw, meta, deliveryPolicy);
        }

        /// <summary>
        /// Opens a store by creating a dummy object of type T that exposes emitters
        /// that match to each stream in the store. This is useful for reading back
        /// stores that were previously created via
        ///     Exporter.Write(T sensor, WhichStreams streamsToEmit, string[] streamNames)
        /// which is useful for saving streams from a sensor and then calling this
        /// method to play them back as if they were an attached device.
        /// </summary>
        /// <typeparam name="T">Type of object through which to expose the streams as emitters.</typeparam>
        /// <returns>Newly constructed object of type T.</returns>
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
        /// <param name="streamName">The name of the storage stream to open.</param>
        /// <param name="reusableInstance">An optional instance to reuse (as a buffer) when deserializing the data.</param>
        /// <returns>A stream that publishes the data read from the store.</returns>
        public IProducer<T> OpenStream<T>(string streamName, T reusableInstance = default(T))
        {
            return this.OpenStream<T>(streamName, new DeserializerComponent<T>(this, this.Serializers, reusableInstance));
        }

        /// <summary>
        /// Opens the specified storage stream as dynamic for reading and returns a stream instance that can be used to consume the messages.
        /// The returned stream will publish data read from the store once the pipeline is running.
        /// </summary>
        /// <remarks>Messages are deserialized as dynamic primitives and/or ExpandoObject of dynamic.</remarks>
        /// <param name="streamName">The name of the storage stream to open.</param>
        /// <returns>A stream of dynamic that publishes the data read from the store.</returns>
        public IProducer<dynamic> OpenDynamicStream(string streamName)
        {
            return this.OpenStream<dynamic>(streamName, new DynamicDeserializerComponent(this, this.reader.OpenStream(streamName).TypeName, this.Serializers.Schemas), false);
        }

        /// <summary>
        /// Opens the specified storage stream as raw `Message` of `BufferReader` for reading and returns a stream instance that can be used to consume the messages.
        /// The returned stream will publish data read from the store once the pipeline is running.
        /// </summary>
        /// <remarks>Messages are not deserialized.</remarks>
        /// <param name="meta">The meta of the storage stream to open.</param>
        /// <returns>A stream of raw messages that publishes the data read from the store.</returns>
        internal Emitter<Message<BufferReader>> OpenRawStream(PsiStreamMetadata meta)
        {
            if (meta.OriginatingLifetime != null && !meta.OriginatingLifetime.IsEmpty && meta.OriginatingLifetime.IsFinite)
            {
                // propose a replay time that covers the stream lifetime
                this.ProposeReplayTime(meta.OriginatingLifetime);
            }

            this.reader.OpenStream(meta); // this checks for duplicates but bypasses type checks
            return this.splitter.Add(meta.Id);
        }

        private IProducer<T> OpenStream<T>(string streamName, ConsumerProducer<Message<BufferReader>, T> deserializer, bool checkType = true)
        {
            if (this.streams.TryGetValue(streamName, out object stream))
            {
                return (IProducer<T>)stream; // if the types don't match, invalid cast exception is the appropriate error
            }

            var meta = this.reader.OpenStream(streamName);

            if (checkType)
            {
                // check that the requested type matches the stream type
                var streamType = meta.TypeName;
                var requestedType = TypeSchema.GetContractName(typeof(T), this.Serializers.RuntimeVersion);
                if (streamType != requestedType)
                {
                    // check if the handler maps the stream type to the requested type
                    var handler = this.Serializers.GetHandler<T>();
                    if (handler.Name != streamType)
                    {
                        if (this.Serializers.Schemas.TryGetValue(streamType, out var streamTypeSchema) &&
                            this.Serializers.Schemas.TryGetValue(requestedType, out var requestedTypeSchema))
                        {
                            // validate compatibility - will throw if types are incompatible
                            streamTypeSchema.ValidateCompatibleWith(requestedTypeSchema);
                        }
                    }
                }
            }

            if (meta.OriginatingLifetime != null && !meta.OriginatingLifetime.IsEmpty && meta.OriginatingLifetime.IsFinite)
            {
                // propose a replay time that covers the stream lifetime
                this.ProposeReplayTime(meta.OriginatingLifetime);
            }

            // register this stream with the store catalog
            this.pipeline.ConfigurationStore.Set(Store.StreamMetadataNamespace, streamName, meta);

            // collate the raw messages by their stream IDs
            var splitterOut = this.splitter.Add(meta.Id);
            splitterOut.PipeTo(deserializer, DeliveryPolicy.Unlimited);

            // preserve the envelope of the deserialized message in the output connector
            var outConnector = new Connector<T>(this, this.pipeline, $"connectorOut{streamName}", preserveEnvelope: true);

            deserializer
                .Process<T, T>(
                    (msg, env, emitter) =>
                    {
                        // do not deliver messages past the stream closing time
                        if (meta.Closed == default || env.OriginatingTime <= meta.Closed)
                        {
                            // call Deliver rather than Post to preserve the original envelope
                            emitter.Deliver(msg, env);
                        }
                    }, DeliveryPolicy.SynchronousOrThrottle)
                .PipeTo(outConnector, DeliveryPolicy.SynchronousOrThrottle);

            outConnector.Out.Name = streamName;
            this.streams[streamName] = outConnector.Out;
            return outConnector.Out;
        }

        /// <summary>
        /// Initializes the serialization subsystem with the metadata from the store.
        /// </summary>
        /// <param name="metadata">The collection of metadata entries from the store catalog.</param>
        /// <param name="runtimeVersion">The version of the runtime that produced the store.</param>
        private void LoadMetadata(IEnumerable<Metadata> metadata, RuntimeInfo runtimeVersion)
        {
            if (this.Serializers == null)
            {
                this.Serializers = new KnownSerializers(runtimeVersion);
            }

            this.Serializers.RegisterMetadata(metadata);
        }
    }
}
