// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Language
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a single intent.
    /// </summary>
    /// <remarks>
    /// An intent is typically determined from a spoken utterance or textual input. This class
    /// may be used to represent a single intent containing a string value and a score.
    /// </remarks>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class Intent
    {
        /// <summary>
        /// Gets or sets the intent string.
        /// </summary>
        [DataMember]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of the intent.
        /// </summary>
        [DataMember]
        public double? Score { get; set; }
    }
}
