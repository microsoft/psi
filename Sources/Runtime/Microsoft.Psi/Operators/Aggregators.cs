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
        /// Aggregate stream values.
        /// </summary>
        /// <typeparam name="TIn">Type of source stream.</typeparam>
        /// <typeparam name="TOut">Type of output stream.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="seed">Initial seed state.</param>
        /// <param name="func">Aggregation function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Aggregate<TIn, TOut>(this IProducer<TIn> source, TOut seed, Func<TOut, TIn, TOut> func, DeliveryPolicy<TIn> deliveryPolicy = null)
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
                deliveryPolicy);
        }

        /// <summary>
        /// Aggregate stream values.
        /// </summary>
        /// <typeparam name="TIn">Type of source stream messages.</typeparam>
        /// <typeparam name="TAcc">Type of initial seed value.</typeparam>
        /// <typeparam name="TOut">Type of output stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="seed">Initial seed state.</param>
        /// <param name="func">Aggregation function.</param>
        /// <param name="selector">Selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Aggregate<TIn, TAcc, TOut>(this IProducer<TIn> source, TAcc seed, Func<TAcc, TIn, TAcc> func, Func<TAcc, TOut> selector, DeliveryPolicy<TIn> deliveryPolicy = null)
        {
            return Aggregate(source, seed, func, deliveryPolicy).Select(selector, DeliveryPolicy.SynchronousOrThrottle);
        }

        /// <summary>
        /// Aggregate stream values.
        /// </summary>
        /// <typeparam name="T">Type of source/output stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="func">Aggregation function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<T> Aggregate<T>(this IProducer<T> source, Func<T, T, T> func, DeliveryPolicy<T> deliveryPolicy = null)
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
                deliveryPolicy).Select(x => x.Item2, DeliveryPolicy.SynchronousOrThrottle);
        }

        /// <summary>
        /// Aggregate stream values.
        /// </summary>
        /// <typeparam name="TAccumulate">Type of initial seed value.</typeparam>
        /// <typeparam name="TIn">Type of input stream messages.</typeparam>
        /// <typeparam name="TOut">Type of output stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="seed">Initial seed value.</param>
        /// <param name="func">Aggregation function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Aggregate<TAccumulate, TIn, TOut>(
            this IProducer<TIn> source,
            TAccumulate seed,
            Func<TAccumulate, TIn, Envelope, Emitter<TOut>, TAccumulate> func,
            DeliveryPolicy<TIn> deliveryPolicy = null)
        {
            var aggregate = new Aggregator<TAccumulate, TIn, TOut>(source.Out.Pipeline, seed, func);
            return PipeTo(source, aggregate, deliveryPolicy);
        }
    }
}