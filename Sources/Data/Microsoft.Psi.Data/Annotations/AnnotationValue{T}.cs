// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Drawing;

    /// <summary>
    /// Represents an annotation value of a specific type.
    /// </summary>
    /// <typeparam name="T">The datatype of the value.</typeparam>
    public class AnnotationValue<T> : IAnnotationValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationValue{T}"/> class.
        /// </summary>
        /// <param name="value">The value of the schema value.</param>
        /// <param name="fillColor">The value of the fill color.</param>
        /// <param name="textColor">The value of the text color.</param>
        public AnnotationValue(T value, Color fillColor, Color textColor)
        {
            this.Value = value;
            this.FillColor = fillColor;
            this.TextColor = textColor;
        }

        /// <summary>
        /// Gets or sets the schema value.
        /// </summary>
        public T Value { get; set; }

        /// <inheritdoc/>
        public string ValueAsString => this.Value?.ToString();

        /// <summary>
        /// Gets the color for drawing the annotation attribute area's interior.
        /// </summary>
        public Color FillColor { get; }

        /// <summary>
        /// Gets the color for drawing the annotation attribute value's text.
        /// </summary>
        public Color TextColor { get; }
    }
}