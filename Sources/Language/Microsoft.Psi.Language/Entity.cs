// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Language
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a single entity.
    /// </summary>
    /// <remarks>
    /// An entity represents a value extracted from a spoken utterance or textual input which may be
    /// required to perform some action. This class may be used to represent a single entity containing
    /// a type, value and a score.
    /// </remarks>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Entity
    {
        /// <summary>
        /// Gets or sets the value of the entity.
        /// </summary>
        [DataMember]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the entity type name.
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of the entity.
        /// </summary>
        [DataMember]
        public double? Score { get; set; }
    }
}
