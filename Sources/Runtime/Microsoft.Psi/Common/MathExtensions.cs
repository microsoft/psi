// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods for certain math operations.
    /// </summary>
    public static class MathExtensions
    {
        /// <summary>
        /// Returns the minimum value in a sequence of double values.
        /// </summary>
        /// <param name="source">A sequence of double values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a minimum value of NaN.
        /// An empty sequence will throw an InvalidOperationException.
        /// </remarks>
        public static double Min(IEnumerable<double> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            double? min = null;
            foreach (double x in source)
            {
                // Math.Min will evaluate to NaN if either of its operands is NaN
                min = min.HasValue ? Math.Min(min.Value, x) : x;
            }

            return min ?? throw new InvalidOperationException("Sequence contains no elements");
        }

        /// <summary>
        /// Returns the minimum value in a sequence of nullable double values.
        /// </summary>
        /// <param name="source">A sequence of nullable double values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a minimum value of NaN.
        /// Null values in the sequence are ignored. An empty sequence will return null.
        /// </remarks>
        public static double? Min(IEnumerable<double?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            double? min = null;
            foreach (double? x in source)
            {
                if (x.HasValue)
                {
                    // Math.Min will evaluate to NaN if either of its operands is NaN
                    min = min.HasValue ? Math.Min(min.Value, x.Value) : x;
                }
            }

            return min;
        }

        /// <summary>
        /// Returns the minimum value in a sequence of float values.
        /// </summary>
        /// <param name="source">A sequence of float values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a minimum value of NaN.
        /// An empty sequence will throw an InvalidOperationException.
        /// </remarks>
        public static float Min(IEnumerable<float> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            float? min = null;
            foreach (float x in source)
            {
                // Math.Min will evaluate to NaN if either of its operands is NaN
                min = min.HasValue ? Math.Min(min.Value, x) : x;
            }

            return min ?? throw new InvalidOperationException("Sequence contains no elements");
        }

        /// <summary>
        /// Returns the minimum value in a sequence of nullable float values.
        /// </summary>
        /// <param name="source">A sequence of nullable float values to determine the minimum value of.</param>
        /// <returns>The minimum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a minimum value of NaN.
        /// Null values in the sequence are ignored. An empty sequence will return null.
        /// </remarks>
        public static float? Min(IEnumerable<float?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            float? min = null;
            foreach (float? x in source)
            {
                if (x.HasValue)
                {
                    // Math.Min will evaluate to NaN if either of its operands is NaN
                    min = min.HasValue ? Math.Min(min.Value, x.Value) : x;
                }
            }

            return min;
        }

        /// <summary>
        /// Returns the maximum value in a sequence of double values.
        /// </summary>
        /// <param name="source">A sequence of double values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a maximum value of NaN.
        /// An empty sequence will throw an InvalidOperationException.
        /// </remarks>
        public static double Max(IEnumerable<double> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            double? max = null;
            foreach (double x in source)
            {
                // Math.Max will evaluate to NaN if either of its operands is NaN
                max = max.HasValue ? Math.Max(max.Value, x) : x;
            }

            return max ?? throw new InvalidOperationException("Sequence contains no elements");
        }

        /// <summary>
        /// Returns the maximum value in a sequence of nullable double values.
        /// </summary>
        /// <param name="source">A sequence of nullable double values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a maximum value of NaN.
        /// Null values in the sequence are ignored. An empty sequence will return null.
        /// </remarks>
        public static double? Max(IEnumerable<double?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            double? max = null;
            foreach (double? x in source)
            {
                if (x.HasValue)
                {
                    // Math.Max will evaluate to NaN if either of its operands is NaN
                    max = max.HasValue ? Math.Max(max.Value, x.Value) : x;
                }
            }

            return max;
        }

        /// <summary>
        /// Returns the maximum value in a sequence of float values.
        /// </summary>
        /// <param name="source">A sequence of float values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a maximum value of NaN.
        /// An empty sequence will throw an InvalidOperationException.
        /// </remarks>
        public static float Max(IEnumerable<float> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            float? max = null;
            foreach (float x in source)
            {
                // Math.Max will evaluate to NaN if either of its operands is NaN
                max = max.HasValue ? Math.Max(max.Value, x) : x;
            }

            return max ?? throw new InvalidOperationException("Sequence contains no elements");
        }

        /// <summary>
        /// Returns the maximum value in a sequence of nullable float values.
        /// </summary>
        /// <param name="source">A sequence of nullable float values to determine the maximum value of.</param>
        /// <returns>The maximum value in the sequence.</returns>
        /// <remarks>
        /// A sequence containing one or more NaN values will return a maximum value of NaN.
        /// Null values in the sequence are ignored. An empty sequence will return null.
        /// </remarks>
        public static float? Max(IEnumerable<float?> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            float? max = null;
            foreach (float? x in source)
            {
                if (x.HasValue)
                {
                    // Math.Max will evaluate to NaN if either of its operands is NaN
                    max = max.HasValue ? Math.Max(max.Value, x.Value) : x;
                }
            }

            return max;
        }
    }
}
