// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Filters
{
    using System;
    using MathNet.Filtering;

    /// <summary>
    /// Operators for computing various MathNet online filters over streams of double.
    /// </summary>
    public static partial class MathNetFilters
    {
        /// <summary>
        /// Samples the input stream and applies a low-pass filter (using MathNet.Filtering) over the sampled input stream. Removes high frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffRate">The desired cutoff for the filter.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A low-pass filtered version of the input.</returns>
        public static IProducer<double> LowpassFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffRate,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateLowpass(impulseResponse, sampleRate, cutoffRate);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Samples the input stream and applies a low-pass filter (using MathNet.Filtering) over the sampled input stream. Removes high frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffRate">The desired cutoff for the filter.</param>
        /// <param name="order">Filter order.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A low-pass filtered version of the input.</returns>
        public static IProducer<double> LowpassFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffRate,
            int order,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateLowpass(impulseResponse, sampleRate, cutoffRate, order);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Samples the input stream and applies a high-pass filter (using MathNet.Filtering) over the sampled input stream. Removes low frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffRate">The desired cutoff for the filter.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A high-pass filtered version of the input.</returns>
        public static IProducer<double> HighpassFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffRate,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateHighpass(impulseResponse, sampleRate, cutoffRate);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Samples the input stream and applies a high-pass filter (using MathNet.Filtering) over the sampled input stream. Removes low frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffRate">The desired cutoff for the filter.</param>
        /// <param name="order">Filter order.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A high-pass filtered version of the input.</returns>
        public static IProducer<double> HighpassFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffRate,
            int order,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateHighpass(impulseResponse, sampleRate, cutoffRate, order);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Samples the input stream and applies a band-pass filter (using MathNet.Filtering) over the sampled input stream. Removes low and high frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffLowRate">The desired low frequency cutoff for the filter.</param>
        /// <param name="cutoffHighRate">The desired high frequency cutoff for the filter.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A band-pass filtered version of the input.</returns>
        public static IProducer<double> BandpassFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffLowRate,
            double cutoffHighRate,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateBandpass(impulseResponse, sampleRate, cutoffLowRate, cutoffHighRate);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Samples the input stream and applies a band-pass filter (using MathNet.Filtering) over the sampled input stream. Removes low and high frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffLowRate">The desired low frequency cutoff for the filter.</param>
        /// <param name="cutoffHighRate">The desired high frequency cutoff for the filter.</param>
        /// <param name="order">Filter order.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A band-pass filtered version of the input.</returns>
        public static IProducer<double> BandpassFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffLowRate,
            double cutoffHighRate,
            int order,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateBandpass(impulseResponse, sampleRate, cutoffLowRate, cutoffHighRate, order);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Samples the input stream and applies a band-stop filter (using MathNet.Filtering) over the sampled input stream. Removes middle frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffLowRate">The desired low frequency cutoff for the filter.</param>
        /// <param name="cutoffHighRate">The desired high frequency cutoff for the filter.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A band-stop filtered version of the input.</returns>
        public static IProducer<double> BandstopFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffLowRate,
            double cutoffHighRate,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateBandstop(impulseResponse, sampleRate, cutoffLowRate, cutoffHighRate);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Samples the input stream and applies a band-stop filter (using MathNet.Filtering) over the sampled input stream. Removes middle frequencies.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="sampleRate">The desired sample rate for the filter.</param>
        /// <param name="cutoffLowRate">The desired low frequency cutoff for the filter.</param>
        /// <param name="cutoffHighRate">The desired high frequency cutoff for the filter.</param>
        /// <param name="order">Filter order.</param>
        /// <param name="impulseResponse">Specifies how the filter will respond to an impulse input (Finite or Infinite). Default is Finite.</param>
        /// <param name="sampleInterpolator">Optional sampling interpolator over the input. Default is a reproducible linear interpolation.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampled messages with, and the sampled messages
        /// will have originating times that align with (i.e., are an integral number of intervals away from) the specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A band-stop filtered version of the input.</returns>
        public static IProducer<double> BandstopFilter(
            this IProducer<double> input,
            double sampleRate,
            double cutoffLowRate,
            double cutoffHighRate,
            int order,
            ImpulseResponse impulseResponse = ImpulseResponse.Finite,
            Interpolator<double, double> sampleInterpolator = null,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateBandstop(impulseResponse, sampleRate, cutoffLowRate, cutoffHighRate, order);
            return MathNetFilter(input, sampleRate, sampleInterpolator ?? Reproducible.Linear(), alignmentDateTime, filter, deliveryPolicy);
        }

        /// <summary>
        /// Applies a de-noising filter (using MathNet.Filtering) over the input stream. Implemented as an unweighted median filter.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A high-pass filtered version of the input.</returns>
        public static IProducer<double> DenoiseFilter(this IProducer<double> input, DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateDenoise();
            return input.Select(s => filter.ProcessSample(s), deliveryPolicy);
        }

        /// <summary>
        /// Applies a de-noising filter (using MathNet.Filtering) over the input stream. Implemented as an unweighted median filter.
        /// </summary>
        /// <param name="input">The input source of doubles.</param>
        /// <param name="order">Window Size, should be odd. A larger number results in a smoother response but also in a longer delay.</param>
        /// <param name="deliveryPolicy">An optional delivery policy for the input stream.</param>
        /// <returns>A high-pass filtered version of the input.</returns>
        public static IProducer<double> DenoiseFilter(this IProducer<double> input, int order, DeliveryPolicy<double> deliveryPolicy = null)
        {
            var filter = OnlineFilter.CreateDenoise(order);
            return input.Select(s => filter.ProcessSample(s), deliveryPolicy);
        }

        private static IProducer<double> MathNetFilter(
            IProducer<double> input,
            double sampleRate,
            Interpolator<double, double> sampleInterpolator,
            DateTime? alignmentDateTime,
            IOnlineFilter filter,
            DeliveryPolicy<double> deliveryPolicy)
        {
            // Sample the input stream at the desired rate and process through the given filter.
            var clock = Generators.Repeat(input.Out.Pipeline, 0, TimeSpan.FromSeconds(1.0 / sampleRate), alignmentDateTime);
            return input
                .Interpolate(clock, sampleInterpolator, sourceDeliveryPolicy: deliveryPolicy, clockDeliveryPolicy: DeliveryPolicy.Unlimited)
                .Select(s => filter.ProcessSample(s), DeliveryPolicy.SynchronousOrThrottle);
        }
    }
}
