// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using Microsoft.Psi;

    /// <summary>
    /// Component that reads messages from a \psi store and publishes them on streams.
    /// </summary>
    /// <remarks>
    /// Reads either at the full speed allowed by available resources or at the desired rate
    /// specified by the <see cref="Pipeline"/>. Instances of this class can be created using the
    /// <see cref="PsiStore.Open"/> method. The store metadata is available immediately after open
    /// (before the pipeline is running) via the <see cref="Importer.AvailableStreams"/> property.
    /// </remarks>
    public sealed class PsiImporter : Importer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PsiImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to open a volatile data store.</param>
        /// <param name="usePerStreamReaders">Flag indicating whether to use per-stream readers.</param>
        public PsiImporter(Pipeline pipeline, string name, string path, bool usePerStreamReaders)
            : base(pipeline, new PsiStoreStreamReader(name, path), usePerStreamReaders)
        {
        }
    }
}
