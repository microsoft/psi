// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.IO;
    using Microsoft.Psi.Data.Json;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a reader for Annotation stores.
    /// </summary>
    public class AnnotationStoreReader : JsonStoreReader
    {
        private readonly AnnotatedEventDefinition definition;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationStoreReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        public AnnotationStoreReader(string name, string path)
            : base(name, path, AnnotationStoreCommon.DefaultExtension)
        {
            // load definition
            string metadataPath = System.IO.Path.Combine(this.Path, AnnotationStoreCommon.GetDefinitionFileName(this.Name) + this.Extension);
            using (var file = File.OpenText(metadataPath))
            using (var reader = new JsonTextReader(file))
            {
                this.definition = this.Serializer.Deserialize<AnnotatedEventDefinition>(reader);
            }
        }

        /// <summary>
        /// Gets the annotated event definition for this store.
        /// </summary>
        public AnnotatedEventDefinition Definition => this.definition;
    }
}
