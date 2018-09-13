// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using Microsoft.Psi.Data.Json;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents the default simple writer for Annotation data stores.
    /// </summary>
    public class AnnotationSimpleWriter : JsonSimpleWriter
    {
        private readonly AnnotatedEventDefinition definition;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSimpleWriter"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="definition">The annotated event definition used to create and validate annotated events for this store.</param>
        /// <param name="createSubdirectory">If true, a numbered subdirectory is created for this store.</param>
        public AnnotationSimpleWriter(string name, string path, AnnotatedEventDefinition definition, bool createSubdirectory = true)
            : base(name, path, createSubdirectory, AnnotationStoreCommon.DefaultExtension)
        {
            this.definition = definition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSimpleWriter"/> class.
        /// </summary>
        /// <param name="definition">The annotated event definition used to create and validate annotated events for this store.</param>
        public AnnotationSimpleWriter(AnnotatedEventDefinition definition)
            : base(AnnotationStoreCommon.DefaultExtension)
        {
            this.definition = definition;
        }

        /// <summary>
        /// Gets the annotated event definition for this store.
        /// </summary>
        public AnnotatedEventDefinition Definition => ((AnnotationStoreWriter)this.Writer)?.Definition ?? this.definition;

        /// <inheritdoc />
        public override void CreateStore(string name, string path, bool createSubdirectory = true, KnownSerializers serializers = null)
        {
            if (serializers != null)
            {
                throw new ArgumentException("Serializers are not used by JsonSimpleWriter and must be null.", nameof(serializers));
            }

            this.Writer = new AnnotationStoreWriter(name, path, this.definition, createSubdirectory);
        }
    }
}
