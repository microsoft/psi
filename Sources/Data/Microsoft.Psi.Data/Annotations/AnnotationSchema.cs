// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an annotation schema that can be used when adding an annotation to a time-based annotated event.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotationSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchema"/> class.
        /// </summary>
        /// <param name="name">The name of the annotation schema.</param>
        public AnnotationSchema(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.InternalValues = new List<AnnotationSchemaValue>();
        }

        /// <summary>
        /// Gets or sets the name of the annotation schema.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the annotation schema is dynamic (i.e. new values can be added).
        /// </summary>
        [DataMember]
        public bool Dynamic { get; set; }

        /// <summary>
        /// Gets the collection of annotation schema values that define this annotation schema.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<AnnotationSchemaValue> Values => this.InternalValues.AsReadOnly();

        /// <summary>
        /// Gets or sets the collection of values that define this annotation schema.
        /// </summary>
        [DataMember(Name = "Values")]
        private List<AnnotationSchemaValue> InternalValues { get; set; }

        /// <summary>
        /// Creates and adds a new annotation schema value to the current annotation schema.
        /// </summary>
        /// <param name="value">The value of the annotation schema value.</param>
        /// <param name="color">The color to use when displaying annotations of this value.</param>
        /// <param name="description">The description of this annotation schema value.</param>
        /// <returns>The newly added annotation schema value.</returns>
        public AnnotationSchemaValue AddSchemaValue(string value, Color color, string description = null)
        {
            if (!this.IsValidValue(value))
            {
                throw new ArgumentOutOfRangeException("value", $"value ({value}) is not permitted in this schema.");
            }

            var schemaValue = new AnnotationSchemaValue(value, color, description);
            this.InternalValues.Add(schemaValue);
            return schemaValue;
        }

        /// <inheritdoc />
        public override bool Equals(object o)
        {
            var other = o as AnnotationSchema;
            return other != null && this.Name == other.Name && this.Dynamic == other.Dynamic && this.InternalValues.SequenceEqual(other.InternalValues);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (this.Name == null ? 0 : this.Name.GetHashCode()) ^ this.Dynamic.GetHashCode() ^ this.InternalValues.GetHashCode();
        }

        /// <summary>
        /// Determines if the given value is a valid value of an annotation schema value found in this annotation schema.
        /// </summary>
        /// <param name="value">The value of an annotation schema value to validate.</param>
        /// <returns>true if the specified value is found in one of the current annotation schema values; otherwise, false.</returns>
        public bool IsValidValue(string value)
        {
            return true;
        }

        /// <summary>
        /// Removes an annotation schema value from the current annotation schema.
        /// </summary>
        /// <param name="schemaValue">The annotation schema value to remove from teh current annotation schema.</param>
        public void RemoveSchemaValue(AnnotationSchemaValue schemaValue)
        {
            this.InternalValues.Remove(schemaValue);
        }
    }
}
