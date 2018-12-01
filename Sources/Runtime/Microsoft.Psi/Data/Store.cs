// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Provides static methods to access multi-stream stores.
    /// </summary>
    public static class Store
    {
        internal static readonly string StreamMetadataNamespace = "store\\metadata";

        /// <summary>
        /// Creates a new multi-stream store and returns an <see cref="Exporter"/> instance
        /// which can be used to write streams to this store.
        /// </summary>
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="Exporter"/></param>
        /// <param name="name">The name of the store to create</param>
        /// <param name="rootPath">The path to use. If null, an in-memory store is created.</param>
        /// <param name="createSubdirectory">Indicates whether to create a numbered subdirectory for each execution of the pipeline.</param>
        /// <param name="serializers">An optional collection of custom serializers to use instead of the default ones</param>
        /// <returns>An <see cref="Exporter"/> instance that can be used to write streams.</returns>
        /// <remarks>
        /// The Exporter maintains a collection of serializers it knows about, which it uses to serialize
        /// the data it writes to the store. By default, the Exporter derives the correct serializers
        /// from the type argument passed to <see cref="Exporter.Write{T}(Emitter{T}, string, bool, DeliveryPolicy)"/>. In other words,
        /// for the most part simply knowing the stream type is sufficient to determine all the types needed to
        /// serialize the messages in the stream.
        /// Use the <see cref="KnownSerializers"/> parameter to override the default behavior and provide a custom set of serializers.
        /// </remarks>
        public static Exporter Create(Pipeline pipeline, string name, string rootPath, bool createSubdirectory = true, KnownSerializers serializers = null)
        {
            return new Exporter(pipeline, name, rootPath, createSubdirectory, serializers);
        }

        /// <summary>
        /// Opens a multi-stream store for read and returns an <see cref="Importer"/> instance
        /// which can be used to inspect the store and open the streams.
        /// The store metadata is available immediately after this call (before the pipeline is running) via the <see cref="Importer.AvailableStreams"/> property.
        /// </summary>
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="Importer"/></param>
        /// <param name="name">The name of the store to open (the same as the catalog file name)</param>
        /// <param name="rootPath">
        /// The path to the store.
        /// This can be one of:
        /// - a full path to a directory containing the store
        /// - a root path containing one or more versions of the store, each in its own subdirectory,
        /// in which case the latest store is opened.
        /// - a null string, in which case an in-memory store is opened.
        /// </param>
        /// <returns>An <see cref="Importer"/> instance that can be used to open streams and read messages.</returns>
        /// <remarks>
        /// The Importer maintains a collection of serializers it knows about, which it uses to deserialize
        /// the data it reads form the store. By default, the Importer derives the correct serializers
        /// from the type argument passed to <see cref="Importer.OpenStream{T}(string, T)"/>. In other words,
        /// for the most part simply knowing the stream type is sufficient to determine all the types needed to
        /// deserialize the messages in the stream.
        /// However, there are two cases when this automatic behavior might not work:
        /// 1. When one of the required types changed between the version used to serialize the file and the
        /// current version, in a way that breaks versioning rules.
        /// Use the <see cref="KnownSerializers.Register{T}(string)"/> method
        /// to remap the name of the old type to a new, compatible type.
        /// 2. When the declared type of a field is different than the actual value assigned to it
        /// (polymorphic fields) and the value assigned is of a type that implements the DataContract serialization rules.
        /// In this case, use the <see cref="KnownSerializers.Register{T}()"/> method
        /// to let the serialization system know which compatible concrete type to use for that DataContract name.
        /// </remarks>
        public static Importer Open(Pipeline pipeline, string name, string rootPath) // open latest if more than one
        {
            return new Importer(pipeline, name, rootPath);
        }

        /// <summary>
        /// Writes the specified stream to a multi-stream store.
        /// </summary>
        /// <typeparam name="TIn">The type of messages in the stream</typeparam>
        /// <param name="source">The source stream to write</param>
        /// <param name="name">The name of the persisted stream.</param>
        /// <param name="writer">The store writer, created by e.g. <see cref="Store.Create(Pipeline, string, string, bool, KnownSerializers)"/></param>
        /// <param name="largeMessages">Indicates whether the stream contains large messages (typically >4k). If true, the messages will be written to the large message file.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The input stream</returns>
        public static IProducer<TIn> Write<TIn>(this IProducer<TIn> source, string name, Exporter writer, bool largeMessages = false, DeliveryPolicy deliveryPolicy = null)
        {
            writer.Write(source.Out, name, largeMessages, deliveryPolicy);
            return source;
        }

        /// <summary>
        /// Indicates whether the specified store file exists.
        /// </summary>
        /// <param name="name">The name of the store to check.</param>
        /// <param name="path">The path of the store to check.</param>
        /// <returns>Returns true if the store exists.</returns>
        public static bool Exists(string name, string path)
        {
            return (path == null) ? InfiniteFileReader.IsActive(StoreCommon.GetCatalogFileName(name), path) : StoreCommon.TryGetPathToLatestVersion(name, path, out string fullPath);
        }

        /// <summary>
        /// Indicates whether the specified store is valid.
        /// </summary>
        /// <param name="name">The name of the store to check.</param>
        /// <param name="path">The path of the store to check.</param>
        /// <returns>Returns true if the store is valid.</returns>
        public static bool IsValid(string name, string path)
        {
            bool allStreamsClosed = false;
            using (var p = Pipeline.Create())
            {
                var importer = Store.Open(p, name, path);
                allStreamsClosed = importer.AvailableStreams.All(meta => meta.IsClosed);
            }

            return allStreamsClosed;
        }

        /// <summary>
        /// Repairs an invalid store.
        /// </summary>
        /// <param name="name">The name of the store to check.</param>
        /// <param name="path">The path of the store to check.</param>
        /// <param name="deleteOldStore">Indicates whether the original store should be deleted.</param>
        public static void Repair(string name, string path, bool deleteOldStore = true)
        {
            string storePath = StoreCommon.GetPathToLatestVersion(name, path);
            string tempFolderPath = Path.Combine(path, $"Repair-{Guid.NewGuid()}");

            // call Crop over the entire store interval to regenerate and repair the streams in the store
            Store.Crop(name, storePath, name, tempFolderPath, TimeInterval.Infinite);

            // create a _BeforeRepair folder in which to save the original store files
            var beforeRepairPath = Path.Combine(storePath, $"BeforeRepair-{Guid.NewGuid()}");
            Directory.CreateDirectory(beforeRepairPath);

            // Move the original store files to the BeforeRepair folder. Do this even if the deleteOldStore
            // flag is true, as deleting the original store files immediately may occasionally fail. This can
            // happen because the InfiniteFileReader disposes of its MemoryMappedView in a background
            // thread, which may still be in progress. If deleteOldStore is true, we will delete the
            // BeforeRepair folder at the very end (by which time any open MemoryMappedViews will likely
            // have finished disposing).
            foreach (var file in Directory.EnumerateFiles(storePath))
            {
                var fileInfo = new FileInfo(file);
                File.Move(file, Path.Combine(beforeRepairPath, fileInfo.Name));
            }

            // move the repaired store files to the original folder
            foreach (var file in Directory.EnumerateFiles(Path.Combine(tempFolderPath, $"{name}.0000")))
            {
                var fileInfo = new FileInfo(file);
                File.Move(file, Path.Combine(storePath, fileInfo.Name));
            }

            // cleanup temporary folder
            Directory.Delete(tempFolderPath, true);

            if (deleteOldStore)
            {
                // delete the old store files
                Directory.Delete(beforeRepairPath, true);
            }
        }

        /// <summary>
        /// Crops a store between the extents of a specified interval, generating a new store.
        /// </summary>
        /// <param name="inputName">The name of the store to crop.</param>
        /// <param name="inputPath">The path of the store to crop.</param>
        /// <param name="outputName">The name of the cropped store.</param>
        /// <param name="outputPath">The path of the cropped store.</param>
        /// <param name="cropInterval">The time interval to which to crop the store.</param>
        /// <param name="createSubdirectory">
        /// Indicates whether to create a numbered subdirectory for each cropped store
        /// generated by multiple calls to this method.
        /// </param>
        public static void Crop(string inputName, string inputPath, string outputName, string outputPath, TimeInterval cropInterval, bool createSubdirectory = true)
        {
            using (var pipeline = Pipeline.Create("CropStore"))
            {
                Importer inputStore = Store.Open(pipeline, inputName, inputPath);
                Exporter outputStore = Store.Create(pipeline, outputName, outputPath, createSubdirectory, inputStore.Serializers);

                // copy all streams from inputStore to outputStore
                foreach (var streamInfo in inputStore.AvailableStreams)
                {
                    inputStore.CopyStream(streamInfo.Name, outputStore);
                }

                // run the pipeline to copy over the specified cropInterval
                pipeline.Run(cropInterval, true, false);
            }
        }

        /// <summary>
        /// Returns the metadata associated with the specified stream, if the stream is persisted to a store.
        /// </summary>
        /// <typeparam name="T">The type of stream messages</typeparam>
        /// <param name="source">The stream to retrieve metadata about</param>
        /// <param name="metadata">Upon return, this parameter contains the metadata associated with the stream, or null if the stream is not persisted</param>
        /// <returns>True if the stream is persisted to a store, false otherwise</returns>
        public static bool TryGetMetadata<T>(IProducer<T> source, out PsiStreamMetadata metadata)
        {
            return Store.TryGetMetadata(source.Out.Pipeline, source.Out.Name, out metadata);
        }

        /// <summary>
        /// Returns the metadata associated with the specified stream, if the stream is persisted to a store.
        /// </summary>
        /// <param name="pipeline">The current pipeline</param>
        /// <param name="streamName">The name of the stream to retrieve metadata about</param>
        /// <param name="metadata">Upon return, this parameter contains the metadata associated with the stream, or null if the stream is not persisted</param>
        /// <returns>True if the stream is persisted to a store, false otherwise</returns>
        public static bool TryGetMetadata(Pipeline pipeline, string streamName, out PsiStreamMetadata metadata)
        {
            if (string.IsNullOrEmpty(streamName))
            {
                metadata = null;
                return false;
            }

            return pipeline.ConfigurationStore.TryGet(StreamMetadataNamespace, streamName, out metadata);
        }

        /// <summary>
        /// Serializes the source stream, preserving the envelope.
        /// </summary>
        /// <typeparam name="T">The type of data to serialize</typeparam>
        /// <param name="source">The source stream generating the data to serialize</param>
        /// <param name="serializers">An optional collection of known types to use</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of Message{Message{BufferReader}}, where the inner Message object preserves the original envelope of the message received by this operator.</returns>
        public static IProducer<Message<BufferReader>> Serialize<T>(this IProducer<T> source, KnownSerializers serializers = null, DeliveryPolicy deliveryPolicy = null)
        {
            var serializer = new SerializerComponent<T>(source.Out.Pipeline, serializers ?? KnownSerializers.Default);
            source.PipeTo(serializer.In, deliveryPolicy);
            return serializer.Out;
        }

        /// <summary>
        /// Deserializes data from a stream of Message{BufferReader}.
        /// </summary>
        /// <typeparam name="T">The type of data expected after deserialization</typeparam>
        /// <param name="source">The stream containing the serialized data</param>
        /// <param name="serializers">An optional collection of known types to use</param>
        /// <param name="reusableInstance">An optional preallocated instance ot use as a buffer. This parameter is required when deserializing <see cref="Shared{T}"/> instances if the deserializer is expected to use a <see cref="SharedPool{T}"/></param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of messages of type T, with their original envelope</returns>
        public static IProducer<T> Deserialize<T>(this IProducer<Message<BufferReader>> source, KnownSerializers serializers = null, T reusableInstance = default(T), DeliveryPolicy deliveryPolicy = null)
        {
            var deserializer = new DeserializerComponent<T>(source.Out.Pipeline, serializers ?? KnownSerializers.Default, reusableInstance);
            source.PipeTo(deserializer, deliveryPolicy);
            return deserializer.Out;
        }
    }
}