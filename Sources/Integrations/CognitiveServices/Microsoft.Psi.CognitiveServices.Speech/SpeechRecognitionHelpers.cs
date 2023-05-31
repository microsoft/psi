// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech.Service;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Static class containing helper methods for processing speech recognition results.
    /// </summary>
    internal static class SpeechRecognitionHelpers
    {
        /// <summary>
        /// Converts a <see cref="SpeechResult"/> to a <see cref="StreamingSpeechRecognitionResult"/>.
        /// </summary>
        /// <param name="result">The <see cref="SpeechResult"/>.</param>
        /// <param name="rankOrder">A value indicating whether to score the alternates by their rank-order.</param>
        /// <returns>The <see cref="StreamingSpeechRecognitionResult"/>.</returns>
        internal static StreamingSpeechRecognitionResult ToStreamingSpeechRecognitionResult(this SpeechResult result, bool rankOrder = true)
        {
            if (result is PartialRecognitionResult partialResult)
            {
                return new StreamingSpeechRecognitionResult(
                    false,
                    partialResult.Text,
                    1.0,
                    new SpeechRecognitionAlternate[] { new SpeechRecognitionAlternate(partialResult.Text, 1.0) },
                    null,
                    result.Duration);
            }
            else if (result is RecognitionResult recoResult)
            {
                var alternates = recoResult.Results?.Select(r => new SpeechRecognitionAlternate(r.LexicalForm, r.Confidence)) ?? new[] { new SpeechRecognitionAlternate(string.Empty, 1.0) };

                if (!rankOrder)
                {
                    // order by confidence score
                    alternates = alternates.OrderByDescending(r => r.Confidence);
                }

                var topAlternate = alternates.First();
                return new StreamingSpeechRecognitionResult(
                    false,
                    topAlternate.Text,
                    topAlternate.Confidence,
                    alternates,
                    null,
                    result.Duration);
            }
            else
            {
                throw new InvalidOperationException("Unexpected recognition result type!");
            }
        }

        /// <summary>
        /// Composes two consecutive speech recognition results.
        /// </summary>
        /// <param name="result1">The first speech recognition result.</param>
        /// <param name="result2">The second speech recognition result.</param>
        /// <param name="rankOrder">A value indicating whether the alternates should be scored by original rank order or confidence.</param>
        /// <returns>The composed speech recognition result.</returns>
        internal static StreamingSpeechRecognitionResult Compose(this StreamingSpeechRecognitionResult result1, StreamingSpeechRecognitionResult result2, bool rankOrder = true)
        {
            // limit the number of alternates to prevent exponential growth through composition
            int maxAlternates = Math.Max(result1.Alternates.Length, result2.Alternates.Length);

            return new StreamingSpeechRecognitionResult(
                false,
                Compose(result1.Text, result2.Text),
                Combine(result1.Confidence, result2.Confidence),
                Compose(result1.Alternates, result2.Alternates, rankOrder).OrderByDescending(alt => alt.score).Take(maxAlternates).Select(alt => alt.value),
                null,
                result1.Duration + result2.Duration);
        }

        /// <summary>
        /// Finalizes an intermediate speech recognition result.
        /// </summary>
        /// <param name="result">The intermediate speech recognition result.</param>
        /// <param name="audio">The audio corresponding to the finalized speech recognition result.</param>
        /// <param name="duration">The duration of the utterance.</param>
        /// <returns>The final speech recognition result.</returns>
        internal static StreamingSpeechRecognitionResult ToFinal(this StreamingSpeechRecognitionResult result, AudioBuffer audio = default, TimeSpan duration = default)
        {
            return new StreamingSpeechRecognitionResult(
                true,
                result.Text,
                result.Confidence,
                result.Alternates.Cast<SpeechRecognitionAlternate>(),
                audio.HasValidData ? audio : result.Audio,
                duration != default ? duration : result.Duration);
        }

        /// <summary>
        /// Concatenates two strings.
        /// </summary>
        /// <param name="string1">The first string.</param>
        /// <param name="string2">The second string.</param>
        /// <returns>The concatenated string.</returns>
        private static string Compose(string string1, string string2)
        {
            string result = string1;

            if (!string.IsNullOrWhiteSpace(string2))
            {
                if (!string.IsNullOrWhiteSpace(string1))
                {
                    // concatenate non-empty strings with word-separator
                    result += ' ' + string2;
                }
                else
                {
                    // ignore empty str1 and take str2 as the result
                    result = string2;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the product of two scores if both are non-zero, otherwise returns the non-zero score.
        /// </summary>
        /// <param name="score1">The first score.</param>
        /// <param name="score2">The second score.</param>
        /// <returns>The combined score.</returns>
        private static double Combine(double score1, double score2)
        {
            double result = score1;

            if (score2 != 0)
            {
                if (score1 != 0)
                {
                    // take the product of the two scores
                    result *= score2;
                }
                else
                {
                    // ignore zero score1 and take score2 as the result
                    result = score2;
                }
            }

            return result;
        }

        /// <summary>
        /// Combines two nullable scores if both are not-null, otherwise returns the non-null score.
        /// </summary>
        /// <param name="score1">The first score.</param>
        /// <param name="score2">The second score.</param>
        /// <returns>The combined score.</returns>
        private static double? Combine(double? score1, double? score2)
        {
            if (score2.HasValue)
            {
                if (score1.HasValue)
                {
                    return Combine(score1.Value, score2.Value);
                }
                else
                {
                    // ignore empty conf1 and take conf2 as the result
                    return score2;
                }
            }
            else
            {
                // ignore empty conf2 and take conf1 as the result
                return score1;
            }
        }

        /// <summary>
        /// Composes two scored alternates.
        /// </summary>
        /// <param name="alternate1">The first alternate.</param>
        /// <param name="score1">The score of the first alternate.</param>
        /// <param name="alternate2">The second alternate.</param>
        /// <param name="score2">The score of the second alternate.</param>
        /// <returns>The composed alternate and combined score.</returns>
        private static (SpeechRecognitionAlternate, double) Compose(ISpeechRecognitionAlternate alternate1, double score1, ISpeechRecognitionAlternate alternate2, double score2)
        {
            return (new SpeechRecognitionAlternate(Compose(alternate1.Text, alternate2.Text), Combine(alternate1.Confidence, alternate2.Confidence)), Combine(score1, score2));
        }

        /// <summary>
        /// Composes two lists representing consecutive speech recognition phrase alternates and assigns
        /// a score to each composed alternate using either the confidence or rank order scoring method.
        /// </summary>
        /// <param name="alternates1">The first alternate.</param>
        /// <param name="alternates2">The second alternate.</param>
        /// <param name="rankOrder">A value indicating that the rank order should be used to score the alternates.</param>
        /// <returns>An enumeration of composed alternates and their combined scores.</returns>
        private static IEnumerable<(SpeechRecognitionAlternate value, double score)> Compose(IEnumerable<ISpeechRecognitionAlternate> alternates1, IEnumerable<ISpeechRecognitionAlternate> alternates2, bool rankOrder)
        {
            foreach ((ISpeechRecognitionAlternate alt1, double score1) in alternates1.Select((alt, index) => (alt, GetScore(alt, index, rankOrder))))
            {
                foreach ((ISpeechRecognitionAlternate alt2, double score2) in alternates2.Select((alt, index) => (alt, GetScore(alt, index, rankOrder))))
                {
                    yield return Compose(alt1, score1, alt2, score2);
                }
            }
        }

        /// <summary>
        /// Gets the score for an alternate using the specified scoring method.
        /// </summary>
        /// <param name="alternate">The alternate.</param>
        /// <param name="index">The index (rank) of the alternate.</param>
        /// <param name="useRank">A value indicating that the rank should be used to score the alternates.</param>
        /// <returns>The score of the alternate.</returns>
        private static double GetScore(ISpeechRecognitionAlternate alternate, int index, bool useRank)
        {
            return useRank ? (1.0 / (index + 1)) : (alternate.Confidence ?? 0);
        }
    }
}
