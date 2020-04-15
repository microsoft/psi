// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Persistence;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents a reader for JSON data stores.
    /// </summary>
    public class JsonStoreReader : JsonStoreBase
    {
        private readonly List<JsonStreamMetadata> catalog = null;
        private readonly List<int> enabledStreams = new List<int>();

        private TimeInterval originatingTimeInterval;
        private ReplayDescriptor descriptor = ReplayDescriptor.ReplayAll;
        private bool hasMoreData = false;
        private Envelope envelope = default(Envelope);
        private JToken data = null;
        private StreamReader streamReader = null;
        private JsonReader jsonReader = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStoreReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="extension">The extension for the underlying file.</param>
        public JsonStoreReader(string name, string path, string extension = DefaultExtension)
            : base(extension)
        {
            this.Name = name;
            this.Path = StoreCommon.GetPathToLatestVersion(name, path);

            // load catalog
            string metadataPath = System.IO.Path.Combine(this.Path, StoreCommon.GetCatalogFileName(this.Name) + this.Extension);
            using (var file = File.OpenText(metadataPath))
            using (var reader = new JsonTextReader(file))
            {
                this.catalog = this.Serializer.Deserialize<List<JsonStreamMetadata>>(reader);
            }

            // compute originating time interval
            this.originatingTimeInterval = TimeInterval.Empty;
            foreach (var metadata in this.catalog)
            {
                var metadataTimeInterval = new TimeInterval(metadata.FirstMessageOriginatingTime, metadata.LastMessageOriginatingTime);
                this.originatingTimeInterval = TimeInterval.Coverage(new TimeInterval[] { this.originatingTimeInterval, metadataTimeInterval });
            }
        }

        /// <summary>
        /// Gets an enumerable of stream metadata contained in the underlying data store.
        /// </summary>
        public IEnumerable<JsonStreamMetadata> AvailableStreams => this.catalog;

        /// <summary>
        /// Gets the orginating time interval (earliest to latest) of the messages in the data store.
        /// </summary>
        public TimeInterval OriginatingTimeInterval => this.originatingTimeInterval;

        /// <summary>
        /// Closes the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        public void CloseStream(string streamName)
        {
            var metadata = this.GetMetadata(streamName);
            this.CloseStream(metadata.Id);
        }

        /// <summary>
        /// Closes the specified stream.
        /// </summary>
        /// <param name="id">The id of the stream.</param>
        public void CloseStream(int id)
        {
            this.enabledStreams.Remove(id);
        }

        /// <summary>
        /// Close all streams.
        /// </summary>
        public void CloseAllStreams()
        {
            this.enabledStreams.Clear();
        }

        /// <summary>
        /// Determines whether the data store contains the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>true if store contains the specified stream, otherwise false.</returns>
        public bool Contains(string streamName)
        {
            if (string.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            return this.catalog.FirstOrDefault(m => m.Name == streamName) != null;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            this.streamReader?.Dispose();
            this.streamReader = null;
            this.jsonReader = null;
        }

        /// <summary>
        /// Gets the stream metadata for the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The stream metadata.</returns>
        public JsonStreamMetadata GetMetadata(string streamName)
        {
            if (string.IsNullOrWhiteSpace(streamName))
            {
                throw new ArgumentNullException(nameof(streamName));
            }

            var metadata = this.catalog.FirstOrDefault(m => m.Name == streamName);
            if (metadata == null)
            {
                throw new ArgumentException($"Stream named '{streamName}' was not found.", nameof(streamName));
            }

            return metadata;
        }

        /// <summary>
        /// Gets the stream metadata for the specified stream.
        /// </summary>
        /// <param name="id">The id of the stream.</param>
        /// <returns>The stream metadata.</returns>
        public JsonStreamMetadata GetMetadata(int id)
        {
            var metadata = this.catalog.FirstOrDefault(m => m.Id == id);
            if (metadata == null)
            {
                throw new ArgumentException($"Stream id '{id}' was not found.", nameof(id));
            }

            return metadata;
        }

        /// <summary>
        /// Opens the stream for the specified stream.
        /// </summary>
        /// <param name="streamName">The name of the stream.</param>
        /// <returns>The stream metadata.</returns>
        public JsonStreamMetadata OpenStream(string streamName)
        {
            var metadata = this.GetMetadata(streamName);
            this.OpenStream(metadata);
            return metadata;
        }

        /// <summary>
        /// Opens the stream for the specified stream.
        /// </summary>
        /// <param name="id">The id of the stream.</param>
        /// <returns>The stream metadata.</returns>
        public JsonStreamMetadata OpenStream(int id)
        {
            var metadata = this.GetMetadata(id);
            this.OpenStream(metadata);
            return metadata;
        }

        /// <summary>
        /// Opens the stream for the specified stream.
        /// </summary>
        /// <param name="metadata">The metadata of the stream.</param>
        /// <returns>true if the stream was opened; otherwise false.</returns>
        public bool OpenStream(JsonStreamMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (this.enabledStreams.Contains(metadata.Id))
            {
                return false;
            }

            this.enabledStreams.Add(metadata.Id);
            return true;
        }

        /// <summary>
        /// Positions the reader to the next message.
        /// </summary>
        /// <param name="envelope">The envelope associated with the message read.</param>
        /// <returns>True if there are more messages, false if no more messages are available.</returns>
        public bool MoveNext(out Envelope envelope)
        {
            do
            {
                envelope = this.envelope;
                if (!this.hasMoreData)
                {
                    return false;
                }

                var messageTime = envelope.OriginatingTime;

                // read data
                this.hasMoreData = this.ReadData(out this.data);
                if (!this.hasMoreData)
                {
                    return false;
                }

                if (this.descriptor.Interval.PointIsWithin(messageTime) && this.enabledStreams.Contains(envelope.SourceId))
                {
                    // data was within time interval and stream was opened
                    return true;
                }
                else if (this.descriptor.Interval.Right < messageTime)
                {
                    // data was outside of time interval, close stream
                    this.CloseStream(envelope.SourceId);
                }

                // read closing object, opening object tags, envelope
                this.hasMoreData =
                    this.jsonReader.Read() && this.jsonReader.TokenType == JsonToken.StartObject &&
                    this.jsonReader.Read() && this.ReadEnvelope(out this.envelope);
            }
            while (this.enabledStreams.Count() > 0);

            return false;
        }

        /// <summary>
        /// Reads the next message from any one of the enabled streams (in serialized form) into the specified buffer.
        /// </summary>
        /// <param name="data">The data associated with the message read.</param>
        /// <returns>True if there are more messages, false if no more messages are available.</returns>
        public bool Read(out JToken data)
        {
            // read closing object, opening object tags, envelope
            this.hasMoreData =
                this.jsonReader.Read() && this.jsonReader.TokenType == JsonToken.StartObject &&
                this.jsonReader.Read() && this.ReadEnvelope(out this.envelope);
            data = this.data;
            return this.hasMoreData;
        }

        /// <summary>
        /// Seek to envelope in stream according to specified replay descriptor.
        /// </summary>
        /// <param name="descriptor">The replay descriptor.</param>
        public void Seek(ReplayDescriptor descriptor)
        {
            this.descriptor = descriptor;

            // load data
            string dataPath = System.IO.Path.Combine(this.Path, StoreCommon.GetDataFileName(this.Name) + this.Extension);
            this.streamReader?.Dispose();
            this.streamReader = File.OpenText(dataPath);
            this.jsonReader = new JsonTextReader(this.streamReader);

            // iterate through data store until we either reach the end or we find the start of the replay descriptor
            while (this.jsonReader.Read())
            {
                // data stores are arrays of messages, messages start as objects
                if (this.jsonReader.TokenType == JsonToken.StartObject)
                {
                    // read envelope
                    if (!this.jsonReader.Read() || !this.ReadEnvelope(out this.envelope))
                    {
                        throw new InvalidDataException("Messages must be an ordered object: {\"Envelope\": <Envelope>, \"Data\": <Data>}. Deserialization needs to read the envelope before the data to know what type of data to deserialize.");
                    }

                    if (this.descriptor.Interval.Left < this.envelope.OriginatingTime)
                    {
                        // found start of interval
                        break;
                    }

                    // skip data
                    if (!this.ReadData(out this.data))
                    {
                        throw new InvalidDataException("Messages must be an ordered object: {\"Envelope\": <Envelope>, \"Data\": <Data>}. Deserialization needs to read the envelope before the data to know what type of data to deserialize.");
                    }
                }
            }
        }

        private bool ReadData(out JToken data)
        {
            this.hasMoreData = this.jsonReader.TokenType == JsonToken.PropertyName && string.Equals(this.jsonReader.Value, "Data") && this.jsonReader.Read();
            data = this.hasMoreData ? JToken.ReadFrom(this.jsonReader) : null;
            this.hasMoreData = this.hasMoreData && this.jsonReader.TokenType == JsonToken.EndObject && this.jsonReader.Read();
            return this.hasMoreData;
        }

        private bool ReadEnvelope(out Envelope envelope)
        {
            this.hasMoreData = this.jsonReader.TokenType == JsonToken.PropertyName && string.Equals(this.jsonReader.Value, "Envelope") && this.jsonReader.Read();
            envelope = this.hasMoreData ? this.Serializer.Deserialize<Envelope>(this.jsonReader) : default(Envelope);
            if (this.hasMoreData)
            {
                var metadata = this.catalog.FirstOrDefault(m => m.Id == this.envelope.SourceId);
                if (metadata == null)
                {
                    throw new InvalidDataException($"Message source/stream id ({this.envelope.SourceId}) was not found in catalog.");
                }
            }

            this.hasMoreData = this.hasMoreData && this.jsonReader.TokenType == JsonToken.EndObject && this.jsonReader.Read();
            return this.hasMoreData;
        }
    }
}
