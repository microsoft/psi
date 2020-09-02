// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a finite annotation schema.
    /// </summary>
    /// <typeparam name="T">The datatype of the values in the schema.</typeparam>
    public class FiniteAnnotationSchema<T> : AnnotationSchema<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FiniteAnnotationSchema{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the finite annotation schema.</param>
        /// <param name="schemaValues">The collection of values for the schema.</param>
        /// <param name="defaultValue">The default value for new instances of the schema.</param>
        public FiniteAnnotationSchema(string name, List<FiniteAnnotationSchemaValue<T>> schemaValues, T defaultValue)
            : base(name, defaultValue)
        {
            this.SchemaValues = schemaValues;
        }

        /// <inheritdoc/>
        public override bool IsFiniteAnnotationSchema => true;

        /// <summary>
        /// Gets or sets the list of schema values in the schema.
        /// </summary>
        public List<FiniteAnnotationSchemaValue<T>> SchemaValues { get; set; }

        /// <summary>
        /// Gets the collection of valid values for the finite annotation.
        /// </summary>
        [IgnoreDataMember]
        public IEnumerable<T> Values => this.SchemaValues.Select(v => v.Value);

        /// <inheritdoc />
        public override bool IsValid(T value)
        {
            return this.SchemaValues.Any(v => v.Value.Equals(value));
        }

        /// <inheritdoc />
        public override AnnotationSchemaValueMetadata GetMetadata(T value)
        {
            FiniteAnnotationSchemaValue<T> schemaValue = this.SchemaValues.FirstOrDefault(v => v.Value.Equals(value));
            return schemaValue != null && schemaValue.Metadata != null ? schemaValue.Metadata : this.Metadata;
        }

        /// <summary>
        /// Sets the metadata for a given annotation schema value.
        /// </summary>
        /// <param name="value">The schema value for which to set the metadata.</param>
        /// <param name="metadata">The metadata to associate with the value.</param>
        public void SetMetadata(T value, AnnotationSchemaValueMetadata metadata)
        {
            FiniteAnnotationSchemaValue<T> schemaValue = this.SchemaValues.First(v => v.Value.Equals(value));
            schemaValue.Metadata = metadata;
        }
    }
}