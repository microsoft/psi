// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Threading;
    using Microsoft.Psi;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents a stream reader for JSON data stores.
    /// </summary>
    public class JsonStreamReader : IStreamReader, IDisposable
    {
        private readonly Dictionary<int, Action<JToken, Envelope>> outputs = new Dictionary<int, Action<JToken, Envelope>>();
        private readonly string extension;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="extension">The extension for the underlying file.</param>
        public JsonStreamReader(string name, string path, string extension)
            : this(extension)
        {
            this.Reader = new JsonStoreReader(name, path, extension);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        public JsonStreamReader(string name, string path)
            : this(name, path, JsonStoreBase.DefaultExtension)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamReader"/> class.
        /// </summary>
        /// <param name="that">Existing <see cref="JsonStreamReader"/> used to initialize new instance.</param>
        public JsonStreamReader(JsonStreamReader that)
            : this(that.Name, that.Path, that.extension)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamReader"/> class.
        /// </summary>
        /// <param name="extension">The extension for the underlying file.</param>
        public JsonStreamReader(string extension = JsonStoreBase.DefaultExtension)
        {
            this.extension = extension;
        }

        /// <inheritdoc />
        public IEnumerable<IStreamMetadata> AvailableStreams => this.Reader?.AvailableStreams;

        /// <inheritdoc />
        public string Name => this.Reader?.Name;

        /// <inheritdoc />
        public string Path => this.Reader?.Path;

        /// <inheritdoc />
        public TimeInterval MessageOriginatingTimeInterval
        {
            get
            {
                TimeInterval timeInterval = TimeInterval.Empty;
                foreach (var metadata in this.AvailableStreams)
                {
                    var metadataTimeInterval = new TimeInterval(metadata.FirstMessageOriginatingTime, metadata.LastMessageOriginatingTime);
                    timeInterval = TimeInterval.Coverage(new TimeInterval[] { timeInterval, metadataTimeInterval });
                }

                return timeInterval;
            }
        }

        /// <inheritdoc />
        public TimeInterval MessageCreationTimeInterval
        {
            get
            {
                TimeInterval timeInterval = TimeInterval.Empty;
                foreach (var metadata in this.AvailableStreams)
                {
                    var metadataTimeInterval = new TimeInterval(metadata.FirstMessageCreationTime, metadata.LastMessageCreationTime);
                    timeInterval = TimeInterval.Coverage(new TimeInterval[] { timeInterval, metadataTimeInterval });
                }

                return timeInterval;
            }
        }

        /// <inheritdoc />
        public TimeInterval StreamTimeInterval
        {
            get
            {
                TimeInterval timeInterval = TimeInterval.Empty;
                foreach (var metadata in this.AvailableStreams)
                {
                    var metadataTimeInterval = new TimeInterval(metadata.OpenedTime, metadata.ClosedTime);
                    timeInterval = TimeInterval.Coverage(new TimeInterval[] { timeInterval, metadataTimeInterval });
                }

                return timeInterval;
            }
        }

        /// <inheritdoc/>
        public long? Size => this.Reader?.Size;

        /// <inheritdoc/>
        public int? StreamCount => this.Reader?.AvailableStreams.Count();

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
        public virtual IStreamReader OpenNew()
        {
            return new JsonStreamReader(this);
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStream<T>(string streamName, Action<T, Envelope> target, Func<T> allocator = null, Action<T> deallocator = null, Action<SerializationException> errorHandler = null)
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
                throw new NotSupportedException($"Allocators are not supported by {nameof(JsonStreamReader)} and must be null.");
            }

            if (deallocator != null)
            {
                throw new NotSupportedException($"Deallocators are not supported by {nameof(JsonStreamReader)} and must be null.");
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
        public IStreamMetadata OpenStreamIndex<T>(string streamName, Action<Func<IStreamReader, T>, Envelope> target, Func<T> allocator = null)
        {
            throw new NotSupportedException($"{nameof(JsonStreamReader)} does not support indexing.");
        }

        /// <inheritdoc />
        public void ReadAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default)
        {
            bool hasMoreData = true;
            this.Reader.Seek(descriptor);
            while (hasMoreData)
            {
                if (cancelationToken.IsCancellationRequested)
                {
                    return;
                }

                hasMoreData = this.Reader.MoveNext(out Envelope envelope);
                if (hasMoreData)
                {
                    hasMoreData = this.Reader.Read(out JToken token);
                    this.outputs[envelope.SourceId](token, envelope);
                }
            }
        }

        /// <inheritdoc />
        public void Seek(TimeInterval interval, bool useOriginatingTime = false)
        {
            throw new NotSupportedException($"{nameof(JsonStreamReader)} does not support seeking.");
        }

        /// <inheritdoc />
        public bool MoveNext(out Envelope envelope)
        {
            throw new NotSupportedException($"{nameof(JsonStreamReader)} does not support stream-style access.");
        }

        /// <inheritdoc />
        public bool IsLive()
        {
            throw new NotSupportedException($"{nameof(JsonStreamReader)} does not support stream-style access.");
        }

        /// <inheritdoc />
        public IStreamMetadata GetStreamMetadata(string name)
        {
            throw new NotSupportedException($"{nameof(JsonStreamReader)} does not support metadata.");
        }

        /// <inheritdoc />
        public T GetSupplementalMetadata<T>(string streamName)
        {
            throw new NotSupportedException($"{nameof(JsonStreamReader)} does not support supplemental metadata.");
        }

        /// <inheritdoc />
        public bool ContainsStream(string name)
        {
            throw new NotSupportedException($"{nameof(JsonStreamReader)} does not support this API.");
        }
    }
}
