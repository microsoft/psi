// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Annotations
{
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Extensions.Base;

    /// <summary>
    /// Provides a singleton resistry for annotation schemas.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotationSchemaRegistry : ObservableObject
    {
        /// <summary>
        /// The singleton instance of the <see cref="AnnotationSchemaRegistry"/>.
        /// </summary>
        [IgnoreDataMember]
        public static readonly AnnotationSchemaRegistry Default = new AnnotationSchemaRegistry();

        private ObservableCollection<AnnotationSchema> internalSchemas;
        private ReadOnlyObservableCollection<AnnotationSchema> schemas;

        private AnnotationSchemaRegistry()
        {
            this.internalSchemas = new ObservableCollection<AnnotationSchema>();
            this.InitNew();
        }

        /// <summary>
        /// Gets collection of annotation schemas.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyObservableCollection<AnnotationSchema> Schemas => this.schemas;

        [DataMember(Name = "Schemas")]
        private ObservableCollection<AnnotationSchema> InternalSchemas
        {
            get { return this.internalSchemas; }
            set { this.Set(nameof(this.Schemas), ref this.internalSchemas, value); }
        }

        /// <summary>
        /// Registers an annotation schema with the registry.
        /// </summary>
        /// <param name="schema">The annotation schema to register.</param>
        public void Register(AnnotationSchema schema)
        {
            this.internalSchemas.Add(schema);
        }

        /// <summary>
        /// Unregisters an annotation schema with the registry.
        /// </summary>
        /// <param name="schema">The annotation schema to unregister.</param>
        public void Unregister(AnnotationSchema schema)
        {
            this.internalSchemas.Remove(schema);
        }

        /// <summary>
        /// Overridable method to allow derived object to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
            this.schemas = new ReadOnlyObservableCollection<AnnotationSchema>(this.internalSchemas);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.InitNew();
        }
    }
}
