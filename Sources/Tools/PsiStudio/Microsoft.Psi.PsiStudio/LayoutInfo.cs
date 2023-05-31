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

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
