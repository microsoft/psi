// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using Microsoft.Psi.Data.Json;

    /// <summary>
    /// Defines a component that plays back data from a JSON store.
    /// </summary>
    public class AnnotationGenerator : JsonGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationGenerator"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides.</param>
        public AnnotationGenerator(Pipeline pipeline, string name, string path)
            : base(pipeline, new AnnotationStoreReader(name, path))
        {
        }
    }
}
