// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the common elements of Annotation data stores.
    /// </summary>
    public static class AnnotationStoreCommon
    {
        /// <summary>
        /// JSON Schema for validating annotations.
        /// </summary>
        public const string AnnotationSchema = @"{
            ""$id"": ""http://www.microsoft.com/annotation-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""Name"": {
                    ""type"": ""string""
                },
                ""Dynamic"": {
                    ""type"": ""boolean"",
                    ""default"": false
                },
                ""Values"": {
                    ""type"": ""array"",
                    ""minItems"": 1,
                    ""items"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""Value"": {
                                ""type"": [ ""null"", ""string"" ],
                            },
                            ""Color"": {
                                ""type"": ""string""
                            },
                            ""Description"": {
                                ""type"": [ ""null"", ""string"" ],
                            },
                            ""Shortcut"": {
                                ""type"": [ ""null"", ""string"" ],
                            }
                        }
                    },
                    ""uniqueItems"": true
                },
             },
            ""additionalProperties"": false,
            ""required"": [ ""Name"", ""Values"" ]
        }";

        /// <summary>
        /// JSON Schema for validating annotation data.
        /// </summary>
        public const string DataSchema = @"{
            ""$id"": ""http://www.microsoft.com/annotation-data-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""array"",
            ""items"": {
                ""$ref"": ""http://www.microsoft.com/annotation-message-schema#""
            }
        }";

        /// <summary>
        /// Default extension for Annotation stores.
        /// </summary>
        public const string DefaultExtension = ".pas";

        /// <summary>
        /// JSON Schema for validating annotation definitions.
        /// </summary>
        public const string DefinitionSchema = @"{
            ""$id"": ""http://www.microsoft.com/annotation-definition-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""Name"": {
                    ""type"": ""string""
                },
                ""Schemas"": {
                    ""type"": ""array"",
                    ""minItems"": 1,
                    ""items"": {
                        ""$ref"": ""http://www.microsoft.com/annotation-schema#""
                    }
                }
             },
            ""additionalProperties"": false,
            ""required"": [ ""Name"", ""Schemas"" ]
        }";

        /// <summary>
        /// JSON Schema for validating annotation messages.
        /// </summary>
        public const string MessageSchema = @"{
            ""$id"": ""http://www.microsoft.com/annotation-message-schema#"",
            ""$schema"": ""http://json-schema.org/draft-06/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""Envelope"": {
                    ""$ref"": ""http://www.microsoft.com/json-envelope-schema#""
                },
                ""Data"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""StartTime"": {
                            ""type"": ""string"",
                            ""format"": ""date-time""
                        },
                        ""EndTime"": {
                            ""type"": ""string"",
                            ""format"": ""date-time""
                        },
                        ""Annotations"": {
                            ""type"": ""array"",
                            ""minItems"": 1,
                            ""items"": {
                                ""type"": [ ""null"", ""string"" ]
                            }
                        }
                    }
                }
             }
        }";

        /// <summary>
        /// URI to annotation schema.
        /// </summary>
        public static readonly Uri AnnotationSchemaUri = new Uri("http://www.microsoft.com/annotation-schema");

        /// <summary>
        /// URI to annotation data schema.
        /// </summary>
        public static readonly Uri DataSchemaUri = new Uri("http://www.microsoft.com/annotation-data-schema");

        /// <summary>
        /// URI to annotation definition schema.
        /// </summary>
        public static readonly Uri DefinitionSchemaUri = new Uri("http://www.microsoft.com/annotation-definition-schema");

        /// <summary>
        /// URI to annotation message schema.
        /// </summary>
        public static readonly Uri MessageSchemaUri = new Uri("http://www.microsoft.com/annotation-message-schema");

        /// <summary>
        /// Preloaded JSON Schemas suitable for reading and writing Annotation stores.
        /// </summary>
        public static readonly Dictionary<Uri, string> PreloadSchemas;

        private static readonly string DefinitionFileName = "Definition";

        static AnnotationStoreCommon()
        {
            PreloadSchemas = new Dictionary<Uri, string>();
            PreloadSchemas.Add(AnnotationSchemaUri, AnnotationSchema);
            PreloadSchemas.Add(DefinitionSchemaUri, DefinitionSchema);
            PreloadSchemas.Add(MessageSchemaUri, MessageSchema);
        }

        /// <summary>
        /// Gets the definition file name for the specified application.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <returns>The definition file name.</returns>
        public static string GetDefinitionFileName(string appName)
        {
            return appName + "." + DefinitionFileName;
        }
    }
}
