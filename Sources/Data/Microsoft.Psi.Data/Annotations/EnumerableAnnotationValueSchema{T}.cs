// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents an enumerable annotation value schema.
    /// </summary>
    /// <typeparam name="T">The datatype of the values in the schema.</typeparam>
    public class EnumerableAnnotationValueSchema<T> : IEnumerableAnnotationValueSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableAnnotationValueSchema{T}"/> class.
        /// </summary>
        /// <param name="possibleValues">The set of possible annotation values for the schema.</param>
        /// <param name="defaultValue">The default value for new instances of the schema.</param>
        public EnumerableAnnotationValueSchema(List<AnnotationValue<T>> possibleValues, T defaultValue)
        {
            this.PossibleValues = possibleValues;
            this.DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the default value for this schema.
        /// </summary>
        public T DefaultValue { get; }

        /// <summary>
        /// Gets or sets the set of possible annotation values.
        /// </summary>
        public List<AnnotationValue<T>> PossibleValues { get; set; }

        /// <inheritdoc/>
        public IAnnotationValue CreateAnnotationValue(string value)
        {
            var annotationValue = this.PossibleValues.FirstOrDefault(v => v.Value.ToString() == value);
            if (annotationValue == null)
            {
                throw new Exception("Cannot convert specified string into a valid annotation value.");
            }
            else
            {
                return annotationValue;
            }
        }

        /// <inheritdoc/>
        public IAnnotationValue GetDefaultAnnotationValue() => this.PossibleValues.First(v => v.Value.Equals(this.DefaultValue));

        /// <inheritdoc/>
        public bool IsValid(IAnnotationValue annotationValue) => this.PossibleValues.Any(v => v.Equals(annotationValue));

        /// <inheritdoc/>
        public IEnumerable<IAnnotationValue> GetPossibleAnnotationValues()
            => this.PossibleValues;
    }
}