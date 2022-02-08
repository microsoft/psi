// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Drawing;

    /// <summary>
    /// Represents an annotation value schema.
    /// </summary>
    /// <typeparam name="T">The datatype of the values contained in the schema.</typeparam>
    public abstract class AnnotationValueSchema<T> : IAnnotationValueSchema
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationValueSchema{T}"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value for new instances of the schema.</param>
        /// <param name="fillColor">The fill color.</param>
        /// <param name="textColor">The text color.</param>
        public AnnotationValueSchema(T defaultValue, Color fillColor, Color textColor)
        {
            this.DefaultValue = defaultValue;
            this.FillColor = fillColor;
            this.TextColor = textColor;
        }

        /// <summary>
        /// Gets the default value for this schema.
        /// </summary>
        public T DefaultValue { get; }

        /// <summary>
        /// Gets the fill color.
        /// </summary>
        public Color FillColor { get; }

        /// <summary>
        /// Gets the text color.
        /// </summary>
        public Color TextColor { get; }

        /// <inheritdoc/>
        public IAnnotationValue GetDefaultAnnotationValue() => new AnnotationValue<T>(this.DefaultValue, this.FillColor, this.TextColor);

        /// <inheritdoc/>
        public IAnnotationValue CreateAnnotationValue(string value) => new AnnotationValue<T>(this.CreateValue(value), this.FillColor, this.TextColor);

        /// <inheritdoc/>
        public bool IsValid(IAnnotationValue annotationValue)
            => annotationValue.FillColor.Equals(this.FillColor) && annotationValue.TextColor.Equals(this.TextColor);

        /// <summary>
        /// Creates a value from the specified string.
        /// </summary>
        /// <param name="value">The value specified as a string.</param>
        /// <returns>The value.</returns>
        public abstract T CreateValue(string value);
    }
}