// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    /// <summary>
    /// Provides static methods to access Annotation stores.
    /// </summary>
    public static class AnnotationStore
    {
        /// <summary>
        /// Creates a new Annotation store and returns an <see cref="AnnotationExporter"/> instance which can be used to write streams to this store.
        /// </summary>
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="AnnotationExporter"/>.</param>
        /// <param name="name">The name of the store to create.</param>
        /// <param name="path">The path to use. If null, an in-memory store is created.</param>
        /// <param name="definition">The annotated event definition used to create and validate annotated events for this store.</param>
        /// <param name="createSubdirectory">Indicates whether to create a numbered subdirectory for each execution of the pipeline.</param>
        /// <returns>An <see cref="AnnotationExporter"/> instance that can be used to write streams.</returns>
        public static AnnotationExporter Create(Pipeline pipeline, string name, string path, AnnotatedEventDefinition definition, bool createSubdirectory = true)
        {
            return new AnnotationExporter(pipeline, name, path, definition, createSubdirectory);
        }

        /// <summary>
        /// Opens a Annotation store for read and returns an <see cref="AnnotationGenerator"/> instance which can be used to inspect the store and open the streams.
        /// </summary>
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="AnnotationGenerator"/>.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides.</param>
        /// <returns>A <see cref="AnnotationGenerator"/> instance that can be used to open streams and read messages.</returns>
        public static AnnotationGenerator Open(Pipeline pipeline, string name, string path)
        {
            return new AnnotationGenerator(pipeline, name, path);
        }

        /// <summary>
        /// Writes the specified stream to a Annotation store.
        /// </summary>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <param name="source">The source stream to write.</param>
        /// <param name="name">The name of the persisted stream.</param>
        /// <param name="writer">The store writer, created by e.g. <see cref="Create(Pipeline, string, string, AnnotatedEventDefinition, bool)"/>.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The input stream.</returns>
        public static IProducer<TIn> Write<TIn>(this IProducer<TIn> source, string name, AnnotationExporter writer, DeliveryPolicy<TIn> deliveryPolicy = null)
        {
            writer.Write(source.Out, name, deliveryPolicy);
            return source;
        }
    }
}
