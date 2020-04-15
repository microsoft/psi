// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using Microsoft.Psi.Data.Json;

    /// <summary>
    /// Component that writes messages to a Annotation store.
    /// </summary>
    public class AnnotationExporter : JsonExporter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationExporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline that owns this instance.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to create a volatile data store.</param>
        /// <param name="definition">The annotated event definition used to create and validate annotated events for this store.</param>
        /// <param name="createSubdirectory">If true, a numbered sub-directory is created for this store.</param>
        internal AnnotationExporter(Pipeline pipeline, string name, string path, AnnotatedEventDefinition definition, bool createSubdirectory = true)
            : base(pipeline, name, new AnnotationStoreWriter(name, path, definition, createSubdirectory))
        {
        }
    }
}
