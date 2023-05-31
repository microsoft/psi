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
        /// <param name="initialState">The initial state.</param>
        /// <param name="aggregator">The aggregator function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Aggregate<TIn, TOut>(
            this IProducer<TIn> source,
            TOut initialState,
            Func<TOut, TIn, TOut> aggregator,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            string name = nameof(Aggregate))
            => source.Aggregate<TOut, TIn, TOut>(
                initialState,
                (state, input, envelope, emitter) =>
                {
                    var newState = aggregator(state, input);
                    emitter.Post(newState, envelope.OriginatingTime);
                    return newState;
                },
                deliveryPolicy,
                name);

        /// <summary>
        /// Aggregate stream values.
        /// </summary>
        /// <typeparam name="TIn">Type of source stream messages.</typeparam>
        /// <typeparam name="TAccumulate">Type of accumulator value.</typeparam>
        /// <typeparam name="TOut">Type of output stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="initialState">The initial state of the accumulator.</param>
        /// <param name="aggregator">The aggregation function.</param>
        /// <param name="selector">A selector function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Aggregate<TIn, TAccumulate, TOut>(
            this IProducer<TIn> source,
            TAccumulate initialState,
            Func<TAccumulate, TIn, TAccumulate> aggregator,
            Func<TAccumulate, TOut> selector,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            string name = nameof(Aggregate))
            => source.Aggregate<TAccumulate, TIn, TOut>(
                initialState,
                (state, input, envelope, emitter) =>
                {
                    var newState = aggregator(state, input);
                    emitter.Post(selector(newState), envelope.OriginatingTime);
                    return newState;
                },
                deliveryPolicy,
                name);

        /// <summary>
        /// Aggregate stream values.
        /// </summary>
        /// <typeparam name="T">Type of source/output stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="aggregator">The aggregator function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Output stream.</returns>
        /// <remarks>The initial state of the aggregation is the first value passed in.</remarks>
        public static IProducer<T> Aggregate<T>(
            this IProducer<T> source,
            Func<T, T, T> aggregator,
            DeliveryPolicy<T> deliveryPolicy = null,
            string name = nameof(Aggregate))
        {
            var first = true;
            return source.Aggregate<T, T, T>(
                default,
                (state, input, envelope, emitter) =>
                {
                    if (first)
                    {
                        state = input;
                        first = false;
                    }
                    else
                    {
                        state = aggregator(state, input);
                    }

                    emitter.Post(state, envelope.OriginatingTime);
                    return state;
                },
                deliveryPolicy,
                name);
        }

        /// <summary>
        /// Aggregate stream values.
        /// </summary>
        /// <typeparam name="TAccumulate">Type of accumulator value.</typeparam>
        /// <typeparam name="TIn">Type of input stream messages.</typeparam>
        /// <typeparam name="TOut">Type of output stream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="initialState">The initial value for the accumulator.</param>
        /// <param name="aggregator">The aggregation function.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Output stream.</returns>
        public static IProducer<TOut> Aggregate<TAccumulate, TIn, TOut>(
            this IProducer<TIn> source,
            TAccumulate initialState,
            Func<TAccumulate, TIn, Envelope, Emitter<TOut>, TAccumulate> aggregator,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            string name = nameof(Aggregate))
        {
            var aggregate = new Aggregator<TAccumulate, TIn, TOut>(source.Out.Pipeline, initialState, aggregator, name);
            return PipeTo(source, aggregate, deliveryPolicy);
        }
    }
}