// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Json
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Serialization;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents a simple reader for JSON data stores.
    /// </summary>
    public class JsonSimpleReader : ISimpleReader, IDisposable
    {
        private readonly Dictionary<int, Action<JToken, Envelope>> outputs = new Dictionary<int, Action<JToken, Envelope>>();
        private readonly string extension;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSimpleReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="extension">The extension for the underlying file.</param>
        public JsonSimpleReader(string name, string path, string extension = JsonStoreBase.DefaultExtension)
            : this(extension)
        {
            this.OpenStore(name, path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSimpleReader"/> class.
        /// </summary>
        /// <param name="that">Existing <see cref="JsonSimpleReader"/> used to initialize new instance.</param>
        public JsonSimpleReader(JsonSimpleReader that)
            : this(that.Name, that.Path, that.extension)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSimpleReader"/> class.
        /// </summary>
        /// <param name="extension">The extension for the underlying file.</param>
        public JsonSimpleReader(string extension = JsonStoreBase.DefaultExtension)
        {
            this.extension = extension;
        }

        /// <inheritdoc />
        public IEnumerable<IStreamMetadata> AvailableStreams => this.Reader?.AvailableStreams;

        /// <inheritdoc />
        public string Name => this.Reader?.Name;

        /// <inheritdoc />
        public string Path => this.Reader?.Path;

        /// <summary>
        /// Gets or sets the underlying store reader.
        /// </summary>
        protected JsonStoreReader Reader { get; set; }

        /// <summary>
        /// Closes all open streams.
        /// </summary>
        public void CloseAllStreams()
        {
            this.outputs.Clear();
            this.Reader.CloseAllStreams();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Reader?.Dispose();
        }

        /// <inheritdoc />
        public virtual ISimpleReader OpenNew()
        {
            return new JsonSimpleReader(this);
        }

        /// <inheritdoc />
        public virtual void OpenStore(string name, string path, KnownSerializers serializers = null)
        {
            if (serializers != null)
            {
                throw new ArgumentException("Serializers are not used by JsonStoreReader and must be null.", nameof(serializers));
            }

            this.Reader = new JsonStoreReader(name, path, this.extension);
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStream<T>(string streamName, Action<T, Envelope> target, Func<T> allocator = null)
        {
            if (string.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (allocator != null)
            {
                throw new ArgumentException("Allocators are not used by JsonStoreReader and must be null.", nameof(allocator));
            }

            var metadata = this.Reader.OpenStream(streamName);

            if (this.outputs.ContainsKey(metadata.Id))
            {
                throw new ArgumentException($"Stream named '{streamName}' was already opened and can only be opened once.", nameof(streamName));
            }

            this.outputs[metadata.Id] = (token, envelope) => target(token.ToObject<T>(), envelope);

            return metadata;
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStreamIndex<T>(string streamName, Action<IndexEntry, Envelope> target)
        {
            throw new NotImplementedException("JsonStoreReader does not support indexing.");
        }

        /// <inheritdoc />
        public TimeInterval OriginatingTimeRange()
        {
            TimeInterval timeInterval = TimeInterval.Empty;
            foreach (var metadata in this.AvailableStreams)
            {
                var metadataTimeInterval = new TimeInterval(metadata.FirstMessageOriginatingTime, metadata.LastMessageOriginatingTime);
                timeInterval = TimeInterval.Coverage(new TimeInterval[] { timeInterval, metadataTimeInterval });
            }

            return timeInterval;
        }

        /// <inheritdoc />
        public T Read<T>(IndexEntry indexEntry)
        {
            throw new NotImplementedException("JsonStoreReader does not support indexing.");
        }

        /// <inheritdoc />
        public void Read<T>(IndexEntry indexEntry, ref T objectToReuse)
        {
            throw new NotImplementedException("JsonStoreReader does not support indexing.");
        }

        /// <inheritdoc />
        public void ReadAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default(CancellationToken))
        {
            bool hasMoreData = true;
            Envelope envelope;
            this.Reader.Seek(descriptor);
            while (hasMoreData)
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    return;
                }

                hasMoreData = this.Reader.MoveNext(out envelope);
                if (hasMoreData)
                {
                    JToken token;
                    hasMoreData = this.Reader.Read(out token);
                    this.outputs[envelope.SourceId](token, envelope);
                }
            }
        }
    }
}
