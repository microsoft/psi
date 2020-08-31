// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    /// <summary>
    /// Represents a value in a finite annotation schema.
    /// </summary>
    /// <typeparam name="T">The datatype of the value.</typeparam>
    public class FiniteAnnotationSchemaValue<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FiniteAnnotationSchemaValue{T}"/> class.
        /// </summary>
        /// <param name="value">The value of the schema value.</param>
        /// <param name="metadata">The metadata for the value, or null if the schema's metadata should be used.</param>
        public FiniteAnnotationSchemaValue(T value, AnnotationSchemaValueMetadata metadata = null)
        {
            this.Value = value;
            this.Metadata = metadata;
        }

        /// <summary>
        /// Gets or sets the schema value.
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the finite annotation schema value.
        /// </summary>
        public AnnotationSchemaValueMetadata Metadata { get; set; }
    }
}