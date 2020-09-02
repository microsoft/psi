// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;

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
        private readonly Dictionary<int, PsiStreamMetadata> metadata = new Dictionary<int, PsiStreamMetadata>();
        private MessageWriter largeMessageWriter;
        private BufferWriter metadataBuffer = new BufferWriter(128);
        private BufferWriter indexBuffer = new BufferWriter(24);
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
                            .Where(
                                n =>
                                {
                                    int i;
                                    return int.TryParse(n, out i);
                                })
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

            this.catalogWriter = new InfiniteFileWriter(this.path, PsiStoreCommon.GetCatalogFileName(this.name), CatalogExtentSize);
            this.pageIndexWriter = new InfiniteFileWriter(this.path, PsiStoreCommon.GetIndexFileName(this.name), IndexExtentSize);
            this.writer = new MessageWriter(PsiStoreCommon.GetDataFileName(this.name), this.path);

            // write the first index entry
            this.UpdatePageIndex(0, default(Envelope));
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
        /// Closes the store.
        /// </summary>
        public void Dispose()
        {
            foreach (var streamId in this.metadata.Keys)
            {
                this.CloseStream(streamId);
            }

            this.pageIndexWriter.Dispose();
            this.catalogWriter.Dispose();
            this.writer.Dispose();
            this.largeMessageWriter?.Dispose();
        }

        /// <summary>
        /// Creates a stream to write messages to.
        /// The stream characteristics are extracted from the provided metadata descriptor.
        /// </summary>
        /// <param name="meta">The metadata describing the stream to open.</param>
        /// <returns>The complete metadata for the stream just created.</returns>
        public PsiStreamMetadata OpenStream(PsiStreamMetadata meta)
        {
            return this.OpenStream(meta.Id, meta.Name, meta.IsIndexed, meta.TypeName).UpdateSupplementalMetadataFrom(meta);
        }

        /// <summary>
        /// Creates a stream to write messages to.
        /// </summary>
        /// <param name="streamId">The id of the stream, unique for this store. All messages with this stream id will be written to this stream.</param>
        /// <param name="streamName">The name of the stream. This name can be later used to open the stream for reading.</param>
        /// <param name="indexed">Indicates whether the stream is indexed or not. Indexed streams have a small index entry in the main data file and the actual message body in a large data file.</param>
        /// <param name="typeName">A name identifying the type of the messages in this stream. This is usually a fully-qualified type name or a data contract name, but can be anything that the caller wants.</param>
        /// <returns>The complete metadata for the stream just created.</returns>
        public PsiStreamMetadata OpenStream(int streamId, string streamName, bool indexed, string typeName)
        {
            if (this.metadata.ContainsKey(streamId))
            {
                throw new InvalidOperationException($"The stream id {streamId} has already been registered with this writer.");
            }

            var meta = new PsiStreamMetadata(streamName, streamId, typeName);
            meta.OpenedTime = Time.GetCurrentTime();
            meta.IsPersisted = true;
            meta.IsIndexed = indexed;
            meta.PartitionName = this.name;
            meta.PartitionPath = this.path;
            this.metadata[streamId] = meta;
            this.WriteToCatalog(meta);

            // make sure we have a large file if needed
            if (indexed)
            {
                this.largeMessageWriter = this.largeMessageWriter ?? new MessageWriter(PsiStoreCommon.GetLargeDataFileName(this.name), this.path);
            }

            return meta;
        }

        /// <summary>
        /// Closes the stream and persists the stream statistics.
        /// </summary>
        /// <param name="streamId">The id of the stream to close.</param>
        /// <param name="originatingTime">The originating time when the stream was closed.</param>
        public void CloseStream(int streamId, DateTime? originatingTime = null)
        {
            var meta = this.metadata[streamId];
            if (!meta.IsClosed)
            {
                meta.ClosedTime = originatingTime ?? meta.LastMessageCreationTime;
                meta.IsClosed = true;
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
            int bytes = 0;
            if (meta.IsIndexed)
            {
                // write the object index entry in the data file and the buffer in the large data file
                IndexEntry indexEntry = default(IndexEntry);
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
        /// Writes details about a stream to the stream catalog.
        /// </summary>
        /// <param name="meta">The stream descriptor to write.</param>
        internal void WriteToCatalog(Metadata meta)
        {
            lock (this.catalogWriter)
            {
                meta.Serialize(this.metadataBuffer);

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
