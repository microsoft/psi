// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Implements a writer that can write multiple streams to the same file,
    /// while cataloging and indexing them.
    /// </summary>
    public sealed class PsiStoreWriter : IDisposable
    {
        /// <summary>
        /// The size of a catalog file extent.
        /// </summary>
        public const int CatalogExtentSize = 512 * 1024;

        /// <summary>
        /// The size of the index file extent.
        /// </summary>
        public const int IndexExtentSize = 1024 * 1024;

        /// <summary>
        /// The frequency (in bytes) of index entries.
        /// Consecutive index entries point to locations that are at least this many bytes apart.
        /// </summary>
        public const int IndexPageSize = 4096;

        private readonly string name;
        private readonly string path;
        private readonly InfiniteFileWriter catalogWriter;
        private readonly InfiniteFileWriter pageIndexWriter;
        private readonly MessageWriter writer;
        private readonly ConcurrentDictionary<int, PsiStreamMetadata> metadata = new ();
        private readonly BufferWriter metadataBuffer = new (128);
        private readonly BufferWriter indexBuffer = new (24);

        /// <summary>
        /// This file is opened in exclusive share mode when the exporter is constructed, and is
        /// deleted when it gets disposed. Other processes can check the live status of the store
        /// by attempting to also open this file. If that fails, then the store is still live.
        /// </summary>
        private readonly FileStream liveMarkerFile;

        private MessageWriter largeMessageWriter;
        private int unindexedBytes = IndexPageSize;
        private IndexEntry nextIndexEntry;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStoreWriter"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which to create the partition, or null to create a volatile data store.</param>
        /// <param name="createSubdirectory">If true, a numbered sub-directory is created for this store.</param>
        public PsiStoreWriter(string name, string path, bool createSubdirectory = true)
        {
            this.name = name;
            if (path != null)
            {
                int id = 0;
                this.path = System.IO.Path.GetFullPath(path);
                if (createSubdirectory)
                {
                    // if the root directory already exists, look for the next available id
                    if (Directory.Exists(this.path))
                    {
                        var existingIds = Directory.EnumerateDirectories(this.path, this.name + ".????")
                            .Select(d => d.Split('.').Last())
                            .Where(n => int.TryParse(n, out _))
                            .Select(n => int.Parse(n));

                        id = (existingIds.Count() == 0) ? 0 : existingIds.Max() + 1;
                    }

                    this.path = System.IO.Path.Combine(this.path, $"{this.name}.{id:0000}");
                }

                if (!Directory.Exists(this.path))
                {
                    Directory.CreateDirectory(this.path);
                }
            }

            // Open the live store marker file in exclusive file share mode.  This will fail
            // if another process is already writing a store with the same name and path.
            this.liveMarkerFile = File.Open(PsiStoreMonitor.GetLiveMarkerFileName(this.Name, this.Path), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            this.catalogWriter = new InfiniteFileWriter(this.path, PsiStoreCommon.GetCatalogFileName(this.name), CatalogExtentSize);
            this.pageIndexWriter = new InfiniteFileWriter(this.path, PsiStoreCommon.GetIndexFileName(this.name), IndexExtentSize);
            this.writer = new MessageWriter(PsiStoreCommon.GetDataFileName(this.name), this.path);

            // write the first index entry
            this.UpdatePageIndex(0, default);
        }

        /// <summary>
        /// Gets the name of the store.
        /// </summary>
        public string Name => this.name;

        /// <summary>
        /// Gets the path to the store.
        /// </summary>
        public string Path => this.path;

        /// <summary>
        /// Gets stream metadata.
        /// </summary>
        public IEnumerable<IStreamMetadata> Metadata => this.metadata.Values;

        /// <summary>
        /// Closes the store.
        /// </summary>
        public void Dispose()
        {
            this.pageIndexWriter.Dispose();
            this.catalogWriter.Dispose();
            this.writer.Dispose();
            this.largeMessageWriter?.Dispose();
            this.liveMarkerFile?.Dispose();

            // If the live store marker file exists, try to delete it.
            string liveMarkerFilePath = PsiStoreMonitor.GetLiveMarkerFileName(this.Name, this.Path);
            if (File.Exists(liveMarkerFilePath))
            {
                try
                {
                    File.Delete(liveMarkerFilePath);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Creates a stream to write messages to.
        /// The stream characteristics are extracted from the provided metadata descriptor.
        /// </summary>
        /// <param name="metadata">The metadata describing the stream to open.</param>
        /// <returns>The complete metadata for the stream just created.</returns>
        public PsiStreamMetadata OpenStream(PsiStreamMetadata metadata)
            => this.OpenStream(metadata.Id, metadata.Name, metadata.IsIndexed, metadata.TypeName, metadata.OpenedTime).UpdateSupplementalMetadataFrom(metadata);

        /// <summary>
        /// Creates a stream to write messages to.
        /// </summary>
        /// <param name="id">The id of the stream, unique for this store. All messages with this stream id will be written to this stream.</param>
        /// <param name="name">The name of the stream. This name can be later used to open the stream for reading.</param>
        /// <param name="indexed">Indicates whether the stream is indexed or not. Indexed streams have a small index entry in the main data file and the actual message body in a large data file.</param>
        /// <param name="typeName">A name identifying the type of the messages in this stream. This is usually a fully-qualified type name or a data contract name, but can be anything that the caller wants.</param>
        /// <param name="streamOpenedTime">The opened time for the stream.</param>
        /// <returns>The complete metadata for the stream just created.</returns>
        public PsiStreamMetadata OpenStream(int id, string name, bool indexed, string typeName, DateTime streamOpenedTime)
        {
            if (this.metadata.ContainsKey(id))
            {
                throw new InvalidOperationException($"The stream id {id} has already been registered with this writer.");
            }

            var psiStreamMetadata = new PsiStreamMetadata(name, id, typeName)
            {
                OpenedTime = streamOpenedTime,
                IsPersisted = true,
                IsIndexed = indexed,
                StoreName = this.name,
                StorePath = this.path,
            };
            this.metadata[id] = psiStreamMetadata;
            this.WriteToCatalog(psiStreamMetadata);

            // make sure we have a large file if needed
            if (indexed)
            {
                this.largeMessageWriter ??= new MessageWriter(PsiStoreCommon.GetLargeDataFileName(this.name), this.path);
            }

            return psiStreamMetadata;
        }

        /// <summary>
        /// Attempt to get stream metadata (available once stream has been opened).
        /// </summary>
        /// <param name="streamId">The id of the stream, unique for this store.</param>
        /// <param name="metadata">The metadata for the stream, if it has previously been opened.</param>
        /// <returns>True if stream metadata if stream has been opened so that metadata is available.</returns>
        public bool TryGetMetadata(int streamId, out PsiStreamMetadata metadata) =>
            this.metadata.TryGetValue(streamId, out metadata);

        /// <summary>
        /// Closes the stream and persists the stream statistics.
        /// </summary>
        /// <param name="streamId">The id of the stream to close.</param>
        /// <param name="originatingTime">The originating time when the stream was closed.</param>
        public void CloseStream(int streamId, DateTime originatingTime)
        {
            var meta = this.metadata[streamId];
            if (!meta.IsClosed)
            {
                // When a store is being rewritten (e.g. to crop, repair, etc.) the OpenedTime can
                // be after the closing originatingTime originally stored. Originating times remain
                // in the timeframe of the original store, while Opened/ClosedTimes in the rewritten
                // store reflect the wall-clock time at which each stream was rewritten.
                // Technically, the rewritten streams are opened, written, and closed in quick
                // succession, but we record an interval at least large enough to envelope the
                // originating time interval. However, a stream with zero or one message will still
                // show an empty interval (ClosedTime = OpenedTime).
                meta.ClosedTime =
                    meta.OpenedTime <= originatingTime ? // opened before/at closing time?
                    originatingTime : // using closing time
                    meta.OpenedTime + meta.MessageOriginatingTimeInterval.Span; // o/w assume closed after span of messages
                meta.IsClosed = true;
                this.WriteToCatalog(meta);
            }
        }

        /// <summary>
        /// Closes the streams and persists the stream statistics.
        /// </summary>
        /// <param name="originatingTime">The originating time when the streams are closed.</param>
        public void CloseAllStreams(DateTime originatingTime)
        {
            foreach (var streamId in this.metadata.Keys)
            {
                this.CloseStream(streamId, originatingTime);
            }
        }

        /// <summary>
        /// Initialize stream opened times.
        /// </summary>
        /// <param name="originatingTime">The originating time when the streams are opened.</param>
        public void InitializeStreamOpenedTimes(DateTime originatingTime)
        {
            foreach (var meta in this.metadata.Values)
            {
                meta.OpenedTime = originatingTime;
                this.WriteToCatalog(meta);
            }
        }

        /// <summary>
        /// Writes a message (envelope + data) to the store. The message is associated with the open stream that matches the id in the envelope.
        /// </summary>
        /// <param name="buffer">The payload to write. This might be written to the main data file or the large data file, depending on stream configuration. </param>
        /// <param name="envelope">The envelope of the message, identifying the stream, the time and the sequence number of the message.</param>
        public void Write(BufferReader buffer, Envelope envelope)
        {
            var meta = this.metadata[envelope.SourceId];
            meta.Update(envelope, buffer.RemainingLength);
            int bytes;
            if (meta.IsIndexed)
            {
                // write the object index entry in the data file and the buffer in the large data file
                var indexEntry = default(IndexEntry);
                indexEntry.ExtentId = int.MinValue + this.largeMessageWriter.CurrentExtentId; // negative value indicates an index into the large file
                indexEntry.Position = this.largeMessageWriter.CurrentMessageStart;
                indexEntry.CreationTime = envelope.CreationTime;
                indexEntry.OriginatingTime = envelope.OriginatingTime;
                unsafe
                {
                    this.indexBuffer.Write((byte*)&indexEntry, sizeof(IndexEntry));
                }

                // write the buffer to the large message file
                this.largeMessageWriter.Write(buffer, envelope);

                // note that our page index points to the data file, so we need to update it with the bytes written to the data file
                bytes = this.writer.Write(this.indexBuffer, envelope);
                this.indexBuffer.Reset();
            }
            else
            {
                bytes = this.writer.Write(buffer, envelope);
            }

            this.UpdatePageIndex(bytes, envelope);
        }

        /// <summary>
        /// Writes the runtime info to the catalog.
        /// </summary>
        /// <param name="runtimeInfo">The runtime info.</param>
        internal void WriteToCatalog(RuntimeInfo runtimeInfo) => this.WriteToCatalog((Metadata)runtimeInfo);

        /// <summary>
        /// Writes the type schema to the catalog.
        /// </summary>
        /// <param name="typeSchema">The type schema.</param>
        internal void WriteToCatalog(TypeSchema typeSchema) => this.WriteToCatalog((Metadata)typeSchema);

        /// <summary>
        /// Writes the psi stream metadata to the catalog.
        /// </summary>
        /// <param name="typeSchema">The psi stream metadata schema.</param>
        internal void WriteToCatalog(PsiStreamMetadata typeSchema) => this.WriteToCatalog((Metadata)typeSchema);

        /// <summary>
        /// Writes details about a stream to the stream catalog.
        /// </summary>
        /// <param name="metadata">The stream descriptor to write.</param>
        private void WriteToCatalog(Metadata metadata)
        {
            lock (this.catalogWriter)
            {
                metadata.Serialize(this.metadataBuffer);

                this.catalogWriter.Write(this.metadataBuffer);
                this.catalogWriter.Flush();
                this.metadataBuffer.Reset();
            }
        }

        /// <summary>
        /// Updates the seek index (which is an index into the main data file) if needed (every <see cref="IndexPageSize"/> bytes).
        /// </summary>
        /// <param name="bytes">Number of bytes written so far to the data file.</param>
        /// <param name="lastEnvelope">The envelope of the last message written.</param>
        private void UpdatePageIndex(int bytes, Envelope lastEnvelope)
        {
            if (lastEnvelope.OriginatingTime > this.nextIndexEntry.OriginatingTime)
            {
                this.nextIndexEntry.OriginatingTime = lastEnvelope.OriginatingTime;
            }

            if (lastEnvelope.CreationTime > this.nextIndexEntry.CreationTime)
            {
                this.nextIndexEntry.CreationTime = lastEnvelope.CreationTime;
            }

            this.unindexedBytes += bytes;

            // only write an index entry if we exceeded the page size
            // The index identifies the upper bound on time and originating time for messages written so far
            if (this.unindexedBytes >= IndexPageSize)
            {
                this.nextIndexEntry.Position = this.writer.CurrentMessageStart;
                this.nextIndexEntry.ExtentId = this.writer.CurrentExtentId;

                unsafe
                {
                    unsafe
                    {
                        var indexEntry = this.nextIndexEntry;
                        var totalBytes = sizeof(IndexEntry);
                        this.pageIndexWriter.ReserveBlock(totalBytes);
                        this.pageIndexWriter.WriteToBlock((byte*)&indexEntry, totalBytes);
                        this.pageIndexWriter.CommitBlock();
                    }
                }

                this.unindexedBytes = 0;
            }
        }
    }
}