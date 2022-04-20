// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Executes a transform action for each item in the input stream. The action can output zero or more results by posting them to the emitter provided as an argument.
        /// </summary>
        /// <typeparam name="TIn">The input message type.</typeparam>
        /// <typeparam name="TOut">The output message type.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="transform">The action to perform on every message in the source stream.
        /// The action parameters are the message, the envelope and an emitter to post results to.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of type <typeparamref name="TOut"/>.</returns>
        public static IProducer<TOut> Process<TIn, TOut>(this IProducer<TIn> source, Action<TIn, Envelope, Emitter<TOut>> transform, DeliveryPolicy<TIn> deliveryPolicy = null, string name = nameof(Process))
        {
            var select = new Processor<TIn, TOut>(source.Out.Pipeline, transform, name: name);
            return PipeTo(source, select, deliveryPolicy);
        }

        /// <summary>
        /// Executes a transform function for each item in the input stream, generating a new stream with the values returned by the function.
        /// The function has access to the envelope of the input message.
        /// </summary>
        /// <typeparam name="TIn">The input message type.</typeparam>
        /// <typeparam name="TOut">The output message type.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="selector">The function to perform on every message in the source stream. The function takes two parameters, the input message and its envelope.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of type <typeparamref name="TOut"/>.</returns>
        public static IProducer<TOut> Select<TIn, TOut>(this IProducer<TIn> source, Func<TIn, Envelope, TOut> selector, DeliveryPolicy<TIn> deliveryPolicy = null, string name = nameof(Select))
            => Process<TIn, TOut>(source, (d, e, s) => s.Post(selector(d, e), e.OriginatingTime), deliveryPolicy, name);

        /// <summary>
        /// Executes a transform function for each item in the input stream, generating a new stream with the values returned by the function.
        /// </summary>
        /// <typeparam name="TIn">The input message type.</typeparam>
        /// <typeparam name="TOut">The output message type.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="selector">The function to perform on every message in the source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of type <typeparamref name="TOut"/>.</returns>
        public static IProducer<TOut> Select<TIn, TOut>(this IProducer<TIn> source, Func<TIn, TOut> selector, DeliveryPolicy<TIn> deliveryPolicy = null, string name = nameof(Select))
            => Select(source, (d, e) => selector(d), deliveryPolicy, name);

        /// <summary>
        /// Executes a transform function for each non-null item in the input stream, generating a new stream with the values returned by the function, or null if the input was null.
        /// The function has access to the envelope of the input message.
        /// </summary>
        /// <typeparam name="TIn">The input message type.</typeparam>
        /// <typeparam name="TOut">The output message type.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="selector">The function to perform on every message in the source stream. The function takes two parameters, the input message and its envelope.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of type <typeparamref name="TOut"/>.</returns>
        public static IProducer<TOut?> NullableSelect<TIn, TOut>(this IProducer<TIn?> source, Func<TIn, Envelope, TOut> selector, DeliveryPolicy<TIn?> deliveryPolicy = null, string name = nameof(NullableSelect))
            where TIn : struct
            where TOut : struct
            => source.Select((v, e) => v.HasValue ? new TOut?(selector(v.Value, e)) : null, deliveryPolicy, name);

        /// <summary>
        /// Executes a transform function for each non-null item in the input stream, generating a new stream with the values returned by the function, or null if the input was null.
        /// </summary>
        /// <typeparam name="TIn">The input message type.</typeparam>
        /// <typeparam name="TOut">The output message type.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="selector">The function to perform on every message in the source stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of type <typeparamref name="TOut"/>.</returns>
        public static IProducer<TOut?> NullableSelect<TIn, TOut>(this IProducer<TIn?> source, Func<TIn, TOut> selector, DeliveryPolicy<TIn?> deliveryPolicy = null, string name = nameof(NullableSelect))
            where TIn : struct
            where TOut : struct
            => source.Select(v => v.HasValue ? new TOut?(selector(v.Value)) : null, deliveryPolicy, name);

        /// <summary>
        /// Decomposes a stream of tuples into a stream containing just the first item of each tuple.
        /// </summary>
        /// <typeparam name="T1">The type of the first item in the tuple.</typeparam>
        /// <typeparam name="T2">The type of the second item in the tuple.</typeparam>
        /// <param name="source">The source stream of tuples.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream containing the first item of each tuple.</returns>
        public static IProducer<T1> Item1<T1, T2>(this IProducer<(T1, T2)> source, DeliveryPolicy<(T1, T2)> deliveryPolicy = null, string name = nameof(Item1))
            => source.Select(t => t.Item1, deliveryPolicy, name);

        /// <summary>
        /// Decomposes a stream of tuples into a stream containing just the second item of each tuple.
        /// </summary>
        /// <typeparam name="T1">The type of the first item in the tuple.</typeparam>
        /// <typeparam name="T2">The type of the second item in the tuple.</typeparam>
        /// <param name="source">The source stream of tuples.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream containing the second item of each tuple.</returns>
        public static IProducer<T2> Item2<T1, T2>(this IProducer<(T1, T2)> source, DeliveryPolicy<(T1, T2)> deliveryPolicy = null, string name = nameof(Item2))
            => source.Select(t => t.Item2, deliveryPolicy, name);

        /// <summary>
        /// Flip takes a tuple of 2 elements and flips their order.
        /// </summary>
        /// <typeparam name="T1">Type of first element.</typeparam>
        /// <typeparam name="T2">Type of second element.</typeparam>
        /// <param name="source">Source to read tuples from.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>Returns a new producer with flipped tuples.</returns>
        public static IProducer<(T2, T1)> Flip<T1, T2>(this IProducer<(T1, T2)> source, DeliveryPolicy<(T1, T2)> deliveryPolicy = null, string name = nameof(Flip))
            => Process<(T1, T2), (T2, T1)>(
                source,
                (d, e, s) =>
                {
                    s.Post((d.Item2, d.Item1), e.OriginatingTime);
                },
                deliveryPolicy,
                name);

        /// <summary>
        /// Executes an action for each item in the input stream and then outputs the item. If the action modifies the item, the resulting stream reflects the change.
        /// </summary>
        /// <typeparam name="T">The input message type.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="action">The action to perform on every message in the source stream. The action has access to the message envelope.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of the same type as the source stream, containing one item for each input item, possibly modified by the action delegate.</returns>
        public static IProducer<T> Do<T>(this IProducer<T> source, Action<T, Envelope> action, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(Do))
            => Process<T, T>(
                source,
                (d, e, s) =>
                {
                    action(d, e);
                    s.Post(d, e.OriginatingTime);
                },
                deliveryPolicy,
                name);

        /// <summary>
        /// Executes an action for each item in the input stream and then outputs the item. If the action modifies the item, the resulting stream reflects the change.
        /// </summary>
        /// <typeparam name="T">The input message type.</typeparam>
        /// <param name="source">The source stream to subscribe to.</param>
        /// <param name="action">The action to perform on every message in the source stream. The action has access to the message envelope.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for this stream operator.</param>
        /// <returns>A stream of the same type as the source stream, containing one item for each input item, possibly modified by the action delegate.</returns>
        public static IProducer<T> Do<T>(this IProducer<T> source, Action<T> action, DeliveryPolicy<T> deliveryPolicy = null, string name = nameof(Do))
            => Do(source, (d, e) => action(d), deliveryPolicy, name);

        /// <summary>
        /// Edit messages in a stream; applying updates/inserts and deletes.
        /// </summary>
        /// <typeparam name="T">The input message type.</typeparam>
        /// <param name="source">The source stream to edit.</param>
        /// <param name="edits">A sequence of edits to be applied. Whether to update/insert or delete, an optional message to upsert and originating times.</param>
        /// <returns>A stream of the same type as the source stream with edits applied.</returns>
        internal static IProducer<T> EditStream<T>(this IProducer<T> source, IEnumerable<(bool upsert, T message, DateTime originatingTime)> edits)
        {
            var originalStream = source.Select(m => (original: true, upsert: true, m), DeliveryPolicy.Unlimited);
            var orderedEdits = edits.OrderBy(e => e.originatingTime).Select(e => ((original: false, e.upsert, e.message), e.originatingTime));
            var editsStream = Generators.Sequence(source.Out.Pipeline, orderedEdits);
            return originalStream
                .Zip(editsStream, DeliveryPolicy.Unlimited)
                .Process<(bool original, bool upsert, T message)[], T>(
                    ((bool original, bool upsert, T message)[] messages, Envelope envelope, Emitter<T> emitter) =>
                    {
                        var (original, upsert, message) = messages[0].original && messages.Length > 1 ? messages[1] : messages[0];
                        if (upsert)
                        {
                            emitter.Deliver(message, envelope);
                        }
                    },
                    DeliveryPolicy.Unlimited);
        }
    }
}