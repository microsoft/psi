// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    /// <summary>
    /// Defines an annotation value schema.
    /// </summary>
    public interface IAnnotationValueSchema
    {
        /// <summary>
        /// Gets the default value for this annotation value schema.
        /// </summary>
        /// <returns>The default value.</returns>
        public IAnnotationValue GetDefaultAnnotationValue();

        /// <summary>
        /// Creates an annotation value from a specified string.
        /// </summary>
        /// <param name="value">The annotation value string.</param>
        /// <returns>The annotation value.</returns>
        public IAnnotationValue CreateAnnotationValue(string value);

        /// <summary>
        /// Gets a value indicating whether a specified annotation value is valid.
        /// </summary>
        /// <param name="annotationValue">The annotation value.</param>
        /// <returns>True if the specified annotation value is valid, otherwise false.</returns>
        public bool IsValid(IAnnotationValue annotationValue);
    }
}
