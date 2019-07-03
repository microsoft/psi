// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data.Json;

    /// <summary>
    /// Represents a partition from an annotation store.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotationPartition : Partition
    {
        private AnnotationPartition(Session session, string storeName, string storePath, string name)
            : base(session, storeName, storePath, name, typeof(AnnotationSimpleReader))
        {
        }

        private AnnotationPartition()
        {
        }

        /// <summary>
        /// Creates a new annotation partition given the specified parameters.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="storeName">The store name of this partition.</param>
        /// <param name="storePath">The store path of this partition.</param>
        /// <param name="definition">The annotated event definition to use when creating new annotated events in this partition.</param>
        /// <param name="name">The partition name.</param>
        /// <returns>The newly created annotation partition.</returns>
        public static AnnotationPartition Create(Session session, string storeName, string storePath, AnnotatedEventDefinition definition, string name = null)
        {
            using (var writer = new AnnotationSimpleWriter(definition))
            {
                writer.CreateStore(storeName, storePath);
                writer.CreateStream(new JsonStreamMetadata(definition.Name, 0, typeof(AnnotatedEvent).AssemblyQualifiedName, storeName, storePath), new List<Message<AnnotatedEvent>>());
                writer.WriteAll(ReplayDescriptor.ReplayAll);
            }

            return new AnnotationPartition(session, storeName, storePath, name);
        }

        /// <summary>
        /// Creates a new annotation partition given the specified parameters.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="storeName">The store name of this partition.</param>
        /// <param name="storePath">The store path of this partition.</param>
        /// <param name="name">The partition name.</param>
        /// <returns>The newly created annotation partition.</returns>
        public static AnnotationPartition CreateFromExistingStore(Session session, string storeName, string storePath, string name = null)
        {
            return new AnnotationPartition(session, storeName, storePath, name);
        }

        /// <summary>
        /// Overridable method to allow derived object to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected override void InitNew()
        {
            this.Reader = new AnnotationSimpleReader(this.StoreName, this.StorePath);
            base.InitNew();
        }
    }
}
