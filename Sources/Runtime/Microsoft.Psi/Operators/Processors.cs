// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Executes a transform action for each item in the input stream. The action can output zero or more results by posting them to the emitter provided as an argument.
        /// </summary>
        /// <typeparam name="TIn">The input message type</typeparam>
        /// <typeparam name="TOut">The output message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="transform">The action to perform on every message in the source stream.
        /// The action parameters are the message, the envelope and an emitter to post results to</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of type <typeparamref name="TOut"/></returns>
        public static IProducer<TOut> Process<TIn, TOut>(this IProducer<TIn> source, Action<TIn, Envelope, Emitter<TOut>> transform, DeliveryPolicy policy = null)
        {
            var select = new Processor<TIn, TOut>(source.Out.Pipeline, transform);
            return PipeTo(source, select, policy ?? DeliveryPolicy.Immediate);
        }

        /// <summary>
        /// Executes a transform function for each item in the input stream, generating a new stream with the values returned by the function.
        /// The function has access to the envelope of the input message.
        /// </summary>
        /// <typeparam name="TIn">The input message type</typeparam>
        /// <typeparam name="TOut">The output message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="selector">The function to perform on every message in the source stream. The function takes two parameters, the input message and its envelope.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of type <typeparamref name="TOut"/></returns>
        public static IProducer<TOut> Select<TIn, TOut>(this IProducer<TIn> source, Func<TIn, Envelope, TOut> selector, DeliveryPolicy policy = null)
        {
            var p = Process<TIn, TOut>(source, (d, e, s) => s.Post(selector(d, e), e.OriginatingTime), policy);
            return p;
        }

        /// <summary>
        /// Executes a transform function for each item in the input stream, generating a new stream with the values returned by the function.
        /// </summary>
        /// <typeparam name="TIn">The input message type</typeparam>
        /// <typeparam name="TOut">The output message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="selector">The function to perform on every message in the source stream.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of type <typeparamref name="TOut"/></returns>
        public static IProducer<TOut> Select<TIn, TOut>(this IProducer<TIn> source, Func<TIn, TOut> selector, DeliveryPolicy policy = null)
        {
            return Select(source, (d, e) => selector(d), policy);
        }

        /// <summary>
        /// Executes a transform function for each non-null item in the input stream, generating a new stream with the values returned by the function, or null if the input was null.
        /// The function has access to the envelope of the input message.
        /// </summary>
        /// <typeparam name="TIn">The input message type</typeparam>
        /// <typeparam name="TOut">The output message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="selector">The function to perform on every message in the source stream. The function takes two parameters, the input message and its envelope.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of type <typeparamref name="TOut"/></returns>
        public static IProducer<TOut?> NullableSelect<TIn, TOut>(this IProducer<TIn?> source, Func<TIn, Envelope, TOut> selector, DeliveryPolicy policy = null)
            where TIn : struct
            where TOut : struct
        {
            return source.Select((v, e) => v.HasValue ? new TOut?(selector(v.Value, e)) : null, policy);
        }

        /// <summary>
        /// Executes a transform function for each non-null item in the input stream, generating a new stream with the values returned by the function, or null if the input was null.
        /// </summary>
        /// <typeparam name="TIn">The input message type</typeparam>
        /// <typeparam name="TOut">The output message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="selector">The function to perform on every message in the source stream.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of type <typeparamref name="TOut"/></returns>
        public static IProducer<TOut?> NullableSelect<TIn, TOut>(this IProducer<TIn?> source, Func<TIn, TOut> selector, DeliveryPolicy policy = null)
            where TIn : struct
            where TOut : struct
        {
            return source.Select(v => v.HasValue ? new TOut?(selector(v.Value)) : null, policy);
        }

        /// <summary>
        /// Decomposes a stream of tuples into a stream containing just the first item of each tuple.
        /// </summary>
        /// <typeparam name="T1">The type of the first item in the tuple</typeparam>
        /// <typeparam name="T2">The type of the second item in the tuple</typeparam>
        /// <param name="source">The source stream of tuples</param>
        /// <returns>A stream containing the first item of each tuple</returns>
        public static IProducer<T1> Item1<T1, T2>(this IProducer<ValueTuple<T1, T2>> source)
        {
            return source.Select(t => t.Item1);
        }

        /// <summary>
        /// Decomposes a stream of tuples into a stream containing just the second item of each tuple.
        /// </summary>
        /// <typeparam name="T1">The type of the first item in the tuple</typeparam>
        /// <typeparam name="T2">The type of the second item in the tuple</typeparam>
        /// <param name="source">The source stream of tuples</param>
        /// <returns>A stream containing the second item of each tuple</returns>
        public static IProducer<T2> Item2<T1, T2>(this IProducer<ValueTuple<T1, T2>> source)
        {
            return source.Select(t => t.Item2);
        }

        /// <summary>
        /// Executes a function for each item in the input stream, generating a new stream with the (zero or more) values returned by the function.
        /// The function must return an <see cref="IEnumerable{TOut}"/>, which can be <see cref="System.Linq.Enumerable.Empty{TOut}"/> to indicate zero results.
        /// The values in the returned <see cref="IEnumerable{TOut}"/> are emitted as separate messages with the same oringinating time.
        /// </summary>
        /// <typeparam name="TIn">The input message type</typeparam>
        /// <typeparam name="TOut">The output message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="selector">The function to perform on every message in the source stream. The function has access to the envelope of the input message.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of type <typeparamref name="TOut"/></returns>
        public static IProducer<TOut> SelectMany<TIn, TOut>(this IProducer<TIn> source, Func<TIn, Envelope, IEnumerable<TOut>> selector, DeliveryPolicy policy = null)
        {
            var p = Process<TIn, TOut>(
                source,
                (d, e, s) =>
                {
                    foreach (var item in selector(d, e))
                    {
                        s.Post(item, e.OriginatingTime);
                    }
                },
                policy);

            return p;
        }

        /// <summary>
        /// Executes a function for each item in the input stream, generating a new stream with the (zero or more) values returned by the function.
        /// The function must return an <see cref="IEnumerable{TOut}"/>, which can be <see cref="System.Linq.Enumerable.Empty{TOut}"/> to indicate zero results.
        /// The values in the returned <see cref="IEnumerable{TOut}"/> are emitted as separate messages with the same oringinating time.
        /// </summary>
        /// <typeparam name="TIn">The input message type</typeparam>
        /// <typeparam name="TOut">The output message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="selector">The function to perform on every message in the source stream.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of type <typeparamref name="TOut"/></returns>
        public static IProducer<TOut> SelectMany<TIn, TOut>(this IProducer<TIn> source, Func<TIn, IEnumerable<TOut>> selector, DeliveryPolicy policy = null)
        {
            return SelectMany(source, (d, e) => selector(d), policy);
        }

        /// <summary>
        /// Flip takes a tuple of 2 elements and flips their order
        /// </summary>
        /// <typeparam name="T1">Type of first element</typeparam>
        /// <typeparam name="T2">Type of second element</typeparam>
        /// <param name="source">Source to read tuples from</param>
        /// <param name="policy">Delivery policy</param>
        /// <returns>Returns a new producer with flipped tuples</returns>
        public static IProducer<(T2, T1)> Flip<T1, T2>(this IProducer<(T1, T2)> source, DeliveryPolicy policy = null)
        {
            var p = Process<(T1, T2), (T2, T1)>(
                source,
                (d, e, s) =>
                {
                    s.Post((d.Item2, d.Item1), e.OriginatingTime);
                },
                policy);
            return p;
        }

        /// <summary>
        /// Executes an action for each item in the input stream and then outputs the item. If the action modifies the item, the resulting stream reflects the change.
        /// </summary>
        /// <typeparam name="T">The input message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="action">The action to perform on every message in the source stream. The action has access to the message envelope.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of the same type as the source stream, containing one item for each input item, possibly modified by the action delegate.</returns>
        public static IProducer<T> Do<T>(this IProducer<T> source, Action<T, Envelope> action, DeliveryPolicy policy = null)
        {
            var p = Process<T, T>(
                source,
                (d, e, s) =>
                {
                    action(d, e);
                    s.Post(d, e.OriginatingTime);
                },
                policy);

            return p;
        }

        /// <summary>
        /// Executes an action for each item in the input stream and then outputs the item. If the action modifies the item, the resulting stream reflects the change.
        /// </summary>
        /// <typeparam name="T">The input message type</typeparam>
        /// <param name="source">The source stream to subscribe to</param>
        /// <param name="action">The action to perform on every message in the source stream. The action has access to the message envelope.</param>
        /// <param name="policy">An optional delivery policy</param>
        /// <returns>A stream of the same type as the source stream, containing one item for each input item, possibly modified by the action delegate.</returns>
        public static IProducer<T> Do<T>(this IProducer<T> source, Action<T> action, DeliveryPolicy policy = null)
        {
            return Do(source, (d, e) => action(d), policy);
        }
    }
}