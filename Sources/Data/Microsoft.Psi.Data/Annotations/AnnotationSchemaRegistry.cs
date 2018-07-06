// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Provides a singleton resistry for annotation schemas.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotationSchemaRegistry
    {
        /// <summary>
        /// The singleton instance of the <see cref="AnnotationSchemaRegistry"/>.
        /// </summary>
        [IgnoreDataMember]
        public static readonly AnnotationSchemaRegistry Default = new AnnotationSchemaRegistry();

        private AnnotationSchemaRegistry()
        {
            this.InternalSchemas = new List<AnnotationSchema>();
            this.InitNew();
        }

        /// <summary>
        /// Gets collection of annotation schemas.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<AnnotationSchema> Schemas => this.InternalSchemas.AsReadOnly();

        [DataMember(Name = "Schemas")]
        private List<AnnotationSchema> InternalSchemas { get; set; }

        /// <summary>
        /// Registers an annotation schema with the registry.
        /// </summary>
        /// <param name="schema">The annotation schema to register.</param>
        public void Register(AnnotationSchema schema)
        {
            this.InternalSchemas.Add(schema);
        }

        /// <summary>
        /// Unregisters an annotation schema with the registry.
        /// </summary>
        /// <param name="schema">The annotation schema to unregister.</param>
        public void Unregister(AnnotationSchema schema)
        {
            this.InternalSchemas.Remove(schema);
        }

        /// <summary>
        /// Overridable method to allow derived object to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.InitNew();
        }
    }
}
