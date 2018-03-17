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
