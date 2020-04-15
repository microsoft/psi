// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System.Xml.Serialization;

    /// <summary>
    /// Represents information about a grammar.
    /// </summary>
    /// <remarks>
    /// This information may be used to define a set of files containing grammar definitions to be comsumed by a speech recognition component.
    /// </remarks>
    public class GrammarInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GrammarInfo"/> class.
        /// </summary>
        public GrammarInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GrammarInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the grammar.</param>
        /// <param name="fileName">The path to the grammar file.</param>
        public GrammarInfo(string name, string fileName)
        {
            this.Name = name;
            this.FileName = fileName;
        }

        /// <summary>
        /// Gets or sets the name of the grammar.
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path to the grammar file.
        /// </summary>
        [XmlAttribute("fileName")]
        public string FileName { get; set; }
    }
}
