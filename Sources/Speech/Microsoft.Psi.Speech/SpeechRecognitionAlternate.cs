// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a speech recognition alternate.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class SpeechRecognitionAlternate : ISpeechRecognitionAlternate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognitionAlternate"/> class.
        /// </summary>
        /// <param name="text">The text of this alternate.</param>
        /// <param name="confidence"> The confidence score of the alternate.</param>
        public SpeechRecognitionAlternate(string text, double? confidence = null)
        {
            this.Text = text;
            this.Confidence = confidence;
        }

        /// <summary>
        /// Gets or sets the text of this alternate.
        /// </summary>
        [DataMember]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of this alternate.
        /// </summary>
        [DataMember]
        public double? Confidence { get; set; }
    }
}