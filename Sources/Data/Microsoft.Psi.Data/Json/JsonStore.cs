// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Json
{
    /// <summary>
    /// Provides static methods to access multi-stream JSON stores.
    /// </summary>
    public static class JsonStore
    {
        /// <summary>
        /// Creates a new multi-stream JSON store and returns an <see cref="JsonExporter"/> instance which can be used to write streams to this store.
        /// </summary>
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="JsonExporter"/>.</param>
        /// <param name="name">The name of the store to create.</param>
        /// <param name="rootPath">The path to use. If null, an in-memory store is created.</param>
        /// <param name="createSubdirectory">Indicates whether to create a numbered subdirectory for each execution of the pipeline.</param>
        /// <returns>An <see cref="JsonExporter"/> instance that can be used to write streams.</returns>
        public static JsonExporter Create(Pipeline pipeline, string name, string rootPath, bool createSubdirectory = true)
        {
            return new JsonExporter(pipeline, name, rootPath, createSubdirectory);
        }

        /// <summary>
        /// Opens a multi-stream JSON store for read and returns an <see cref="JsonGenerator"/> instance which can be used to inspect the store and open the streams.
        /// </summary>
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="JsonGenerator"/>.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides.</param>
        /// <returns>A <see cref="JsonGenerator"/> instance that can be used to open streams and read messages.</returns>
        public static JsonGenerator Open(Pipeline pipeline, string name, string path)
        {
            return new JsonGenerator(pipeline, name, path);
        }

        /// <summary>
        /// Writes the specified stream to a multi-stream JSON store.
        /// </summary>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <param name="source">The source stream to write.</param>
        /// <param name="name">The name of the persisted stream.</param>
        /// <param name="writer">The store writer, created by e.g. <see cref="Create(Pipeline, string, string, bool)"/>.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The input stream.</returns>
        public static IProducer<TIn> Write<TIn>(this IProducer<TIn> source, string name, JsonExporter writer, DeliveryPolicy<TIn> deliveryPolicy = null)
        {
            writer.Write(source.Out, name, deliveryPolicy);
            return source;
        }
    }
}
