// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Drawing;

    /// <summary>
    /// Represents an annotation schema.
    /// </summary>
    /// <typeparam name="T">The datatype of the values contained in the schema.</typeparam>
    public class AnnotationSchema<T> : IAnnotationSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchema{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the finite annotation schema.</param>
        /// <param name="defaultValue">The default value for new instances of the schema.</param>
        /// <param name="metadata">The metadata to use for all values in the schema unless overridden by a specific value's metadata.</param>
        public AnnotationSchema(string name, T defaultValue, AnnotationSchemaValueMetadata metadata = null)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
            this.Metadata = metadata ?? CreateDefaultMetadata();
        }

        /// <inheritdoc />
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or sets the default value for this annotation schema.
        /// </summary>
        public virtual T DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the finite annotation schema value.
        /// </summary>
        public AnnotationSchemaValueMetadata Metadata { get; set; }

        /// <inheritdoc/>
        public virtual bool IsFiniteAnnotationSchema => false;

        /// <summary>
        /// Gets the schema metadata for a given schema value.
        /// </summary>
        /// <param name="value">The value for which to retrieve the schema value metadata.</param>
        /// <returns>The metadata for the schema value, or null if the valule is not valid.</returns>
        public virtual AnnotationSchemaValueMetadata GetMetadata(T value)
        {
            return this.Metadata;
        }

        /// <summary>
        /// Determines whether a value is a valid schema value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>True if the value is a valid schema value, otherwise false.</returns>
        public virtual bool IsValid(T value)
        {
            return true;
        }

        private static AnnotationSchemaValueMetadata CreateDefaultMetadata()
        {
            return new AnnotationSchemaValueMetadata()
            {
                BorderColor = Color.LightGray,
                BorderWidth = 1,
                Description = null,
                FillColor = Color.DarkGray,
                TextColor = Color.White,
            };
        }
    }
}