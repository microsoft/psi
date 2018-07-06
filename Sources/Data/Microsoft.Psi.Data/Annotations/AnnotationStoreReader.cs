// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.IO;
    using Microsoft.Psi.Data.Json;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Schema;

    /// <summary>
    /// Represents a reader for Annotation stores.
    /// </summary>
    public class AnnotationStoreReader : JsonStoreReader
    {
        private readonly AnnotatedEventDefinition definition;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationStoreReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store</param>
        public AnnotationStoreReader(string name, string path)
            : base(name, path, AnnotationStoreCommon.DataSchema, AnnotationStoreCommon.DefaultExtension, AnnotationStoreCommon.PreloadSchemas)
        {
            // load definition
            string metadataPath = System.IO.Path.Combine(this.Path, AnnotationStoreCommon.GetDefinitionFileName(this.Name) + this.Extension);
            using (var file = File.OpenText(metadataPath))
            using (var reader = new JsonTextReader(file))
            using (var validatingReader = new JSchemaValidatingReader(reader))
            {
                validatingReader.Schema = JSchema.Parse(AnnotationStoreCommon.DefinitionSchema, this.Resolver);
                validatingReader.ValidationEventHandler += (s, e) => throw new InvalidDataException(e.Message);
                this.definition = this.Serializer.Deserialize<AnnotatedEventDefinition>(validatingReader);
            }
        }

        /// <summary>
        /// Gets the annotated event definition for this store.
        /// </summary>
        public AnnotatedEventDefinition Definition => this.definition;
    }
}
