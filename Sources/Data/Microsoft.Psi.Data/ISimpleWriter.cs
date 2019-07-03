// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Defines an interface for all simple writers of data stores.
    /// </summary>
    public interface ISimpleWriter
    {
        /// <summary>
        /// Gets the name of the data store.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the path of the data store.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Creates the specified store.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the persisted file will reside.</param>
        /// <param name="createSubdirectory">If true, a numbered subdirectory is created for this store.</param>
        /// <param name="serializers">Optional set of serialization configuration (known types, serializers, known assemblies).</param>
        void CreateStore(string name, string path, bool createSubdirectory = true, KnownSerializers serializers = null);

        /// <summary>
        /// Creates a stream of messages in the data store.
        /// </summary>
        /// <typeparam name="TData">The underlying type of the messaging in the data store.</typeparam>
        /// <param name="metadata">The metadata of the new stream.</param>
        /// <param name="source">The source of message to be written to the data store.</param>
        void CreateStream<TData>(IStreamMetadata metadata, IEnumerable<Message<TData>> source);

        /// <summary>
        /// Writes all of the created streams to the data store.
        /// </summary>
        /// <param name="descriptor">Replay descriptor defining the bounds to write.</param>
        /// <param name="cancelationToken">The token to monitor for cancellation requests.</param>
        void WriteAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default(CancellationToken));
    }
}
