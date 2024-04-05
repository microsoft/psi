// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Psi;

    /// <summary>
    /// Implements helper methods for language generation.
    /// </summary>
    public static class Language
    {
        private static readonly Random Random = new ();

        private static List<(string QueryName, string[] QueryParameters, string Result)> llmQueryDebugInfo = new ();

        private static string[] numbers = new[]
        {
            "zero",
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "ten",
            "eleven",
            "twelve",
            "thirteen",
            "fourteen",
            "fifteen",
            "sixteen",
            "seventeen",
            "eighteen",
            "nineteen",
            "twenty",
        };

        /// <summary>
        /// Gets various ways of saying "Got it".
        /// </summary>
        public static string GotIt => ChooseRandom(
            "Ok.",
            "Sounds good.",
            "Got it.");

        /// <summary>
        /// Gets various ways of saying "Sounds good".
        /// </summary>
        public static string SoundsGood => ChooseRandom(
            "Sounds good.",
            "Sure.");

        /// <summary>
        /// Gets various ways of saying "Sure".
        /// </summary>
        public static string Sure => ChooseRandom(
            "Sure.");

        /// <summary>
        /// Gets various ways of saying a non-understanding happened.
        /// </summary>
        public static string Nonunderstanding => ChooseRandom(
            "I'm sorry I did not understand that.",
            "Sorry I did not understand that.");

        /// <summary>
        /// Gets a number as a text.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns>The text for that number.</returns>
        public static string GetNumber(int number)
            => number > numbers.Length - 1 ? "unknown" : numbers[number];

        /// <summary>
        /// Chooses randomly between several options.
        /// </summary>
        /// <param name="options">The set of options.</param>
        /// <returns>A random option.</returns>
        public static string ChooseRandom(params string[] options)
            => options[Random.Next(options.Length)];

        /// <summary>
        /// Capitalizes the first letter of a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The resulting, capitalized string.</returns>
        public static string Capitalize(this string input)
        {
            if (input == null)
            {
                return input;
            }

            input = input.Trim();
            if (input.Length > 0)
            {
                return input[0].ToString().ToUpper() + input.Substring(1);
            }
            else
            {
                return input;
            }
        }

        /// <summary>
        /// Determines if a string contains one of several values.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="values">The values to check.</param>
        /// <returns>True if the string contains one of the values, false otherwise.</returns>
        public static bool ContainsOneOf(this string input, params string[] values)
            => values.Length > 0 && values.Any(word => Regex.IsMatch(input, $@"\b{Regex.Escape(word)}\b"));

        /// <summary>
        /// Tries to get the largest word overlap between a set of strings and a reference string.
        /// </summary>
        /// <param name="strings">The set of strings.</param>
        /// <param name="reference">The reference string.</param>
        /// <param name="result">On return, contains the string with the largest word overlap.</param>
        /// <returns>True if a string with a non-zero overlap was found, false otherwise.</returns>
        public static bool TryGetLargestWordOverlap(this IEnumerable<string> strings, string reference, out string result)
        {
            var referenceTokens = reference.ToLower().Split(' ');
            var maxOverlap = 0;
            result = null;

            foreach (var s in strings)
            {
                var tokens = s.ToLower().Split(' ');
                var overlap = tokens.Intersect(referenceTokens).Count();
                if (overlap > maxOverlap)
                {
                    maxOverlap = overlap;
                    result = s;
                }
            }

            return result != null;
        }

        /// <summary>
        /// Tries to get the string after a specified text in a reference string.
        /// </summary>
        /// <param name="source">The reference string.</param>
        /// <param name="text">The text to search for.</param>
        /// <param name="afterText">On return, contains the string after the specified text.</param>
        /// <returns>True if the text was found, false otherwise.</returns>
        public static bool TryGetStringAfter(this string source, string text, out string afterText)
        {
            afterText = null;
            if (source.Contains(text))
            {
                afterText = source.Substring(source.IndexOf(text) + text.Length);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get the string after one of several specified texts in a reference string.
        /// </summary>
        /// <param name="source">The reference string.</param>
        /// <param name="texts">The texts to search for.</param>
        /// <param name="afterText">On return, contains the string after the specified text.</param>
        /// <returns>True if the text was found, false otherwise.</returns>
        public static bool TryGetStringAfterOneOf(this string source, string[] texts, out string afterText)
        {
            foreach (var text in texts)
            {
                if (source.TryGetStringAfter(text, out afterText))
                {
                    return true;
                }
            }

            afterText = null;
            return false;
        }

        /// <summary>
        /// Converts a string to a sentence case.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The sentence case string.</returns>
        public static string ToSentenceCase(this string input)
            => $"{char.ToUpper(input[0])}{input.Substring(1)}";
    }
}
