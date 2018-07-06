// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Drawing;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an annotation schema value that can be used to construct an annotation schema.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotationSchemaValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchemaValue"/> class.
        /// </summary>
        /// <param name="value">The value of this annotation schema value.</param>
        /// <param name="color">The color to use when displaying annotations of this value.</param>
        /// <param name="description">The description of this annotation schema value.</param>
        /// <param name="shortcut">The keyboard shortcut of this annotation schema value.</param>
        public AnnotationSchemaValue(string value, Color color, string description = null, string shortcut = null)
        {
            this.Value = value;
            this.Color = color;
            this.Description = description == null ? (value == null ? "null" : value.ToString()) : description;
            this.Shortcut = shortcut;
        }

        /// <summary>
        /// Gets or sets the color to use when displaying annotations of this value.
        /// </summary>
        [DataMember]
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets the description of this annotation schema value.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the keyboard shortcut of this annotation schema value.
        /// </summary>
        [DataMember]
        public string Shortcut { get; set; }

        /// <summary>
        /// Gets or sets the value of this annotation schema value.
        /// </summary>
        [DataMember]
        public string Value { get; set; }

        /// <inheritdoc />
        public override bool Equals(object o)
        {
            var other = o as AnnotationSchemaValue;
            return other != null && this.Color == other.Color && this.Description == other.Description && this.Shortcut == other.Shortcut && this.Value == other.Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return
                (this.Value == null ? 0 : this.Value.GetHashCode()) ^
                this.Color.GetHashCode() ^
                (this.Description == null ? 0 : this.Description.GetHashCode()) ^
                (this.Shortcut == null ? 0 : this.Shortcut.GetHashCode());
        }
    }
}
