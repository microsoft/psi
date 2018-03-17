// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace MultiModalSpeechDetection
{
    using System;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;

    public static class OperatorExtensions
    {
        // This field is required to run the sample and must be a valid key which may
        // be obtained by signing up at https://azure.microsoft.com/en-us/try/cognitive-services/?api=speech-api.
        private static string bingSubscriptionKey = string.Empty;

        /// <summary>
        /// Define an operator that uses the Bing Speech Recognizer to translate from audio to text
        /// </summary>
        /// <param name="audio">Our audio stream</param>
        /// <param name="speechDetector">A stream that indicates whether the user is speaking</param>
        /// <returns>A new producer with the translated speech and time stamp</returns>
        public static IProducer<(string, TimeSpan)> SpeechToText(this IProducer<AudioBuffer> audio, IProducer<bool> speechDetector)
        {
            var speechRecognizer = new BingSpeechRecognizer(audio.Out.Pipeline, new BingSpeechRecognizerConfiguration() { SubscriptionKey = bingSubscriptionKey });
            audio.Join(speechDetector).PipeTo(speechRecognizer);
            return speechRecognizer.Where(r => r.IsFinal).Select(r => (r.Text, r.Duration.Value));
        }

        /// <summary>
        /// Defines an operator that will hold a signal (i.e. smooth out the signal)
        /// </summary>
        /// <param name="source">Source of the signal</param>
        /// <param name="threshold">Threshold below which the signal is considered off</param>
        /// <param name="decay">Speed at which signal decays</param>
        /// <returns>Returns true if signal is on, false otherwise</returns>
        public static IProducer<bool> Hold(this IProducer<double> source, double threshold, double decay = 0.2)
        {
            double maxValue = 0;

            return source.Select(
                newValue =>
                {
                    if (newValue > maxValue && newValue > threshold)
                    {
                        maxValue = newValue;
                    }
                    else
                    {
                        maxValue = maxValue * (1 - decay) + newValue * decay;
                    }

                    return maxValue >= threshold;
                });
        }
    }
}
