// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Filters
{
    /// <summary>
    /// Represents helper methods for working withfilters.
    /// </summary>
    public static partial class FilterExtensions
    {
        /// <summary>
        /// Extension to apply <see cref="OneEuroFilter"/> straight to a double stream.
        /// </summary>
        /// <param name="input">The input to operate on.</param>
        /// <param name="cutoffSlope">Cutoff slope (beta).</param>
        /// <param name="minCutoffFrequency">Minimum cutoff frequency.</param>
        /// <param name="derivateCutoffFrequency">Cutoff frequency for derivate.</param>
        /// <returns>The emitter for the filtered output.</returns>
        public static IProducer<double> OneEuroFilter(this IProducer<double> input, double cutoffSlope = 0, double minCutoffFrequency = 1.0, double derivateCutoffFrequency = 1.0)
        {
            var filter = new OneEuroFilter(input.Out.Pipeline, cutoffSlope, minCutoffFrequency, derivateCutoffFrequency);
            return input.PipeTo(filter);
        }
    }
}
