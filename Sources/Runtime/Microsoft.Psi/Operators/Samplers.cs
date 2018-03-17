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

        public static IProducer<T> Sample<T>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            TimeSpan matchTolerance = default(TimeSpan),
            DeliveryPolicy policy = null)
        {
            return Sample(source, samplingInterval, new RelativeTimeInterval(-matchTolerance, matchTolerance), policy);
        }

        public static IProducer<T> Sample<T>(
            this IProducer<T> source,
            TimeSpan samplingInterval,
            RelativeTimeInterval matchWindow,
            DeliveryPolicy policy = null)
        {
            return Sample(source, samplingInterval, Match.Best<T>(matchWindow), policy);
        }

        public static IProducer<T> Sample<T, TClock>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            TimeSpan tolerance,
            DeliveryPolicy policy = null)
        {
            return Sample(source, clock, new RelativeTimeInterval(-tolerance, tolerance), policy);
        }

        public static IProducer<T> Sample<T, TClock>(
            this IProducer<T> source,
            IProducer<TClock> clock,
            RelativeTimeInterval matchWindow,
            DeliveryPolicy policy = null)
        {
            return Sample(source, clock, Match.Best<T>(matchWindow), policy);
        }

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