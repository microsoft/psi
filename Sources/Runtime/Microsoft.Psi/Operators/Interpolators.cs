// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Interpolate a stream using a specified interpolator at a given sampling interval.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="samplingInterval">Interval at which to apply the interpolator.</param>
        /// <param name="interpolator">Interpolator to use for generating results.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampling messages with. If the paramater
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TInterpolation> Interpolate<T, TInterpolation>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            Interpolator<T, TInterpolation> interpolator,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy deliveryPolicy = null)
        {
            var clock = Generators.Repeat(source.Out.Pipeline, 0, samplingInterval, alignmentDateTime);
            return source.Interpolate(clock, interpolator, deliveryPolicy);
        }

        /// <summary>
        /// Interpolate a stream using a specified interpolator at interpolation points
        /// given by a clock stream.
        /// </summary>
        /// <typeparam name="T">Type of source messages.</typeparam>
        /// <typeparam name="TClock">Type of messages on the clock stream.</typeparam>
        /// <typeparam name="TInterpolation">Type of the interpolation result.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="clock">Clock stream that dictates the interpolation points.</param>
        /// <param name="interpolator">Interpolator to use for generating results.</param>
        /// <param name="sourceDeliveryPolicy">An optional delivery policy for the source stream.</param>
        /// <param name="clockDeliveryPolicy">An optional delivery policy for the clock stream.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TInterpolation> Interpolate<T, TClock, TInterpolation>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            Interpolator<T, TInterpolation> interpolator,
            DeliveryPolicy sourceDeliveryPolicy = null,
            DeliveryPolicy clockDeliveryPolicy = null)
        {
            var fuse = new Fuse<TClock, T, TInterpolation, TInterpolation>(source.Out.Pipeline, interpolator, (clk, data) => data[0]);
            clock.PipeTo(fuse.InPrimary, clockDeliveryPolicy);
            source.PipeTo(fuse.InSecondaries[0], sourceDeliveryPolicy);
            return fuse;
        }

        /// <summary>
        /// Sample a stream at a given sampling interval, by selecting the nearest message
        /// within a given tolerance to the interpolation point.
        /// </summary>
        /// <typeparam name="T">Type of source (and output) messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="samplingInterval">Interval at which to apply the interpolator.</param>
        /// <param name="tolerance">The tolerance within which to search for the nearest message.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampling messages with. If the paramater
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Sampled stream.</returns>
        public static IProducer<T> Sample<T>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            TimeSpan tolerance,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy deliveryPolicy = null)
        {
            return source.Interpolate(
                samplingInterval,
                Reproducible.Nearest<T>(new RelativeTimeInterval(-tolerance, tolerance)),
                alignmentDateTime,
                deliveryPolicy);
        }

        /// <summary>
        /// Sample a stream at a given sampling interval, by selecting the nearest message
        /// within a relative time interval to the interpolation point.
        /// </summary>
        /// <typeparam name="T">Type of source (and output) messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="samplingInterval">Interval at which to apply the interpolator.</param>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the nearest message.</param>
        /// <param name="alignmentDateTime">If non-null, this parameter specifies a time to align the sampling messages with. If the paramater
        /// is non-null, the messages will have originating times that align with (i.e., are an integral number of intervals away from) the
        /// specified alignment time.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Sampled stream.</returns>
        public static IProducer<T> Sample<T>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            RelativeTimeInterval relativeTimeInterval,
            DateTime? alignmentDateTime = null,
            DeliveryPolicy deliveryPolicy = null)
        {
            return source.Interpolate(
                samplingInterval,
                Reproducible.Nearest<T>(relativeTimeInterval),
                alignmentDateTime,
                deliveryPolicy);
        }

        /// <summary>
        /// Sample a stream at interpolation points given by a clock stream, by selecting the nearest
        /// message within a given tolerance to the interpolation point.
        /// </summary>
        /// <typeparam name="T">Type of source and output messages.</typeparam>
        /// <typeparam name="TClock">Type of messages on the clock stream.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="clock">Clock stream that dictates the interpolation points.</param>
        /// <param name="tolerance">The tolerance within which to search for the nearest message.</param>
        /// <param name="sourceDeliveryPolicy">An optional delivery policy for the source stream.</param>
        /// <param name="clockDeliveryPolicy">An optional delivery policy for the clock stream.</param>
        /// <returns>Sampled stream.</returns>
        public static IProducer<T> Sample<T, TClock>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            TimeSpan tolerance,
            DeliveryPolicy sourceDeliveryPolicy = null,
            DeliveryPolicy clockDeliveryPolicy = null)
        {
            return source.Interpolate(clock, Reproducible.Nearest<T>(new RelativeTimeInterval(-tolerance, tolerance)), sourceDeliveryPolicy, clockDeliveryPolicy);
        }

        /// <summary>
        /// Samples a stream at interpolation points given by a clock stream, by selecting the nearest
        /// message within a relative time interval to the interpolation point.
        /// </summary>
        /// <typeparam name="T">Type of source and output messages.</typeparam>
        /// <typeparam name="TClock">Type of messages on the clock stream.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="clock">Clock stream that dictates the interpolation points.</param>
        /// <param name="relativeTimeInterval">The relative time interval within which to search for the nearest message.</param>
        /// <param name="sourceDeliveryPolicy">An optional delivery policy for the source stream.</param>
        /// <param name="clockDeliveryPolicy">An optional delivery policy for the clock stream.</param>
        /// <returns>Sampled stream.</returns>
        public static IProducer<T> Sample<T, TClock>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            RelativeTimeInterval relativeTimeInterval,
            DeliveryPolicy sourceDeliveryPolicy = null,
            DeliveryPolicy clockDeliveryPolicy = null)
        {
            return source.Interpolate(clock, Reproducible.Nearest<T>(relativeTimeInterval), sourceDeliveryPolicy, clockDeliveryPolicy);
        }
    }
}