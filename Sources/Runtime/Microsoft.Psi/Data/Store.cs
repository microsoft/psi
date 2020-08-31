// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// OBSOLETE. Provides static methods to access multi-stream stores.
    /// </summary>
    public static class Store
    {
        /// <summary>
        /// OBSOLETE. Creates a new multi-stream store and returns an <see cref="Exporter"/> instance
        /// which can be used to write streams to this store.
        /// </summary>
        /// <param name="pipeline">The <see cref="Pipeline"/> that owns the <see cref="Exporter"/>.</param>
        /// <param name="name">The name of the store to create.</param>
        /// <param name="rootPath">The path to use. If null, an in-memory store is created.</param>
        /// <param name="createSubdirectory">Indicates whether to create a numbered subdirectory for each execution of the pipeline.</param>
        /// <param name="serializers">An optional collection of custom serializers to use instead of the default ones.</param>
        /// <returns>An <see cref="Exporter"/> instance that can be used to write streams.</returns>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static Exporter Create(Pipeline pipeline, string name, string rootPath, bool createSubdirectory = true, KnownSerializers serializers = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Opens a multi-stream store for read and returns an <see cref="Importer"/> instance
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
        /// <param name="streamReader">Optional stream reader (default PsiStoreStreamReader).</param>
        /// <returns>An <see cref="Importer"/> instance that can be used to open streams and read messages.</returns>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static Importer Open(Pipeline pipeline, string name, string rootPath, IStreamReader streamReader = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Indicates whether the specified store file exists.
        /// </summary>
        /// <param name="name">The name of the store to check.</param>
        /// <param name="path">The path of the store to check.</param>
        /// <returns>Returns true if the store exists.</returns>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static bool Exists(string name, string path)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Indicates whether all streams in a store have been marked as "closed".
        /// </summary>
        /// <param name="name">The name of the store to check.</param>
        /// <param name="path">The path of the store to check.</param>
        /// <returns>Returns true if all streams in the store are marked as closed.</returns>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static bool IsClosed(string name, string path)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Repairs an invalid store in place.
        /// </summary>
        /// <param name="name">The name of the store to check.</param>
        /// <param name="path">The path of the store to check.</param>
        /// <param name="deleteOriginalStore">Indicates whether the original store should be deleted.</param>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static void Repair(string name, string path, bool deleteOriginalStore = true)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Crops a store between the extents of a specified interval, generating a new store.
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
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static void Crop((string Name, string Path) input, (string Name, string Path) output, TimeSpan start, RelativeTimeInterval length, bool createSubdirectory = true, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Crops a store between the extents of a specified originating time interval, generating a new store.
        /// </summary>
        /// <param name="input">The name and path of the store to crop.</param>
        /// <param name="output">The name and path of the cropped store.</param>
        /// <param name="cropInterval">The originating time interval to which to crop the store.</param>
        /// <param name="createSubdirectory">
        /// Indicates whether to create a numbered subdirectory for each cropped store
        /// generated by multiple calls to this method.
        /// </param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static void Crop((string Name, string Path) input, (string Name, string Path) output, TimeInterval cropInterval, bool createSubdirectory = true, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Crops a store in place between the extents of a specified interval.
        /// </summary>
        /// <param name="input">The name and path of the store to crop.</param>
        /// <param name="start">Start of crop interval relative to beginning of store.</param>
        /// <param name="length">Length of crop interval.</param>
        /// <param name="deleteOriginalStore">Indicates whether the original store should be deleted.</param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static void CropInPlace((string Name, string Path) input, TimeSpan start, RelativeTimeInterval length, bool deleteOriginalStore = true, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Crops a store in place between the extents of a specified originating time interval.
        /// </summary>
        /// <param name="input">The name and path of the store to crop.</param>
        /// <param name="cropInterval">The originating time interval to which to crop the store.</param>
        /// <param name="deleteOriginalStore">Indicates whether the original store should be deleted.</param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static void CropInPlace((string Name, string Path) input, TimeInterval cropInterval, bool deleteOriginalStore = true, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Copies a store, or a subset of it.
        /// </summary>
        /// <param name="input">The name and path of the store to crop.</param>
        /// <param name="output">The name and path of the cropped store.</param>
        /// <param name="cropIntervalFunction">An optional function that defines an originating time interval to copy. By default, the extents of the entire store.</param>
        /// <param name="includeStreamPredicate">An optional predicate that specifies which streams to include. By default, include all streams. By default, all streams are copied.</param>
        /// <param name="createSubdirectory">
        /// Indicates whether to create a numbered subdirectory for each cropped store
        /// generated by multiple calls to this method.
        /// </param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static void Copy(
            (string Name, string Path) input,
            (string Name, string Path) output,
            Func<Importer, TimeInterval> cropIntervalFunction = null,
            Predicate<IStreamMetadata> includeStreamPredicate = null,
            bool createSubdirectory = true,
            IProgress<double> progress = null,
            Action<string> loggingCallback = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Concatenates a set of stores, generating a new store.
        /// </summary>
        /// <remarks>Streams of the same name across stores must also have the same types as well as non-intersecting originating times.</remarks>
        /// <param name="storeFiles">Set of store files (name, path pairs) to concatenate.</param>
        /// <param name="output">Output store (name, path pair).</param>
        /// <param name="progress">An optional progress reporter for progress updates.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        [Obsolete("Store APIs have moved to PsiStore.", true)]
        public static void Concatenate(IEnumerable<(string Name, string Path)> storeFiles, (string Name, string Path) output, IProgress<double> progress = null, Action<string> loggingCallback = null)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Returns the metadata associated with the specified stream, if the stream is persisted to a store.
        /// </summary>
        /// <typeparam name="T">The type of stream messages.</typeparam>
        /// <param name="source">The stream to retrieve metadata about.</param>
        /// <param name="metadata">Upon return, this parameter contains the metadata associated with the stream, or null if the stream is not persisted.</param>
        /// <returns>True if the stream is persisted to a store, false otherwise.</returns>
        public static bool TryGetStreamMetadata<T>(IProducer<T> source, out IStreamMetadata metadata)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }

        /// <summary>
        /// OBSOLETE. Returns the metadata associated with the specified stream, if the stream is persisted to a store.
        /// </summary>
        /// <param name="pipeline">The current pipeline.</param>
        /// <param name="streamName">The name of the stream to retrieve metadata about.</param>
        /// <param name="metadata">Upon return, this parameter contains the metadata associated with the stream, or null if the stream is not persisted.</param>
        /// <returns>True if the stream is persisted to a store, false otherwise.</returns>
        public static bool TryGetStreamMetadata(Pipeline pipeline, string streamName, out IStreamMetadata metadata)
        {
            throw new NotImplementedException("Store APIs have moved to PsiStore.");
        }
    }
}