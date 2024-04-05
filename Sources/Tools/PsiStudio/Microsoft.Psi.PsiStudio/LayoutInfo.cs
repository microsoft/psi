// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    /// <summary>
    /// Information about a PsiStudio layout file.
    /// </summary>
    public class LayoutInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the layout.</param>
        /// <param name="path">The path to the layout file.</param>
        public LayoutInfo(string name, string path)
        {
            this.Name = name;
            this.Path = path;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutInfo"/> class for serialization.
        /// </summary>
        public LayoutInfo()
        {
        }

        /// <summary>
        /// Gets or sets the name of the layout.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path to the layout file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the first value is equivalent to the second value. False otherwise.</returns>
        public static bool operator ==(LayoutInfo a, LayoutInfo b)
        {
            if (object.ReferenceEquals(a, null))
            {
                return object.ReferenceEquals(b, null);
            }

            return a.Equals(b);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns>True if the first value is not equivalent to the second value. False otherwise.</returns>
        public static bool operator !=(LayoutInfo a, LayoutInfo b)
        {
            return !(a == b);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var other = obj as LayoutInfo;
            if (other == null)
            {
                return false;
            }

            return this.Name == other.Name && this.Path == other.Path;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (this.Name?.GetHashCode() ?? 0);
            hash = hash * 23 + (this.Path?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
