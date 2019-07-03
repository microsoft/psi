// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents an incremental, streaming speech recognition result which may be partial or final.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class StreamingSpeechRecognitionResult : SpeechRecognitionResult, IStreamingSpeechRecognitionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingSpeechRecognitionResult"/> class.
        /// </summary>
        /// <param name="isFinal"> Indicates whether this is a final or a partial result.</param>
        /// <param name="text">The recognized text of this result.</param>
        /// <param name="confidence"> The confidence score of the result.</param>
        /// <param name="alternates"> The list of alternates for this result, as text strings.</param>
        /// <param name="audio"> The audio buffer that formed this result.</param>
        /// <param name="duration"> The duration of the audio that produced this recognition result.</param>
        public StreamingSpeechRecognitionResult(bool isFinal, string text, double? confidence = null, IEnumerable<SpeechRecognitionAlternate> alternates = null, AudioBuffer? audio = null, TimeSpan? duration = null)
            : base(text, confidence, alternates, audio, duration)
        {
            this.IsFinal = isFinal;
        }

        /// <summary>
        /// Gets a value indicating whether this result is final (true), or if it is a partial hypothesis (false).
        /// </summary>
        [DataMember]
        public bool IsFinal { get; private set; }
    }
}