// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Implements a reader that allows access to the multiple streams persisted in a single store.
    /// The store reader abstracts read/write access to streams,
    /// and provides the means to read only some of the streams present in the store.
    /// The reader loads and exposes the metadata associated with the store prior to reading any data.
    /// </summary>
    public sealed class PsiStoreReader : IDisposable
    {
        private readonly Dictionary<int, bool> isIndexedStream = new ();
        private readonly MessageReader messageReader;
        private readonly MessageReader largeMessageReader;
        private readonly Shared<MetadataCache> metadataCache;
        private readonly Shared<PageIndexCache> indexCache;
        private readonly HashSet<int> enabledStreams = new ();

        private TimeInterval replayInterval = TimeInterval.Empty;
        private bool useOriginatingTime = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStoreReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="metadataUpdateHandler">Delegate to call.</param>
        /// <param name="autoOpenAllStreams">Automatically open all streams.</param>
        public PsiStoreReader(string name, string path, Action<IEnumerable<Metadata>, RuntimeInfo> metadataUpdateHandler, bool autoOpenAllStreams = false)
        {
            this.Name = name;
            this.Path = PsiStore.GetPathToLatestVersion(name, path);
            this.AutoOpenAllStreams = autoOpenAllStreams;

            // open the data readers
            this.messageReader = new MessageReader(PsiStoreCommon.GetDataFileName(this.Name), this.Path);
            this.largeMessageReader = new MessageReader(PsiStoreCommon.GetLargeDataFileName(this.Name), this.Path);
            this.indexCache = Shared.Create(new PageIndexCache(name, this.Path));
            this.metadataCache = Shared.Create(new MetadataCache(name, this.Path, metadataUpdateHandler));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStoreReader"/> class.
        /// This provides a fast way to create a reader,
        /// by reusing the metadata and index already loaded by an existing store reader.
        /// </summary>
        /// <param name="other">Another reader pointing to the same store.</param>
        public PsiStoreReader(PsiStoreReader other)
        {
            this.Name = other.Name;
            this.Path = other.Path;
            this.AutoOpenAllStreams = other.AutoOpenAllStreams;
            this.messageReader = new MessageReader(PsiStoreCommon.GetDataFileName(this.Name), this.Path);
            this.largeMessageReader = new MessageReader(PsiStoreCommon.GetLargeDataFileName(this.Name), this.Path);
            this.indexCache = other.indexCache.AddRef();
            this.metadataCache = other.metadataCache.AddRef();
        }

        /// <summary>
        /// Gets the set of streams in this store.
        /// </summary>
        public IEnumerable<PsiStreamMetadata> AvailableStreams => this.metadataCache.Resource.AvailableStreams;

        /// <summary>
        /// Gets the name of the store.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the path to the store (this is the path to the directory containing the data, index and catalog files).
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets a value indicating whether the reader should read all the messages in the store.
        /// </summary>
        public bool AutoOpenAllStreams { get; } = false;

        /// <summary>
        /// Gets the interval between the creation times of the first and last messages written to this store, across all streams.
        /// </summary>
        public TimeInterval MessageCreationTimeInterval => this.metadataCache.Resource.MessageCreationTimeInterval;

        /// <summary>
        /// Gets the interval between the originating times of the first and last messages written to this store, across all streams.
        /// </summary>
        public TimeInterval MessageOriginatingTimeInterval => this.metadataCache.Resource.MessageOriginatingTimeInterval;

        /// <summary>
        /// Gets the interval between the opened and closed times, across all streams.
        /// </summary>
        public TimeInterval StreamTimeInterval => this.metadataCache.Resource.StreamTimeInterval;

        /// <summary>
        /// Gets the size of the store.
        /// </summary>
        public long Size => PsiStoreCommon.GetSize(this.Name, this.Path);

        /// <summary>
        /// Gets the number of streams in the store.
        /// </summary>
        public int StreamCount => this.metadataCache.Resource.AvailableStreams.Count();

        /// <summary>
        /// Gets info about the runtime that was used to write to this store.
        /// </summary>
        public RuntimeInfo RuntimeInfo => this.metadataCache.Resource.RuntimeInfo;

        /// <summary>
        /// Opens the specified stream for reading.
        /// </summary>
        /// <param name="name">The name of the stream to open.</param>
        /// <returns>The metadata describing the opened stream.</returns>
        public PsiStreamMetadata OpenStream(string name)
        {
            var meta = this.GetMetadata(name);
            this.OpenStream(meta);
            return meta;
        }

        /// <summary>
        /// Opens the specified stream for reading.
        /// </summary>
        /// <param name="id">The id of the stream to open.</param>
        /// <returns>The metadata describing the opened stream.</returns>
        public PsiStreamMetadata OpenStream(int id)
        {
            var meta = this.GetMetadata(id);
            this.OpenStream(meta);
            return meta;
        }

        /// <summary>
        /// Opens the specified stream for reading.
        /// </summary>
        /// <param name="meta">The metadata describing the stream to open.</param>
        /// <returns>True if the stream was successfully opened, false if no matching stream could be found.</returns>
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
        /// Closes the specified stream. Messages from this stream will be skipped.
        /// </summary>
        /// <param name="name">The name of the stream to close.</param>
        public void CloseStream(string name)
        {
            var meta = this.GetMetadata(name);
            this.CloseStream(meta.Id);
        }

        /// <summary>
        /// Closes the specified stream. Messages from this stream will be skipped.
        /// </summary>
        /// <param name="id">The id of the stream to close.</param>
        public void CloseStream(int id)
        {
            this.enabledStreams.Remove(id);
        }

        /// <summary>
        /// Closes all the streams.
        /// </summary>
        public void CloseAllStreams()
        {
            this.enabledStreams.Clear();
        }

        /// <summary>
        /// Checks whether the specified stream exist in this store.
        /// </summary>
        /// <param name="streamName">The name of the stream to look for.</param>
        /// <returns>True if a stream with the specified name exists, false otherwise.</returns>
        public bool Contains(string streamName)
        {
            return this.metadataCache.Resource.TryGet(streamName, out _);
        }

        /// <summary>
        /// Returns a metadata descriptor for the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The metadata describing the specified stream.</returns>
        public PsiStreamMetadata GetMetadata(string streamName)
        {
            if (!this.metadataCache.Resource.TryGet(streamName, out PsiStreamMetadata meta))
            {
                throw new ArgumentException($"The store {this.Name} does not contain a stream named {streamName}.");
            }

            return meta;
        }

        /// <summary>
        /// Returns a metadata descriptor for the specified stream.
        /// </summary>
        /// <param name="id">The id of the stream.</param>
        /// <returns>The metadata describing the specified stream.</returns>
        public PsiStreamMetadata GetMetadata(int id)
        {
            if (!this.metadataCache.Resource.TryGet(id, out PsiStreamMetadata meta))
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
        /// <param name="interval">The interval for reading data.</param>
        /// <param name="useOriginatingTime">Indicates whether the interval refers to originating times or creation times.</param>
        public void Seek(TimeInterval interval, bool useOriginatingTime = false)
        {
            this.replayInterval = interval;
            this.useOriginatingTime = useOriginatingTime;
            var indexEntry = this.indexCache.Resource.Search(interval.Left, useOriginatingTime);
            this.messageReader.Seek(indexEntry.ExtentId, indexEntry.Position);
        }

        /// <summary>
        /// Gets the current temporal extents of the store by time and originating time.
        /// </summary>
        /// <returns>A pair of TimeInterval objects that represent the times and originating times of the first and last messages currently in the store.</returns>
        public (TimeInterval, TimeInterval) GetLiveStoreExtents()
        {
            Envelope envelope;

            // Get the times of the first message
            this.Seek(new TimeInterval(DateTime.MinValue, DateTime.MaxValue), true);
            DateTime firstMessageCreationTime = DateTime.MinValue;
            DateTime firstMessageOriginatingTime = DateTime.MinValue;
            DateTime lastMessageCreationTime = DateTime.MinValue;
            DateTime lastMessageOriginatingTime = DateTime.MinValue;
            if (this.messageReader.MoveNext())
            {
                envelope = this.messageReader.Current;
                firstMessageCreationTime = envelope.CreationTime;
                firstMessageOriginatingTime = envelope.OriginatingTime;
                lastMessageCreationTime = envelope.CreationTime;
                lastMessageOriginatingTime = envelope.OriginatingTime;
            }

            // Get the last Index Entry from the cache and seek to it
            IndexEntry indexEntry = this.indexCache.Resource.Search(DateTime.MaxValue, true);
            this.Seek(new TimeInterval(indexEntry.OriginatingTime, DateTime.MaxValue), true);

            // Find the last message in the extent
            while (this.messageReader.MoveNext())
            {
                envelope = this.messageReader.Current;
                lastMessageCreationTime = envelope.CreationTime;
                lastMessageOriginatingTime = envelope.OriginatingTime;
            }

            this.metadataCache.Resource.Update();

            return (new TimeInterval(firstMessageCreationTime, lastMessageCreationTime), new TimeInterval(firstMessageOriginatingTime, lastMessageOriginatingTime));
        }

        /// <summary>
        /// Positions the reader to the next message from any one of the enabled streams.
        /// </summary>
        /// <param name="envelope">The envelope associated with the message read.</param>
        /// <returns>True if there are more messages, false if no more messages are available.</returns>
        public bool MoveNext(out Envelope envelope)
        {
            envelope = default;
            do
            {
                var hasData = this.AutoOpenAllStreams ? this.messageReader.MoveNext() : this.messageReader.MoveNext(this.enabledStreams);
                if (!hasData)
                {
                    if (!PsiStoreMonitor.IsStoreLive(this.Name, this.Path))
                    {
                        return false;
                    }

                    bool acquired = false;
                    try
                    {
                        acquired = this.messageReader.DataReady.WaitOne(100); // DataReady is a pulse event, and might be missed
                    }
                    catch (AbandonedMutexException)
                    {
                        // If the writer goes away while we're still reading from the store we'll receive this exception
                    }

                    hasData = this.AutoOpenAllStreams ? this.messageReader.MoveNext() : this.messageReader.MoveNext(this.enabledStreams);
                    if (acquired)
                    {
                        this.messageReader.DataReady.ReleaseMutex();
                    }

                    if (!hasData)
                    {
                        return false;
                    }
                }

                var messageTime = this.useOriginatingTime ? this.messageReader.Current.OriginatingTime : this.messageReader.Current.CreationTime;
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
            while (this.AutoOpenAllStreams || this.enabledStreams.Count() > 0);

            return false;
        }

        /// <summary>
        /// Reads the next message from any one of the enabled streams (in serialized form) into the specified buffer.
        /// </summary>
        /// <param name="buffer">A buffer to read into.</param>
        /// <returns>Number of bytes read into the specified buffer.</returns>
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
                if (!this.largeMessageReader.MoveNext())
                {
                    throw new ArgumentException($"Invalid index entry (extent: {extentId}, position: {indexEntry.Position}, current: {this.largeMessageReader.CurrentExtentId})");
                }

                return this.largeMessageReader.Read(ref buffer);
            }

            return this.messageReader.Read(ref buffer);
        }

        /// <summary>
        /// Reads the message from the specified position, without changing the current cursor position.
        /// Cannot be used together with MoveNext/Read.
        /// </summary>
        /// <param name="indexEntry">The position to read from.</param>
        /// <param name="buffer">A buffer to read into.</param>
        /// <returns>Number of bytes read into the specified buffer.</returns>
        public int ReadAt(IndexEntry indexEntry, ref byte[] buffer)
        {
            if (indexEntry.ExtentId < 0)
            {
                var extentId = indexEntry.ExtentId - int.MinValue;
                this.largeMessageReader.Seek(extentId, indexEntry.Position);
                if (!this.largeMessageReader.MoveNext())
                {
                    throw new ArgumentException($"Invalid index entry (extent: {indexEntry.ExtentId - int.MinValue}, position: {indexEntry.Position}, current: {this.largeMessageReader.CurrentExtentId})");
                }

                return this.largeMessageReader.Read(ref buffer);
            }

            this.messageReader.Seek(indexEntry.ExtentId, indexEntry.Position);
            if (!this.messageReader.MoveNext())
            {
                throw new ArgumentException($"Invalid index entry (extent: {indexEntry.ExtentId}, position: {indexEntry.Position}, current: {this.messageReader.CurrentExtentId})");
            }

            return this.messageReader.Read(ref buffer);
        }

        /// <summary>
        /// Returns the position of the next message from any one of the enabled streams.
        /// </summary>
        /// <returns>The position of the message, excluding the envelope.</returns>
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
                indexEntry.CreationTime = this.messageReader.Current.CreationTime;
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
            if (this.isIndexedStream.TryGetValue(id, out bool isIndexed))
            {
                return isIndexed;
            }

            if (this.metadataCache.Resource.TryGet(id, out PsiStreamMetadata meta))
            {
                isIndexed = meta.IsIndexed;
                this.isIndexedStream.Add(id, isIndexed);
                return isIndexed;
            }

            return false;
        }
    }
}
