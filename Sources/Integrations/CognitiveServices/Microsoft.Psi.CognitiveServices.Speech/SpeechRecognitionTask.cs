// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech.Service;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Represents a speech recognition task that is either in progress or queued awaiting recognition results.
    /// </summary>
    internal class SpeechRecognitionTask
    {
        // initialize recognitionResults with an initial empty result - the code assumes this!
        private List<SpeechResult> recognitionResults = new List<SpeechResult>() { new PartialRecognitionResult() };

        /// <summary>
        /// Gets or sets the time at which the utterance started.
        /// </summary>
        internal DateTime SpeechStartTime { get; set; }

        /// <summary>
        /// Gets or sets the time at which the utterance ended.
        /// </summary>
        internal DateTime SpeechEndTime { get; set; }

        /// <summary>
        /// Gets or sets an audio buffer containing the utterance.
        /// </summary>
        internal AudioBuffer Audio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the speech recognition task has been finalized.
        /// </summary>
        internal bool IsFinalized { get; set; }

        /// <summary>
        /// Gets a value indicating whether the end of speech has been detected.
        /// </summary>
        internal bool IsDoneSpeaking => this.SpeechEndTime != default;

        /// <summary>
        /// Adds the next recognition result received from the speech recognition service.
        /// </summary>
        /// <param name="recognitionResult">The recognition result to append.</param>
        /// <remarks>
        /// The recognition results are accumulated as they are received from the speech service, and
        /// may be used at a later time to build the partial and final speech recognition results.
        /// </remarks>
        internal void AppendResult(SpeechResult recognitionResult)
        {
            if ((recognitionResult is PartialRecognitionResult || (recognitionResult is RecognitionResult result && result.RecognitionStatus == RecognitionStatus.Success)) &&
                this.recognitionResults[this.recognitionResults.Count - 1] is PartialRecognitionResult)
            {
                // replace the immediately preceding partial result
                this.recognitionResults[this.recognitionResults.Count - 1] = recognitionResult;
            }
            else
            {
                // append the result to the list of results
                this.recognitionResults.Add(recognitionResult);
            }
        }

        /// <summary>
        /// Builds a partial <see cref="StreamingSpeechRecognitionResult"/> from the results returned by the recognizer so far.
        /// </summary>
        /// <returns>A <see cref="StreamingSpeechRecognitionResult"/> containing the partial recognition result.</returns>
        internal StreamingSpeechRecognitionResult BuildPartialSpeechRecognitionResult()
        {
            // this.recognitionResults should always contain at least one item - the field initializer ensures this
            var composedResult = this.recognitionResults[0].ToStreamingSpeechRecognitionResult();

            for (int i = 1; i < this.recognitionResults.Count; i++)
            {
                if (this.recognitionResults[i] is RecognitionResult result &&
                    result.RecognitionStatus != RecognitionStatus.Success)
                {
                    continue;
                }

                // compose two consecutive results
                composedResult = composedResult.Compose(this.recognitionResults[i].ToStreamingSpeechRecognitionResult());
            }

            return composedResult;
        }

        /// <summary>
        /// Builds a <see cref="StreamingSpeechRecognitionResult"/> from the results returned by the recognizer so far.
        /// </summary>
        /// <returns>A <see cref="StreamingSpeechRecognitionResult"/> containing the recognition result.</returns>
        internal StreamingSpeechRecognitionResult BuildSpeechRecognitionResult()
        {
            return this.BuildPartialSpeechRecognitionResult().ToFinal(this.Audio, this.SpeechEndTime - this.SpeechStartTime);
        }
    }
}
