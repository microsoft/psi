// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Sample stream by interval with an interpolator.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="samplingInterval">Interval at which to sample.</param>
        /// <param name="interpolator">Interpolator with which to sample.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Sample<T>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            Match.Interpolator<T> interpolator,
            DeliveryPolicy policy = null)
        {
            policy = policy ?? DeliveryPolicy.Immediate;
            var sampler = new Sampler<T>(source.Out.Pipeline, interpolator, samplingInterval);
            source.PipeTo(sampler);
            return sampler;
        }

        /// <summary>
        /// Sample stream by interval with a time window.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="samplingInterval">Interval at which to sample.</param>
        /// <param name="matchTolerance">Match tolerance with which to sample.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Sample<T>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            TimeSpan matchTolerance = default(TimeSpan),
            DeliveryPolicy policy = null)
        {
            return Sample(source, samplingInterval, new RelativeTimeInterval(-matchTolerance, matchTolerance), policy);
        }

        /// <summary>
        /// Sample stream by interval with a relative time interval window.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="samplingInterval">Interval at which to sample.</param>
        /// <param name="matchWindow">Relative time interval window in which to sample.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Sample<T>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            RelativeTimeInterval matchWindow,
            DeliveryPolicy policy = null)
        {
            return Sample(source, samplingInterval, Match.Best<T>(matchWindow), policy);
        }

        /// <summary>
        /// Sample stream by clock signal with a time window.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <typeparam name="TClock">Type of clock signal messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="clock">Clock signal stream.</param>
        /// <param name="tolerance">Time span tolerance in which to sample.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Sample<T, TClock>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            TimeSpan tolerance,
            DeliveryPolicy policy = null)
        {
            return Sample(source, clock, new RelativeTimeInterval(-tolerance, tolerance), policy);
        }

        /// <summary>
        /// Sample stream by clock signal with a relative time interval window.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <typeparam name="TClock">Type of clock signal messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="clock">Clock signal stream.</param>
        /// <param name="matchWindow">Relative time interval window in which to sample.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Sample<T, TClock>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            RelativeTimeInterval matchWindow,
            DeliveryPolicy policy = null)
        {
            return Sample(source, clock, Match.Best<T>(matchWindow), policy);
        }

        /// <summary>
        /// Sample stream by clock signal with an interpolator.
        /// </summary>
        /// <typeparam name="T">Type of source/output messages.</typeparam>
        /// <typeparam name="TClock">Type of clock signal messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="clock">Clock signal stream.</param>
        /// <param name="interpolator">Interpolator with which to sample.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Sample<T, TClock>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            Match.Interpolator<T> interpolator,
            DeliveryPolicy policy = null)
        {
            policy = policy ?? DeliveryPolicy.Immediate;
            var join = new Join<TClock, T, T>(source.Out.Pipeline, interpolator, (clk, data) => data[0]);
            clock.PipeTo(join.InPrimary, policy);
            source.PipeTo(join.InSecondaries[0], policy);
            return join;
        }
    }
}