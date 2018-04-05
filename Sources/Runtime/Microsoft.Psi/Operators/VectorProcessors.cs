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
        /// Create and applies a sub-pipeline to each element in the input collection.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="vectorSize">Vector arity.</param>
        /// <param name="action">Action function.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Stream of collections of output.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Func<int, TIn, Envelope, TOut> action,
            bool joinOrDefault = false,
            DeliveryPolicy policy = null)
        {
            return source.Parallel(vectorSize, (index, singleItemStream) => singleItemStream.Select((item, e) => action(index, item, e)), joinOrDefault, policy);
        }

        /// <summary>
        /// Create and applies a sub-pipeline to each element in the input collection.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="vectorSize">Vector arity.</param>
        /// <param name="transformSelector">Function mapping indexes to output producers.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Stream of collections of output.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Func<int, IProducer<TIn>, IProducer<TOut>> transformSelector,
            bool joinOrDefault = false,
            DeliveryPolicy policy = null)
        {
            policy = policy ?? DeliveryPolicy.Immediate;
            var p = new Parallel<TIn, TOut>(source.Out.Pipeline, vectorSize, transformSelector, joinOrDefault);
            source.PipeTo(p, policy);
            return p;
        }

        /// <summary>
        /// Create and applies a sub-pipeline to each element in the input collection.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="action">Action function.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Stream of key to output mappings.</returns>
        public static IProducer<Dictionary<TKey, TOut>> Parallel<TIn, TKey, TOut>(
            this IProducer<Dictionary<TKey, TIn>> source,
            Func<TKey, TIn, Envelope, TOut> action,
            bool joinOrDefault = false,
            DeliveryPolicy policy = null)
        {
            return source.Parallel<TIn, TKey, TOut>((key, stream) => stream.Select((val, e) => action(key, val, e)), joinOrDefault, policy);
        }

        /// <summary>
        /// Create and applies a sub-pipeline to each element in the input collection.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="transformSelector">Function mapping indexes to output producers.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Stream of key to output mappings.</returns>
        public static IProducer<Dictionary<TKey, TOut>> Parallel<TIn, TKey, TOut>(
            this IProducer<Dictionary<TKey, TIn>> source,
            Func<TKey, IProducer<TIn>, IProducer<TOut>> transformSelector,
            bool joinOrDefault = false,
            DeliveryPolicy policy = null)
        {
            policy = policy ?? DeliveryPolicy.Immediate;
            var p = new ParallelSparse<TIn, TKey, TOut>(source.Out.Pipeline, transformSelector, joinOrDefault);
            source.PipeTo(p, policy);
            return p;
        }

        /// <summary>
        /// Create and applies a sub-pipeline to each element in the input collection.
        /// </summary>
        /// <typeparam name="TIn">Type of input messages.</typeparam>
        /// <typeparam name="TKey">Type of key.</typeparam>
        /// <typeparam name="TOut">Type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="transformSelector">Function mapping indexes to output producers.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        /// <param name="policy">Delivery policy.</param>
        /// <returns>Stream of key to output mappings.</returns>
        public static IProducer<Dictionary<TKey, TOut>> Parallel<TIn, TKey, TOut>(
            this IProducer<Dictionary<TKey, TIn>> source,
            Func<IProducer<TIn>, IProducer<TOut>> transformSelector,
            bool joinOrDefault = false,
            DeliveryPolicy policy = null)
        {
            policy = policy ?? DeliveryPolicy.Immediate;
            var p = new ParallelSparse<TIn, TKey, TOut>(source.Out.Pipeline, (k, v) => transformSelector(v), joinOrDefault);
            source.PipeTo(p, policy);
            return p;
        }
    }
}