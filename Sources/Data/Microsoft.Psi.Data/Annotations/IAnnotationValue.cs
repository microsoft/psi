// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Drawing;

    /// <summary>
    /// Defines an annotation value in an untyped fashion.
    /// </summary>
    public interface IAnnotationValue
    {
        /// <summary>
        /// Gets a string representation of the annotation value.
        /// </summary>
        public string ValueAsString { get; }

        /// <summary>
        /// Gets the color for drawing the annotation value area's interior.
        /// </summary>
        public Color FillColor { get; }

        /// <summary>
        /// Gets the color for drawing the annotation value text.
        /// </summary>
        public Color TextColor { get; }
    }
}
