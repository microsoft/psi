// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents a speech recognition result.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class SpeechRecognitionResult : ISpeechRecognitionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechRecognitionResult"/> class.
        /// </summary>
        /// <param name="text">The recognized text of this result.</param>
        /// <param name="confidence"> The confidence score of the result.</param>
        /// <param name="alternates"> The list of alternates for this result, as text strings.</param>
        /// <param name="audio"> The audio buffer that formed this result.</param>
        /// <param name="duration"> The duration of the audio that produced this recognition result.</param>
        public SpeechRecognitionResult(string text, double? confidence = null, IEnumerable<SpeechRecognitionAlternate> alternates = null, AudioBuffer? audio = null, TimeSpan? duration = null)
        {
            this.Text = text;
            this.Confidence = confidence;
            this.Alternates = alternates?.ToArray() ?? new SpeechRecognitionAlternate[0];
            this.Audio = audio.GetValueOrDefault();
            this.Duration = duration;
        }

        /// <summary>
        /// Gets the recognized text of this SpeechRecognitionResult.
        /// </summary>
        [DataMember]
        public string Text { get; private set; }

        /// <summary>
        /// Gets the confidence score of the recognition result.
        /// </summary>
        [DataMember]
        public double? Confidence { get; private set; }

        /// <summary>
        /// Gets the alternates of this SpeechRecognitionResult.
        /// </summary>
        [DataMember]
        public ISpeechRecognitionAlternate[] Alternates { get; private set; }

        /// <summary>
        /// Gets the audio buffer that produced the recognition result. This may be empty
        /// where the audio is not available.
        /// </summary>
        [DataMember]
        public AudioBuffer Audio { get; private set; }

        /// <summary>
        /// Gets the duration of the audio that produced the recognition result.
        /// </summary>
        [DataMember]
        public TimeSpan? Duration { get; private set; }

        /// <summary>
        /// Returns a display string for this SpeechRecognitionResult.
        /// </summary>
        /// <returns>A display string for this SpeechRecognitionResult.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} ({1})", this.Text, this.Confidence);

            if (this.Alternates != null && this.Alternates.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("********* Alternates *********");
                for (int i = 0; i < this.Alternates.Length; ++i)
                {
                    sb.AppendFormat("[{0}] {1} ({2})", i, this.Alternates[i].Text, this.Alternates[i].Confidence);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}