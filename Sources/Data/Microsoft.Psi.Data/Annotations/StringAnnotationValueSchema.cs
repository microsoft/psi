// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Drawing;

    /// <summary>
    /// Represents a string annotation value schema.
    /// </summary>
    public class StringAnnotationValueSchema : AnnotationValueSchema<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringAnnotationValueSchema"/> class.
        /// </summary>
        /// <param name="defaultValue">The default value for new instances of the schema.</param>
        /// <param name="fillColor">The fill color.</param>
        /// <param name="textColor">The text color.</param>
        public StringAnnotationValueSchema(string defaultValue, Color fillColor, Color textColor)
            : base(defaultValue, fillColor, textColor)
        {
        }

        /// <inheritdoc/>
        public override string CreateValue(string value) => value;
    }
}
