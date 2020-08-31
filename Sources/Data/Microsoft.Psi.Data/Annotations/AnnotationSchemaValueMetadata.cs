// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Drawing;

    /// <summary>
    /// Represents the metadata associated with a schema value.
    /// </summary>
    public class AnnotationSchemaValueMetadata
    {
        /// <summary>
        /// Gets or sets the color for drawing the annotation area's border.
        /// </summary>
        public Color BorderColor { get; set; }

        /// <summary>
        /// Gets or sets the color for drawing the annotation area's interior.
        /// </summary>
        public Color FillColor { get; set; }

        /// <summary>
        /// Gets or sets the color for drawing the annotation's text.
        /// </summary>
        public Color TextColor { get; set; }

        /// <summary>
        /// Gets or sets the width of the annotation's border.
        /// </summary>
        public double BorderWidth { get; set; }

        /// <summary>
        /// Gets or sets a description of the value.
        /// </summary>
        public string Description { get; set; }
    }
}