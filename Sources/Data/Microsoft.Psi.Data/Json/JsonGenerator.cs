// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Defines a component that plays back data from a JSON store.
    /// </summary>
    public class JsonGenerator : Generator, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly JsonStoreReader reader;
        private readonly HashSet<string> streams;
        private readonly Dictionary<int, ValueTuple<object, Action<JToken, DateTime>>> emitters;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonGenerator"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides.</param>
        public JsonGenerator(Pipeline pipeline, string name, string path)
            : this(pipeline, new JsonStoreReader(name, path))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonGenerator"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="reader">The underlying store reader.</param>
        protected JsonGenerator(Pipeline pipeline, JsonStoreReader reader)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.reader = reader;
            this.streams = new HashSet<string>();
            this.emitters = new Dictionary<int, ValueTuple<object, Action<JToken, DateTime>>>();
            this.reader.Seek(ReplayDescriptor.ReplayAll);
        }

        /// <summary>
        /// Gets the name of the application that generated the persisted files, or the root name of the files.
        /// </summary>
        public string Name => this.reader.Name;

        /// <summary>
        /// Gets the directory in which the main persisted file resides.
        /// </summary>
        public string Path => this.reader.Path;

        /// <summary>
        /// Gets an enumerable of stream metadata contained in the underlying data store.
        /// </summary>
        public IEnumerable<IStreamMetadata> AvailableStreams => this.reader.AvailableStreams;

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in the underlying data store.
        /// </summary>
        public TimeInterval OriginatingTimeInterval => this.reader.OriginatingTimeInterval;

        /// <summary>
        /// Determines whether the underlying data store contains the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>true if store contains the specified stream, otherwise false.</returns>
        public bool Contains(string streamName) => this.reader.AvailableStreams.Any(av => av.Name == streamName);

        /// <inheritdoc />
        public void Dispose()
        {
            this.reader?.Dispose();
        }

        /// <summary>
        /// Gets the stream metadata for the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The stream metadata.</returns>
        public IStreamMetadata GetMetadata(string streamName) => this.reader.AvailableStreams.FirstOrDefault(av => av.Name == streamName);

        /// <summary>
        /// Opens the specified stream for reading and returns an emitter for use in the pipeline.
        /// </summary>
        /// <typeparam name="T">Type of data in underlying stream.</typeparam>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The newly created emmitte that generates messages from the stream of type <typeparamref name="T"/>.</returns>
        public Emitter<T> OpenStream<T>(string streamName)
        {
            // if stream already opened, return emitter
            if (this.streams.Contains(streamName))
            {
                var m = this.GetMetadata(streamName);
                var e = this.emitters[m.Id];

                // if the types don't match, invalid cast exception is the appropriate error
                return (Emitter<T>)e.Item1;
            }

            // open stream in underlying reader
            var metadata = this.reader.OpenStream(streamName);

            // register this stream with the store catalog
            this.pipeline.ConfigurationStore.Set(Store.StreamMetadataNamespace, streamName, metadata);

            // create emitter
            var emitter = this.pipeline.CreateEmitter<T>(this, streamName);
            this.emitters[metadata.Id] = ValueTuple.Create<Emitter<T>, Action<JToken, DateTime>>(
                emitter,
                (token, originatingTime) =>
                {
                    var t = token.ToObject<T>();
                    emitter.Post(t, originatingTime);
                });

            return emitter;
        }

        /// <summary>
        /// GenerateNext is called by the Generator base class when the next sample should be read.
        /// </summary>
        /// <param name="currentTime">The originating time of the message that triggered the current call to GenerateNext.</param>
        /// <returns>The originating time at which to read the next sample.</returns>
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            Envelope env;
            if (this.reader.MoveNext(out env))
            {
                this.reader.Read(out JToken data);
                this.emitters[env.SourceId].Item2(data, env.OriginatingTime);
                return env.OriginatingTime;
            }

            return DateTime.MaxValue;
        }
    }
}
