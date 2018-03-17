// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Data
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Schema;

    /// <summary>
    /// Represents the common elements of JSON data stores.
    /// </summary>
    public abstract class JsonStoreBase : IDisposable
    {
        /// <summary>
        /// JSON Schema for validating catalogs.
        /// </summary>
        public const string CatalogSchemaString = @"{
            ""$id"": ""http://www.microsoft.com/json-catalog-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""array"",
            ""items"": {
                ""$ref"": ""http://www.microsoft.com/json-metadata-schema#""
            }
        }";

        /// <summary>
        /// JSON Schema for validating data streams.
        /// </summary>
        public const string DataSchemaString = @"{
            ""$id"": ""http://www.microsoft.com/json-data-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""array"",
            ""items"": {
                ""$ref"": ""http://www.microsoft.com/json-message-schema#""
            }
        }";

        /// <summary>
        /// Default extension for the underlying file.
        /// </summary>
        public const string DefaultExtension = ".json";

        /// <summary>
        /// JSON Schema for validating message envelopes.
        /// </summary>
        public const string EnvelopeSchemaString = @"{
            ""$id"": ""http://www.microsoft.com/json-envelope-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""SourceId"": {
                    ""type"": ""integer"",
                    ""minimum"": 0
                },
                ""SequenceId"": {
                    ""type"": ""integer"",
                    ""minimum"": 0
                },
                ""OriginatingTime"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""Time"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                }
             },
            ""additionalProperties"": false,
            ""required"": [ ""SourceId"", ""SequenceId"", ""OriginatingTime"", ""Time"" ]
        }";

        /// <summary>
        /// JSON Schema for validating messages.
        /// </summary>
        public const string MessageSchemaString = @"{
            ""$id"": ""http://www.microsoft.com/json-message-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""Envelope"": {
                    ""$ref"": ""http://www.microsoft.com/json-envelope-schema#""
                },
                ""Data"": {}
             },
            ""additionalProperties"": false,
            ""required"": [ ""Envelope"", ""Data"" ]
        }";

        /// <summary>
        /// JSON Schema for validating stream metadata.
        /// </summary>
        public const string MetadataSchemaString = @"{
            ""$id"": ""http://www.microsoft.com/json-metadata-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""Name"": {
                    ""type"": ""string""
                },
                ""Id"": {
                    ""type"": ""integer"",
                    ""minimum"": 0
                },
                ""TypeName"": {
                    ""type"": ""string""
                },
                ""PartitionName"": {
                    ""type"": ""string""
                },
                ""PartitionPath"": {
                    ""type"": ""string""
                },
                ""FirstMessageTime"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""LastMessageTime"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""FirstOriginatingMessageTime"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""LastOriginatingMessageTime"": {
                    ""type"": ""string"",
                    ""format"": ""date-time""
                },
                ""AverageMessageSize"": {
                    ""type"": ""integer"",
                    ""minimum"": 0
                },
                ""AverageLatency"": {
                    ""type"": ""integer"",
                    ""minimum"": 0
                },
                ""MessageCount"": {
                    ""type"": ""integer"",
                    ""minimum"": 0
                }
             }
        }";

        /// <summary>
        /// URI to catalog schema.
        /// </summary>
        public static readonly Uri CatalogSchemaUri = new Uri("http://www.microsoft.com/json-catalog-schema");

        /// <summary>
        /// URI to data stream schema.
        /// </summary>
        public static readonly Uri DataSchemaUri = new Uri("http://www.microsoft.com/json-data-schema");

        /// <summary>
        /// URI to message envelope schema.
        /// </summary>
        public static readonly Uri EnvelopeSchemaUri = new Uri("http://www.microsoft.com/json-envelope-schema");

        /// <summary>
        /// URI to message schema.
        /// </summary>
        public static readonly Uri MessageSchemaUri = new Uri("http://www.microsoft.com/json-message-schema");

        /// <summary>
        /// URI to stream metadata schema.
        /// </summary>
        public static readonly Uri MetadataSchemaUri = new Uri("http://www.microsoft.com/json-metadata-schema");

        /// <summary>
        /// JSON schema resolver preloaded with schemas suitable for reading and writing JSON stores.
        /// </summary>
        protected static readonly JSchemaPreloadedResolver PreloadedResolver;

        static JsonStoreBase()
        {
            // preload schemas
            PreloadedResolver = new JSchemaPreloadedResolver();
            PreloadedResolver.Add(EnvelopeSchemaUri, EnvelopeSchemaString);
            PreloadedResolver.Add(MessageSchemaUri, MessageSchemaString);
            PreloadedResolver.Add(MetadataSchemaUri, MetadataSchemaString);
            PreloadedResolver.Add(CatalogSchemaUri, CatalogSchemaString);
            PreloadedResolver.Add(DataSchemaUri, DataSchemaString);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStoreBase"/> class.
        /// </summary>
        /// <param name="dataSchemaString">JSON schema used to validate data stream.</param>
        /// <param name="extension">The extension for the underlying file.</param>
        /// <param name="preloadSchemas">Dictionary of URis to JSON schemas to preload before validating any JSON. Would likely include schemas references by the catalog and data schemas.</param>
        protected JsonStoreBase(string dataSchemaString, string extension, IDictionary<Uri, string> preloadSchemas)
            : this(extension)
        {
            if (string.IsNullOrWhiteSpace(dataSchemaString))
            {
                throw new ArgumentNullException(nameof(dataSchemaString));
            }

            // preload schemas
            if (preloadSchemas != null)
            {
                foreach (var preloadSchema in preloadSchemas)
                {
                    this.Resolver.Add(preloadSchema.Key, preloadSchema.Value);
                }
            }

            // load schema
            this.CatalogSchema = JSchema.Parse(CatalogSchemaString, this.Resolver);
            this.DataSchema = JSchema.Parse(dataSchemaString, this.Resolver);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStoreBase"/> class.
        /// </summary>
        /// <param name="extension">The extension for the underlying file.</param>
        protected JsonStoreBase(string extension)
        {
            this.Resolver = new JSchemaPreloadedResolver(PreloadedResolver);
            this.Extension = extension;
            this.Serializer = new JsonSerializer();
        }

        /// <summary>
        /// Gets or sets the catalog JSON schema.
        /// </summary>
        public JSchema CatalogSchema { get; protected set; }

        /// <summary>
        /// Gets or sets the data JSON schema.
        /// </summary>
        public JSchema DataSchema { get; protected set; }

        /// <summary>
        /// Gets or sets the underlying file extension.
        /// </summary>
        public string Extension { get; protected set; }

        /// <summary>
        /// Gets or sets the name of the data store.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the path of the data store.
        /// </summary>
        public string Path { get; protected set; }

        /// <summary>
        /// Gets or sets the JSON schema resolver.
        /// </summary>
        public JSchemaPreloadedResolver Resolver { get; protected set; }

        /// <summary>
        /// Gets or sets the JSON serializer.
        /// </summary>
        public JsonSerializer Serializer { get; protected set; }

        /// <inheritdoc />
        public abstract void Dispose();
    }
}
