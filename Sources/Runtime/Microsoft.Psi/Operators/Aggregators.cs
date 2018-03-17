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
        public static IProducer<TOut> Aggregate<TIn, TOut>(this IProducer<TIn> source, TOut seed, Func<TOut, TIn, TOut> func, DeliveryPolicy policy = null)
        {
            return Aggregate<TOut, TIn, TOut>(
                source.Out,
                seed,
                (a, d, e, s) =>
                {
                    var newState = func(a, d);
                    s.Post(newState, e.OriginatingTime);
                    return newState;
                },
                policy);
        }

        public static IProducer<TOut> Aggregate<TIn, TAcc, TOut>(this IProducer<TIn> source, TAcc seed, Func<TAcc, TIn, TAcc> func, Func<TAcc, TOut> selector)
        {
            return Aggregate(source, seed, func).Select(selector);
        }

        public static IProducer<T> Aggregate<T>(this IProducer<T> source, Func<T, T, T> func, DeliveryPolicy policy = null)
        {
            // `Aggregate` where `TIn` is same type as `TOut`, seed becomes first value
            return Aggregate(
                source,
                Tuple.Create(true, default(T)),
                (s, x) =>
                {
                    var first = s.Item1;
                    var val = s.Item2;
                    return Tuple.Create(false, first ? x : func(val, x));
                },
                policy).Select(x => x.Item2);
        }

        public static IProducer<TOut> Aggregate<TAccumulate, TIn, TOut>(
            this IProducer<TIn> source,
            TAccumulate seed,
            Func<TAccumulate, TIn, Envelope, Emitter<TOut>, TAccumulate> func,
            DeliveryPolicy policy = null)
        {
            var aggregate = new Aggregator<TAccumulate, TIn, TOut>(source.Out.Pipeline, seed, func);
            return PipeTo(source, aggregate, policy ?? DeliveryPolicy.Immediate);
        }
    }
}