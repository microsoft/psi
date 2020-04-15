// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Threading;
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
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="Exporter"/>.</param>
        /// <param name="name">The name of the store to create.</param>
        /// <param name="rootPath">The path to use. If null, an in-memory store is created.</param>
        /// <param name="createSubdirectory">Indicates whether to create a numbered subdirectory for each execution of the pipeline.</param>
        /// <param name="serializers">An optional collection of custom serializers to use instead of the default ones.</param>
        /// <returns>An <see cref="Exporter"/> instance that can be used to write streams.</returns>
        /// <remarks>
        /// The Exporter maintains a collection of serializers it knows about, which it uses to serialize
        /// the data it writes to the store. By default, the Exporter derives the correct serializers
        /// from the type argument passed to <see cref="Exporter.Write{T}(Emitter{T}, string, bool, DeliveryPolicy{T})"/>. In other words,
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
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="Importer"/>.</param>
        /// <param name="name">The name of the store to open (the same as the catalog file name).</param>
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
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <param name="source">The source stream to write.</param>
        /// <param name="name">The name of the persisted stream.</param>
        /// <param name="writer">The store writer, created by e.g. <see cref="Store.Create(Pipeline, string, string, bool, KnownSerializers)"/>.</param>
        /// <param name="largeMessages">Indicates whether the stream contains large messages (typically >4k). If true, the messages will be written to the large message file.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The input stream.</returns>
        public static IProducer<TIn> Write<TIn>(this IProducer<TIn> source, string name, Exporter writer, bool largeMessages = false, DeliveryPolicy<TIn> deliveryPolicy = null)
        {
            writer.Write(source.Out, name, largeMessages, deliveryPolicy);
            return source;
        }

        /// <summary>
        /// Writes the envelopes for the specified stream to a multi-stream store.
        /// </summary>
        /// <typeparam name="TIn">The type of messages in the stream.</typeparam>
        /// <param name="source">The source stream for which to write envelopes.</param>
        /// <param name="name">The name of the persisted stream.</param>
        /// <param name="writer">The store writer, created by e.g. <see cref="Store.Create(Pipeline, string, string, bool, KnownSerializers)"/>.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The input stream.</returns>
        public static IProducer<TIn> WriteEnvelopes<TIn>(this IProducer<TIn> source, string name, Exporter writer, DeliveryPolicy<TIn> deliveryPolicy = null)
        {
            writer.WriteEnvelopes(source.Out, name, deliveryPolicy);
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
        /// Indicates whether all streams in a store have been marked as "closed".
        /// </summary>
        /// <param name="name">The name of the store to check.</param>
        /// <param name="path">The path of the store to check.</param>
        /// <returns>Returns true if all streams in the store are marked as closed.</returns>
        public static bool IsClosed(string name, string path)
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
            Store.Crop((name, storePath), (name, tempFolderPath), TimeInterval.Infinite);

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
        /// <param name="input">The name and path of the store to crop.</param>
        /// <param name="output">The name and path of the cropped store.</param>
        /// <param name="start">Start of crop interval relative to beginning of store.</param>
        /// <param name="length">Length of crop interval.</param>
        /// <param name="createSubdirectory">
        /// Indicates whether to create a numbered subdirectory for each cropped store
        /// generated by multiple calls to this method.
        /// </param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        public static void Crop((string Name, string Path) input, (string Name, string Path) output, TimeSpan start, RelativeTimeInterval length, bool createSubdirectory = true, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            Crop(input, output, store => new TimeInterval(store.ActiveTimeInterval.Left + start, length), createSubdirectory, progress, loggingCallback);
        }

        /// <summary>
        /// Crops a store between the extents of a specified interval, generating a new store.
        /// </summary>
        /// <param name="input">The name and path of the store to crop.</param>
        /// <param name="output">The name and path of the cropped store.</param>
        /// <param name="cropInterval">The time interval to which to crop the store.</param>
        /// <param name="createSubdirectory">
        /// Indicates whether to create a numbered subdirectory for each cropped store
        /// generated by multiple calls to this method.
        /// </param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        public static void Crop((string Name, string Path) input, (string Name, string Path) output, TimeInterval cropInterval, bool createSubdirectory = true, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            Crop(input, output, _ => cropInterval, createSubdirectory, progress, loggingCallback);
        }

        /// <summary>
        /// Concatenates a set of stores, generating a new store.
        /// </summary>
        /// <remarks>Streams of the same name across stores must also have the same types as well as non-intersecting originating times.</remarks>
        /// <param name="storeFiles">Set of store files (name, path pairs) to concatenate.</param>
        /// <param name="output">Output store (name, path pair).</param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        public static void Concatenate(IEnumerable<(string Name, string Path)> storeFiles, (string Name, string Path) output, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            using (var pipeline = Pipeline.Create(nameof(Concatenate), DeliveryPolicy.Unlimited))
            {
                var outputStore = Store.Create(pipeline, output.Name, output.Path);
                var outputReceivers = new Dictionary<string, Receiver<Message<BufferReader>>>(); // per-stream receivers writing to store
                var inputStores = storeFiles.Select(file => (file, Store.Open(pipeline, file.Name, Path.GetFullPath(file.Path))));
                var streamMetas = inputStores.SelectMany(s => s.Item2.AvailableStreams).GroupBy(s => s.Name); // streams across input stores, grouped by name
                var totalMessageCount = 0; // total messages in all stores
                var totalWrittenCount = 0; // total currently written (to track progress)
                var totalInFlight = 0;
                foreach (var group in streamMetas)
                {
                    var name = group.Key;
                    loggingCallback?.Invoke($"Stream: {name}");
                    var emitter = pipeline.CreateEmitter<Message<BufferReader>>(pipeline, name);
                    var meta = group.First(); // using meta from first store in which stream appears
                    outputStore.Write(emitter, meta);
                    outputReceivers.Add(name, pipeline.CreateReceiver<Message<BufferReader>>(
                        pipeline,
                        m =>
                        {
                            emitter.Post(Message.Create(m.Data, m.OriginatingTime, m.Time, meta.Id, emitter.LastEnvelope.SequenceId + 1), m.OriginatingTime);
                            Interlocked.Decrement(ref totalInFlight);
                            Interlocked.Increment(ref totalWrittenCount);
                            if (totalWrittenCount % 10000 == 0)
                            {
                                progress?.Report((double)totalWrittenCount / totalMessageCount);
                            }
                        },
                        name));

                    // validate types match across stores and stream lifetimes don't overlap
                    foreach (var stream in group)
                    {
                        totalMessageCount += stream.MessageCount;
                        loggingCallback?.Invoke($"  Partition: {stream.PartitionName} {stream.Id} ({stream.TypeName.Split(',')[0]}) {stream.OriginatingLifetime.Left}-{stream.OriginatingLifetime.Right}");
                        if (group.GroupBy(pair => pair.TypeName).Count() != 1)
                        {
                            throw new ArgumentException("Type Mismatch");
                        }

                        foreach (var crosscheck in group)
                        {
                            if (crosscheck != stream && crosscheck.OriginatingLifetime.IntersectsWith(stream.OriginatingLifetime))
                            {
                                throw new ArgumentException("Originating Lifetime Overlap");
                            }
                        }
                    }
                }

                // for each store (in order), create pipeline to read to end; piping to receivers in outer pipeline
                var orderedStores = inputStores.OrderBy(i => i.Item2.OriginatingTimeInterval.Left).Select(i => (i.file.Name, i.file.Path));
                Generators.Sequence(pipeline, orderedStores, TimeSpan.FromTicks(1)).Do(file =>
                {
                    loggingCallback?.Invoke($"Processing: {file.Name} {file.Path}");
                    using (var p = Pipeline.Create(file.Name, DeliveryPolicy.Unlimited))
                    {
                        var store = Store.Open(p, file.Name, Path.GetFullPath(file.Path));
                        foreach (var stream in store.AvailableStreams)
                        {
                            var connector = store.CreateOutputConnectorTo<Message<BufferReader>>(pipeline, stream.Name);
                            store.OpenRawStream(stream).Do(_ => Interlocked.Increment(ref totalInFlight)).PipeTo(connector);
                            connector.Out.PipeTo(outputReceivers[stream.Name], true); // while outer pipeline running
                        }

                        p.Run();

                        while (totalInFlight > 0)
                        {
                            Thread.Sleep(100);
                        }

                        // unsubscribe to reuse receivers with next store
                        foreach (var r in outputReceivers.Values)
                        {
                            r.OnUnsubscribe();
                        }
                    }
                });

                loggingCallback?.Invoke("Concatenating...");
                pipeline.Run();
                loggingCallback?.Invoke("Done.");
            }
        }

        /// <summary>
        /// Returns the metadata associated with the specified stream, if the stream is persisted to a store.
        /// </summary>
        /// <typeparam name="T">The type of stream messages.</typeparam>
        /// <param name="source">The stream to retrieve metadata about.</param>
        /// <param name="metadata">Upon return, this parameter contains the metadata associated with the stream, or null if the stream is not persisted.</param>
        /// <returns>True if the stream is persisted to a store, false otherwise.</returns>
        public static bool TryGetMetadata<T>(IProducer<T> source, out PsiStreamMetadata metadata)
        {
            return Store.TryGetMetadata(source.Out.Pipeline, source.Out.Name, out metadata);
        }

        /// <summary>
        /// Returns the metadata associated with the specified stream, if the stream is persisted to a store.
        /// </summary>
        /// <param name="pipeline">The current pipeline.</param>
        /// <param name="streamName">The name of the stream to retrieve metadata about.</param>
        /// <param name="metadata">Upon return, this parameter contains the metadata associated with the stream, or null if the stream is not persisted.</param>
        /// <returns>True if the stream is persisted to a store, false otherwise.</returns>
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
        /// Crops a store between the extents of a specified interval, generating a new store.
        /// </summary>
        /// <param name="input">The name and path of the store to crop.</param>
        /// <param name="output">The name and path of the cropped store.</param>
        /// <param name="cropIntervalFn">Function creating crop interval.</param>
        /// <param name="createSubdirectory">
        /// Indicates whether to create a numbered subdirectory for each cropped store
        /// generated by multiple calls to this method.
        /// </param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        private static void Crop((string Name, string Path) input, (string Name, string Path) output, Func<Importer, TimeInterval> cropIntervalFn, bool createSubdirectory = true, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            using (var pipeline = Pipeline.Create("CropStore"))
            {
                Importer inputStore = Store.Open(pipeline, input.Name, input.Path);
                Exporter outputStore = Store.Create(pipeline, output.Name, output.Path, createSubdirectory, inputStore.Serializers);

                // copy all streams from inputStore to outputStore
                foreach (var streamInfo in inputStore.AvailableStreams)
                {
                    inputStore.CopyStream(streamInfo.Name, outputStore);
                }

                // run the pipeline to copy over the specified cropInterval
                loggingCallback?.Invoke("Cropping...");
                pipeline.RunAsync(cropIntervalFn(inputStore), false, progress);
                pipeline.WaitAll();
                loggingCallback?.Invoke("Done.");
            }
        }
    }
}