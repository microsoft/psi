// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a collection of streams.
    /// </summary>
    public class StreamCollection
    {
        private readonly Dictionary<string, (Type Type, IEmitter Emitter)> emitters = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamCollection"/> class.
        /// </summary>
        public StreamCollection()
        {
        }

        private StreamCollection(Dictionary<string, (Type Type, IEmitter Emitter)> emitters)
            => this.emitters = emitters;

        /// <summary>
        /// Gets the stream with the specified name from the collection, or null if it does not exist.
        /// </summary>
        /// <typeparam name="T">The type of the stream data.</typeparam>
        /// <param name="name">The stream name.</param>
        /// <returns>The stream with the specified name, or null if it does not exist.</returns>
        public IProducer<T> GetOrDefault<T>(string name)
            => this.emitters.ContainsKey(name) ? this.emitters[name].Emitter as IProducer<T> : null;

        /// <summary>
        /// Adds a stream to the collection.
        /// </summary>
        /// <typeparam name="T">The type of the stream data.</typeparam>
        /// <param name="stream">The stream to add.</param>
        /// <param name="name">The name of the stream.</param>
        public void Add<T>(IProducer<T> stream, string name)
            => this.emitters.Add(name, (typeof(T), stream.Out));

        /// <summary>
        /// Writes the streams in the collection to the specified exporter.
        /// </summary>
        /// <param name="exporter">The exporter to write to.</param>
        /// <returns>The stream collection.</returns>
        public StreamCollection Write(Exporter exporter)
            => this.Write(null, exporter);

        /// <summary>
        /// Writes the streams in the collection to the specified exporter with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to prepend to the stream names.</param>
        /// <param name="exporter">The exporter to write to.</param>
        /// <returns>The stream collection.</returns>
        public StreamCollection Write(string prefix, Exporter exporter)
        {
            prefix = string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix}.";
            var writeMethod = typeof(Exporter).GetMethods().Where(m => m.Name == nameof(Exporter.Write) && m.GetGenericArguments().Count() == 1).First();
            foreach (var name in this.emitters.Keys)
            {
                writeMethod
                    .MakeGenericMethod([this.emitters[name].Type])
                    .Invoke(exporter, new object[] { this.emitters[name].Emitter, $"{prefix}{name}", false, null });
            }

            return this;
        }

        /// <summary>
        /// Writes the streams in the collection to another stream collection.
        /// </summary>
        /// <param name="streamCollection">The stream collection to write to.</param>
        /// <returns>The stream collection.</returns>
        public StreamCollection Write(StreamCollection streamCollection)
            => this.Write(null, streamCollection);

        /// <summary>
        /// Writes the streams in the collection to another stream collection with the specified prefix.
        /// </summary>
        /// <param name="prefix">The prefix to prepend to the stream names.</param>
        /// <param name="streamCollection">The stream collection to write to.</param>
        /// <returns>The stream collection.</returns>
        public StreamCollection Write(string prefix, StreamCollection streamCollection)
        {
            prefix = string.IsNullOrEmpty(prefix) ? string.Empty : $"{prefix}.";
            foreach (var name in this.emitters.Keys)
            {
                streamCollection.emitters.Add($"{prefix}{name}", this.emitters[name]);
            }

            return this;
        }

        /// <summary>
        /// Bridges the streams in the collection to another pipeline.
        /// </summary>
        /// <param name="pipeline">The target pipeline.</param>
        /// <returns>The bridged stream collection.</returns>
        public StreamCollection BridgeTo(Pipeline pipeline)
        {
            var bridgedEmitters = new Dictionary<string, (Type Type, IEmitter Emitter)>();
            var bridgeToMethod = typeof(Psi.Operators).GetMethod(nameof(Operators.BridgeTo));
            foreach (var name in this.emitters.Keys)
            {
                // Construct the bridged emitter via reflection
                var bridgedEmitter = (bridgeToMethod
                    .MakeGenericMethod([this.emitters[name].Type])
                    .Invoke(null, new object[] { this.emitters[name].Emitter, pipeline, null, null }) as dynamic).Out as IEmitter;

                bridgedEmitters.Add(name, (this.emitters[name].Type, bridgedEmitter));
            }

            return new (bridgedEmitters);
        }
    }
}
