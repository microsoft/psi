// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Persistence;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Schema;

    /// <summary>
    /// Represents a writer for JSON data stores.
    /// </summary>
    public class JsonStoreWriter : JsonStoreBase
    {
        private readonly Dictionary<int, JsonStreamMetadata> catalog = new Dictionary<int, JsonStreamMetadata>();

        private StreamWriter streamWriter = null;
        private JsonWriter jsonWriter = null;
        private JSchemaValidatingWriter validatingWriter = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStoreWriter"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store</param>
        /// <param name="dataSchemaString">JSON schema used to validate data stream.</param>
        /// <param name="createSubdirectory">If true, a numbered subdirectory is created for this store</param>
        /// <param name="extension">The extension for the underlying file.</param>
        /// <param name="preloadSchemas">Dictionary of URis to JSON schemas to preload before validating any JSON. Would likely include schemas references by the catalog and data schemas.</param>
        public JsonStoreWriter(string name, string path, string dataSchemaString, bool createSubdirectory = true, string extension = DefaultExtension, IDictionary<Uri, string> preloadSchemas = null)
            : base(dataSchemaString, extension, preloadSchemas)
        {
            ushort id = 0;
            this.Name = name;
            this.Path = System.IO.Path.GetFullPath(path);
            if (createSubdirectory)
            {
                // if the root directory already exists, look for the next available id
                if (Directory.Exists(this.Path))
                {
                    var existingIds = Directory.EnumerateDirectories(this.Path, this.Name + ".????")
                        .Select(d => d.Split('.').Last())
                        .Where(n => ushort.TryParse(n, out ushort i))
                        .Select(n => ushort.Parse(n));
                    id = (ushort)(existingIds.Count() == 0 ? 0 : existingIds.Max() + 1);
                }

                this.Path = System.IO.Path.Combine(this.Path, $"{this.Name}.{id:0000}");
            }

            if (!Directory.Exists(this.Path))
            {
                Directory.CreateDirectory(this.Path);
            }

            string dataPath = System.IO.Path.Combine(this.Path, StoreCommon.GetDataFileName(this.Name) + this.Extension);
            this.streamWriter = File.CreateText(dataPath);
            this.jsonWriter = new JsonTextWriter(this.streamWriter);
            this.validatingWriter = new JSchemaValidatingWriter(this.jsonWriter);
            this.validatingWriter.Schema = this.DataSchema;
            this.validatingWriter.ValidationEventHandler += (s, e) => throw new InvalidDataException(e.Message);
            this.validatingWriter.WriteStartArray();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStoreWriter"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store</param>
        /// <param name="createSubdirectory">If true, a numbered subdirectory is created for this store</param>
        public JsonStoreWriter(string name, string path, bool createSubdirectory = true)
        : this(name, path, JsonStoreBase.DataSchemaString, createSubdirectory)
        {
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            this.WriteCatalog();
            this.validatingWriter.WriteEndArray();
            this.streamWriter.Dispose();
            this.streamWriter = null;
            this.jsonWriter = null;
            this.validatingWriter = null;
        }

        /// <summary>
        /// Opens the stream for the specified stream
        /// </summary>
        /// <param name="metadata">The metadata of the stream.</param>
        /// <returns>The stream metadata.</returns>
        public JsonStreamMetadata OpenStream(JsonStreamMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            return this.OpenStream(metadata.Id, metadata.Name, metadata.TypeName);
        }

        /// <summary>
        /// Opens the stream for the specified stream
        /// </summary>
        /// <param name="streamId">The stream id.</param>
        /// <param name="streamName">The stream name.</param>
        /// <param name="typeName">The stream type name..</param>
        /// <returns>The stream metadata.</returns>
        public JsonStreamMetadata OpenStream(int streamId, string streamName, string typeName)
        {
            if (this.catalog.ContainsKey(streamId))
            {
                throw new InvalidOperationException($"The stream id {streamId} has already been registered with this writer.");
            }

            var metadata = new JsonStreamMetadata() { Id = streamId, Name = streamName, PartitionName = this.Name, PartitionPath = this.Path, TypeName = typeName };
            this.catalog[metadata.Id] = metadata;
            this.WriteCatalog(); // ensure catalog is up to date even if crashing later
            return metadata;
        }

        /// <summary>
        /// Writes the next message to the data store.
        /// </summary>
        /// <param name="data">The data associated with the message write.</param>
        /// <param name="envelope">The envelope associated with the message write.</param>
        public void Write(JToken data, Envelope envelope)
        {
            var metadata = this.catalog[envelope.SourceId];
            metadata.Update(envelope, data.ToString().Length);
            this.WriteMessage(data, envelope, this.validatingWriter);
        }

        private void WriteCatalog()
        {
            string metadataPath = System.IO.Path.Combine(this.Path, StoreCommon.GetCatalogFileName(this.Name) + this.Extension);
            using (var file = File.CreateText(metadataPath))
            using (var writer = new JsonTextWriter(file))
            using (var validatingWriter = new JSchemaValidatingWriter(writer))
            {
                validatingWriter.Schema = this.CatalogSchema;
                validatingWriter.ValidationEventHandler += (s, e) => throw new InvalidDataException(e.Message);
                this.Serializer.Serialize(validatingWriter, this.catalog.Values.ToList());
            }
        }

        private void WriteMessage(JToken data, Envelope envelope, JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Envelope");
            writer.WriteStartObject();
            writer.WritePropertyName("SourceId");
            writer.WriteValue(envelope.SourceId);
            writer.WritePropertyName("SequenceId");
            writer.WriteValue(envelope.SequenceId);
            writer.WritePropertyName("OriginatingTime");
            writer.WriteValue(envelope.OriginatingTime);
            writer.WritePropertyName("Time");
            writer.WriteValue(envelope.Time);
            writer.WriteEndObject();
            writer.WritePropertyName("Data");
            data.WriteTo(writer);
            writer.WriteEndObject();
        }
    }
}
