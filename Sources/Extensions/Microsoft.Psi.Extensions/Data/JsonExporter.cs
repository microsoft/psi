// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Data
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi.Components;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Component that writes messages to a multi-stream JSON store.
    /// </summary>
    public class JsonExporter : IDisposable
    {
        private readonly JsonStoreWriter writer;
        private readonly Merger<Message<JToken>, string> merger;
        private readonly Pipeline pipeline;
        private readonly Receiver<Message<JToken>> writerInput;
        private readonly ManualResetEvent throttle = new ManualResetEvent(true);

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonExporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline that owns this instance.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store</param>
        /// <param name="createSubdirectory">If true, a numbered sub-directory is created for this store</param>
        public JsonExporter(Pipeline pipeline, string name, string path, bool createSubdirectory = true)
            : this(pipeline, new JsonStoreWriter(name, path, createSubdirectory))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonExporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline that owns this instance.</param>
        /// <param name="writer">The underlying store writer.</param>
        protected JsonExporter(Pipeline pipeline, JsonStoreWriter writer)
        {
            this.pipeline = pipeline;
            this.writer = writer;
            this.merger = new Merger<Message<JToken>, string>(pipeline);
            this.writerInput = pipeline.CreateReceiver<Message<JToken>>(this, (m, e) => this.writer.Write(m.Data, m.Envelope), nameof(this.writerInput));
            this.merger.Select(this.ThrottledMessages).PipeTo(this.writerInput, DeliveryPolicy.Unlimited);
        }

        /// <summary>
        /// Gets the name of the store being written to
        /// </summary>
        public string Name => this.writer.Name;

        /// <summary>
        /// Gets the path to the store being written to if the store is persisted to disk, or null if the store is volatile.
        /// </summary>
        public string Path => this.writer.Path;

        /// <summary>
        /// Closes the store.
        /// </summary>
        public void Dispose()
        {
            this.writer.Dispose();
        }

        /// <summary>
        /// Writes the specified stream to this multi-stream store.
        /// </summary>
        /// <typeparam name="T">The type of messages in the stream</typeparam>
        /// <param name="source">The source stream to write</param>
        /// <param name="name">The name of the persisted stream.</param>
        /// <param name="policy">An optional delivery policy</param>
        public void Write<T>(Emitter<T> source, string name, DeliveryPolicy policy = null)
        {
            // add another input to the merger to hook up the serializer to
            // and check for duplicate names in the process
            var mergeInput = this.merger.Add(name);

            // name the stream if it's not already named
            source.Name = source.Name ?? name;

            // tell the writer to write the serialized stream
            var metadata = this.writer.OpenStream(source.Id, name, typeof(T).AssemblyQualifiedName);

            // register this stream with the store catalog
            this.pipeline.ConfigurationStore.Set(Store.StreamMetadataNamespace, name, metadata);

            // hook up the serializer
            var serializer = new JsonSerializerComponent<T>(this.pipeline);
            serializer.PipeTo(mergeInput, DeliveryPolicy.Immediate);
            source.PipeTo(serializer, policy ?? DeliveryPolicy.Unlimited);
        }

        /// <summary>
        /// Writes the specified stream to this multi-stream store.
        /// </summary>
        /// <param name="source">The source stream to write</param>
        /// <param name="metadata">The stream metadata of the stream.</param>
        /// <param name="policy">An optional delivery policy</param>
        internal void Write(Emitter<Message<JToken>> source, JsonStreamMetadata metadata, DeliveryPolicy policy = null)
        {
            var mergeInput = this.merger.Add(metadata.Name); // this checks for duplicates
            this.writer.OpenStream(metadata);
            Operators.PipeTo(source, mergeInput, policy);
        }

        private Message<JToken> ThrottledMessages(ValueTuple<string, Message<Message<JToken>>> messages)
        {
            this.throttle.WaitOne();
            return messages.Item2.Data;
        }

        private sealed class JsonSerializerComponent<T> : ConsumerProducer<T, Message<JToken>>
        {
            public JsonSerializerComponent(Pipeline pipeline)
                : base(pipeline)
            {
            }

            protected override void Receive(T data, Envelope e)
            {
                var token = JToken.FromObject(data);
                var resultMsg = Message.Create(token, e);
                this.Out.Post(resultMsg, e.OriginatingTime);
            }
        }
    }
}
