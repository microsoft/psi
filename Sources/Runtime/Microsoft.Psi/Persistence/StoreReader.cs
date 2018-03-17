// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Implements a storage reader that allows access to the multiple storage streams persisted in a single store.
    /// The store reader abstracts read/write access to storage streams,
    /// and provides the means to read only some of the streams present in the store.
    /// The reader loads and exposes the metadata associated with the store prior to reading any data.
    /// </summary>
    public sealed class StoreReader : IDisposable
    {
        private readonly Dictionary<int, bool> isIndexedStream = new Dictionary<int, bool>();
        private readonly string name;
        private readonly string path;
        private readonly MessageReader messageReader;
        private readonly MessageReader largeMessageReader;
        private readonly Shared<MetadataCache> metadataCache;
        private readonly Shared<PageIndexCache> indexCache;

        private TimeInterval replayInterval = TimeInterval.Empty;
        private bool useOriginatingTime = false;
        private List<int> enabledStreams = new List<int>();
        private bool autoOpenAllStreams = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store</param>
        /// <param name="metadataUpdateHandler">Delegate to call</param>
        /// <param name="autoOpenAllStreams">Automatically open all streams</param>
        public StoreReader(string name, string path, Action<IEnumerable<Metadata>, RuntimeInfo> metadataUpdateHandler, bool autoOpenAllStreams = false)
        {
            this.name = name;
            this.path = StoreCommon.GetPathToLatestVersion(name, path);
            this.autoOpenAllStreams = autoOpenAllStreams;

            // open the data readers
            this.messageReader = new MessageReader(StoreCommon.GetDataFileName(this.name), this.path);
            this.largeMessageReader = new MessageReader(StoreCommon.GetLargeDataFileName(this.name), this.path);
            this.indexCache = Shared.Create(new PageIndexCache(name, this.path));
            this.metadataCache = Shared.Create(new MetadataCache(name, this.path, metadataUpdateHandler));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreReader"/> class.
        /// This provides a fast way to create a reader,
        /// by reusing the metadata and index already loaded by an existing store reader.
        /// </summary>
        /// <param name="other">Another reader pointing to the same store.</param>
        public StoreReader(StoreReader other)
        {
            this.name = other.Name;
            this.path = other.Path;
            this.autoOpenAllStreams = other.AutoOpenAllStreams;
            this.messageReader = new MessageReader(StoreCommon.GetDataFileName(this.name), this.path);
            this.largeMessageReader = new MessageReader(StoreCommon.GetLargeDataFileName(this.name), this.path);
            this.indexCache = other.indexCache.AddRef();
            this.metadataCache = other.metadataCache.AddRef();
        }

        /// <summary>
        /// Gets the set of logical storage streams in this store.
        /// </summary>
        public IEnumerable<PsiStreamMetadata> AvailableStreams => this.metadataCache.Resource.AvailableStreams;

        /// <summary>
        /// Gets the name of the store
        /// </summary>
        public string Name => this.name;

        /// <summary>
        /// Gets the path to the store (this is the path to the directory containing the data, index and catalog files)
        /// </summary>
        public string Path => this.path;

        /// <summary>
        /// Gets a value indicating whether the reader should read all the messages in the store.
        /// </summary>
        public bool AutoOpenAllStreams => this.autoOpenAllStreams;

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this store, across all logical streams.
        /// </summary>
        public TimeInterval ActiveTimeInterval => this.metadataCache.Resource.ActiveTimeInterval;

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this store, across all logical streams.
        /// </summary>
        public TimeInterval OriginatingTimeInterval => this.metadataCache.Resource.OriginatingTimeInterval;

        /// <summary>
        /// Gets the version of the runtime used to write to this store.
        /// </summary>
        public RuntimeInfo RuntimeVersion => this.metadataCache.Resource.RuntimeVersion;

        /// <summary>
        /// Indicates whether this store is still being written to by an active writer.
        /// </summary>
        /// <returns>True if an active writer is still writing to this store, false otherwise</returns>
        public bool IsMoreDataExpected() => this.messageReader.IsMoreDataExpected();

        /// <summary>
        /// Opens the specified logical storage stream for reading.
        /// </summary>
        /// <param name="name">The name of the storage stream to open.</param>
        /// <returns>The metadata describing the opened storage stream</returns>
        public PsiStreamMetadata OpenStream(string name)
        {
            var meta = this.GetMetadata(name);
            this.OpenStream(meta);
            return meta;
        }

        /// <summary>
        /// Opens the specified logical storage stream for reading.
        /// </summary>
        /// <param name="id">The id of the storage stream to open.</param>
        /// <returns>The metadata describing the opened storage stream</returns>
        public PsiStreamMetadata OpenStream(int id)
        {
            var meta = this.GetMetadata(id);
            this.OpenStream(meta);
            return meta;
        }

        /// <summary>
        /// Opens the specified logical storage stream for reading.
        /// </summary>
        /// <param name="meta">The metadata describing the storage stream to open.</param>
        /// <returns>True if the storage stream was successfully opened, false if no matching stream could be found.</returns>
        public bool OpenStream(PsiStreamMetadata meta)
        {
            if (this.enabledStreams.Contains(meta.Id))
            {
                return false;
            }

            this.enabledStreams.Add(meta.Id);
            this.IsIndexedStream(meta.Id); // update `isIndexedStream` dictionary
            return true;
        }

        /// <summary>
        /// Closes the specified storage stream. Messages from this stream will be skipped.
        /// </summary>
        /// <param name="name">The name of the stream to close.</param>
        public void CloseStream(string name)
        {
            var meta = this.GetMetadata(name);
            this.CloseStream(meta.Id);
        }

        /// <summary>
        /// Closes the specified storage stream. Messages from this stream will be skipped.
        /// </summary>
        /// <param name="id">The id of the stream to close.</param>
        public void CloseStream(int id)
        {
            this.enabledStreams.Remove(id);
        }

        /// <summary>
        /// Closes allthe storage streams.
        /// </summary>
        public void CloseAllStreams()
        {
            this.enabledStreams.Clear();
        }

        /// <summary>
        /// Checks whether the specified storage stream exist in this store.
        /// </summary>
        /// <param name="streamName">The name of the storage stream to look for.</param>
        /// <returns>True if a storage stream with the specified name exists, false otherwise</returns>
        public bool Contains(string streamName)
        {
            PsiStreamMetadata meta;
            return this.metadataCache.Resource.TryGet(streamName, out meta);
        }

        /// <summary>
        /// Returns a metadata descriptor for the specified storage stream.
        /// </summary>
        /// <param name="streamName">The name of the storage stream</param>
        /// <returns>The metadata describing the specified stream.</returns>
        public PsiStreamMetadata GetMetadata(string streamName)
        {
            PsiStreamMetadata meta;
            if (!this.metadataCache.Resource.TryGet(streamName, out meta))
            {
                throw new ArgumentException($"The store {this.name} does not contain a stream named {streamName}.");
            }

            return meta;
        }

        /// <summary>
        /// Returns a metadata descriptor for the specified storage stream.
        /// </summary>
        /// <param name="id">The id of the storage stream</param>
        /// <returns>The metadata describing the specified stream.</returns>
        public PsiStreamMetadata GetMetadata(int id)
        {
            PsiStreamMetadata meta;
            if (!this.metadataCache.Resource.TryGet(id, out meta))
            {
                throw new ArgumentException("A stream with this id could not be found: " + id);
            }

            return meta;
        }

        /// <summary>
        /// Closes all associated files.
        /// </summary>
        public void Dispose()
        {
            this.messageReader.Dispose();
            this.largeMessageReader.Dispose();
            this.metadataCache.Dispose();
            this.indexCache.Dispose();
        }

        /// <summary>
        /// Moves the reader to the start of the specified interval and restricts the read to messages within the interval.
        /// </summary>
        /// <param name="interval">The interval for reading data</param>
        /// <param name="useOriginatingTime">Indicates whether the interval refers to originating times or creation times</param>
        public void Seek(TimeInterval interval, bool useOriginatingTime = false)
        {
            this.replayInterval = interval;
            this.useOriginatingTime = useOriginatingTime;
            var indexEntry = this.indexCache.Resource.Search(interval.Left, useOriginatingTime);
            this.messageReader.Seek(indexEntry.ExtentId, indexEntry.Position);
        }

        /// <summary>
        /// Positions the reader to the next message from any one of the enabled streams.
        /// </summary>
        /// <param name="envelope">The envelope associated with the message read</param>
        /// <returns>True if there are more messages, false if no more messages are available</returns>
        public bool MoveNext(out Envelope envelope)
        {
            envelope = default(Envelope);
            do
            {
                var hasData = this.autoOpenAllStreams ? this.messageReader.MoveNext() : this.messageReader.MoveNext(this.enabledStreams);
                if (!hasData)
                {
                    if (!this.messageReader.IsMoreDataExpected())
                    {
                        return false;
                    }

                    var acquired = this.messageReader.DataReady.WaitOne(100); // DataReady is a pulse event, and might be missed
                    hasData = this.autoOpenAllStreams ? this.messageReader.MoveNext() : this.messageReader.MoveNext(this.enabledStreams);
                    if (acquired)
                    {
                        this.messageReader.DataReady.ReleaseMutex();
                    }

                    if (!hasData)
                    {
                        return false;
                    }
                }

                var messageTime = this.useOriginatingTime ? this.messageReader.Current.OriginatingTime : this.messageReader.Current.Time;
                if (this.replayInterval.PointIsWithin(messageTime))
                {
                    envelope = this.messageReader.Current;
                    this.metadataCache.Resource.Update();
                    return true;
                }

                if (this.replayInterval.Right < messageTime)
                {
                    this.CloseStream(this.messageReader.Current.SourceId);
                }
            }
            while (this.autoOpenAllStreams || this.enabledStreams.Count() > 0);

            return false;
        }

        /// <summary>
        /// Reads the next message from any one of the enabled streams (in serialized form) into the specified buffer.
        /// </summary>
        /// <param name="buffer">A buffer to read into.</param>
        /// <returns>Number of bytes read into the specified buffer</returns>
        public int Read(ref byte[] buffer)
        {
            var streamId = this.messageReader.Current.SourceId;

            // if the entry is an index entry, we need to load it from the large message file
            if (this.IsIndexedStream(streamId))
            {
                IndexEntry indexEntry;
                unsafe
                {
                    this.messageReader.Read((byte*)&indexEntry, sizeof(IndexEntry));
                }

                var extentId = indexEntry.ExtentId - int.MinValue;
                this.largeMessageReader.Seek(extentId, indexEntry.Position);
                this.largeMessageReader.MoveNext();
                return this.largeMessageReader.Read(ref buffer);
            }

            return this.messageReader.Read(ref buffer);
        }

        /// <summary>
        /// Reads the message from the specified position, without changing the current cursor position.
        /// Cannot be used together with MoveNext/Read.
        /// </summary>
        /// <param name="indexEntry">The position to read from</param>
        /// <param name="buffer">A buffer to read into.</param>
        /// <returns>Number of bytes read into the specified buffer</returns>
        public int ReadAt(IndexEntry indexEntry, ref byte[] buffer)
        {
            if (indexEntry.ExtentId < 0)
            {
                var extentId = indexEntry.ExtentId - int.MinValue;
                this.largeMessageReader.Seek(extentId, indexEntry.Position);
                this.largeMessageReader.MoveNext();
                return this.largeMessageReader.Read(ref buffer);
            }

            this.messageReader.Seek(indexEntry.ExtentId, indexEntry.Position);
            this.messageReader.MoveNext();
            return this.messageReader.Read(ref buffer);
        }

        /// <summary>
        /// Returns the position of the next message from any one of the enabled streams.
        /// </summary>
        /// <returns>The position of the message, excluding the envelope</returns>
        public IndexEntry ReadIndex()
        {
            IndexEntry indexEntry;
            var streamId = this.messageReader.Current.SourceId;

            // if the entry is an index entry, we just return it
            if (this.IsIndexedStream(streamId))
            {
                unsafe
                {
                    this.messageReader.Read((byte*)&indexEntry, sizeof(IndexEntry));
                }
            }
            else
            {
                // we need to make one on the fly
                indexEntry.Position = this.messageReader.CurrentMessageStart;
                indexEntry.ExtentId = this.messageReader.CurrentExtentId;
                indexEntry.Time = this.messageReader.Current.Time;
                indexEntry.OriginatingTime = this.messageReader.Current.OriginatingTime;
            }

            return indexEntry;
        }

        internal void EnsureMetadataUpdate()
        {
            this.metadataCache.Resource.Update();
        }

        private bool IsIndexedStream(int id)
        {
            bool isIndexed;
            if (this.isIndexedStream.TryGetValue(id, out isIndexed))
            {
                return isIndexed;
            }

            PsiStreamMetadata meta;
            if (this.metadataCache.Resource.TryGet(id, out meta))
            {
                isIndexed = meta.IsIndexed;
                this.isIndexedStream.Add(id, isIndexed);
                return isIndexed;
            }

            return false;
        }
    }
}
