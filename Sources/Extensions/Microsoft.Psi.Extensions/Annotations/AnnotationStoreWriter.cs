// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Annotations
{
    using System.IO;
    using Microsoft.Psi.Extensions.Data;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Schema;

    /// <summary>
    /// Represents a writer for Annotation stores.
    /// </summary>
    public class AnnotationStoreWriter : JsonStoreWriter
    {
        private readonly AnnotatedEventDefinition definition;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationStoreWriter"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="definition">The annotated event definition used to create and validate annotated events for this store.</param>
        /// <param name="createSubdirectory">If true, a numbered subdirectory is created for this store.</param>
        public AnnotationStoreWriter(string name, string path, AnnotatedEventDefinition definition, bool createSubdirectory = true)
            : base(name, path, AnnotationStoreCommon.DataSchema, createSubdirectory, AnnotationStoreCommon.DefaultExtension, AnnotationStoreCommon.PreloadSchemas)
        {
            this.definition = definition;
            this.definition.PropertyChanged += (s, e) => this.WriteDefinition();
            this.WriteDefinition();
        }

        /// <summary>
        /// Gets the annotated event definition for this store.
        /// </summary>
        public AnnotatedEventDefinition Definition => this.definition;

        private void WriteDefinition()
        {
            string metadataPath = System.IO.Path.Combine(this.Path, AnnotationStoreCommon.GetDefinitionFileName(this.Name) + this.Extension);
            using (var file = File.CreateText(metadataPath))
            using (var writer = new JsonTextWriter(file))
            using (var validatingWriter = new JSchemaValidatingWriter(writer))
            {
                validatingWriter.Schema = JSchema.Parse(AnnotationStoreCommon.DefinitionSchema, this.Resolver);
                validatingWriter.ValidationEventHandler += (s, e) => throw new InvalidDataException(e.Message);
                this.Serializer.Serialize(validatingWriter, this.definition);
            }
        }
    }
}
