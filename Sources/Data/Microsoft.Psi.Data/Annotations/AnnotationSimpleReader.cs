// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Json;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents the default simple reader for Annotation data stores.
    /// </summary>
    public class AnnotationSimpleReader : JsonSimpleReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSimpleReader"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        public AnnotationSimpleReader(string name, string path)
            : base(name, path, AnnotationStoreCommon.DefaultExtension)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSimpleReader"/> class.
        /// </summary>
        public AnnotationSimpleReader()
            : base(AnnotationStoreCommon.DefaultExtension)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSimpleReader"/> class.
        /// </summary>
        /// <param name="that">Existing <see cref="AnnotationSimpleReader"/> used to initialize new instance.</param>
        public AnnotationSimpleReader(AnnotationSimpleReader that)
            : base(that)
        {
        }

        /// <summary>
        /// Gets the annotated event definition for this store.
        /// </summary>
        public AnnotatedEventDefinition Definition => ((AnnotationStoreReader)this.Reader)?.Definition;

        /// <inheritdoc />
        public override ISimpleReader OpenNew()
        {
            return new AnnotationSimpleReader(this);
        }

        /// <inheritdoc />
        public override void OpenStore(string name, string path, KnownSerializers serializers = null)
        {
            if (serializers != null)
            {
                throw new ArgumentException("Serializers are not used by JsonStoreReader and must be null.", nameof(serializers));
            }

            this.Reader = new AnnotationStoreReader(name, path);
        }
    }
}
