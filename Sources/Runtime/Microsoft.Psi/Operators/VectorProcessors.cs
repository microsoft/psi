// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Extension methods that simplify operator usage.
    /// </summary>
    public static partial class Operators
    {
        #region Parallel extension methods using fixed-size parallel

        /// <summary>
        /// Transforms a stream of fixed-size array messages by creating a stream for each element in the array,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// array stream.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <typeparam name="TOut">Type of output array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="vectorSize">Vector arity.</param>
        /// <param name="streamTransform">Function mapping from an index and stream of input element to a stream of output element.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Func<int, IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy deliveryPolicy = null)
        {
            var p = new ParallelFixedLength<TIn, TOut>(source.Out.Pipeline, vectorSize, streamTransform, outputDefaultIfDropped, defaultValue);
            return PipeTo(source, p, deliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of fixed-size array messages by creating a stream for each element in the array,
        /// and performing an action on each of these streams.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="vectorSize">Vector arity.</param>
        /// <param name="streamAction">Action to apply to the individual element streams.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Action<int, IProducer<TIn>> streamAction,
            DeliveryPolicy deliveryPolicy = null)
        {
            var p = new ParallelFixedLength<TIn, TIn>(source.Out.Pipeline, vectorSize, streamAction);
            source.PipeTo(p, deliveryPolicy);
            return source;
        }

        /// <summary>
        /// Transforms a stream of fixed-size array messages by creating a stream for each element in the array,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// array stream.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <typeparam name="TOut">Type of output array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="vectorSize">Vector arity.</param>
        /// <param name="streamTransform">Function mapping from an input element stream to an output element stream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Func<IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy deliveryPolicy = null)
        {
            return source.Parallel(vectorSize, (i, s) => streamTransform(s), outputDefaultIfDropped, defaultValue, deliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of fixed-size array messages by creating a stream for each element in the array,
        /// and performing an action on each of these streams.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="vectorSize">Vector arity.</param>
        /// <param name="streamAction">Action to apply to the individual element streams.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Action<IProducer<TIn>> streamAction,
            DeliveryPolicy deliveryPolicy = null)
        {
            return source.Parallel(vectorSize, (i, s) => streamAction(s), deliveryPolicy);
        }

        #endregion

        #region Parallel extension methods using variable-size parallel

        /// <summary>
        /// Transforms a stream of variable-size array messages by creating a stream for each element in the array,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// array stream.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <typeparam name="TOut">Type of output array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamTransform">Function mapping from an input element stream to an output element stream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            Func<int, IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy deliveryPolicy = null)
        {
            var p = new ParallelVariableLength<TIn, TOut>(source.Out.Pipeline, streamTransform, outputDefaultIfDropped, defaultValue);
            return PipeTo(source, p, deliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of variable-size array messages by creating a stream for each element in the array,
        /// and performing an action on each of these streams.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamAction">Action to apply to the individual element streams.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            Action<int, IProducer<TIn>> streamAction,
            DeliveryPolicy deliveryPolicy = null)
        {
            var p = new ParallelVariableLength<TIn, TIn>(source.Out.Pipeline, streamAction);
            source.PipeTo(p, deliveryPolicy);
            return source;
        }

        /// <summary>
        /// Transforms a stream of variable-size array messages by creating a stream for each element in the array,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// array stream.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <typeparam name="TOut">Type of output array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamTransform">Function mapping from an input element stream to an output element stream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            Func<IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy deliveryPolicy = null)
        {
            return source.Parallel((i, s) => streamTransform(s), outputDefaultIfDropped, defaultValue, deliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of variable-size array messages by creating a stream for each element in the array,
        /// and performing an action on each of these streams.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamAction">Action to apply to the individual element streams.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            Action<IProducer<TIn>> streamAction,
            DeliveryPolicy deliveryPolicy = null)
        {
            return source.Parallel((i, s) => streamAction(s), deliveryPolicy);
        }

        #endregion

        #region Parallel extension methods using dictionaries

        /// <summary>
        /// Transforms a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// dictionary stream.
        /// </summary>
        /// <typeparam name="TIn">Type of input dictionary values.</typeparam>
        /// <typeparam name="TKey">Type of input dictionary keys.</typeparam>
        /// <typeparam name="TOut">Type of output dictionary values.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamTransform">Function mapping from an input element stream to an output element stream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TKey, TOut>> Parallel<TIn, TKey, TOut>(
            this IProducer<Dictionary<TKey, TIn>> source,
            Func<TKey, IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy deliveryPolicy = null,
            Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
        {
            var p = new ParallelSparse<TIn, TKey, TOut>(source.Out.Pipeline, streamTransform, outputDefaultIfDropped, defaultValue, branchTerminationPolicy);
            return PipeTo(source, p, deliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams.
        /// </summary>
        /// <typeparam name="TIn">Type of input dictionary values.</typeparam>
        /// <typeparam name="TKey">Type of input dictionary keys.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamAction">The action to apply to each element stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TKey, TIn>> Parallel<TIn, TKey>(
            this IProducer<Dictionary<TKey, TIn>> source,
            Action<TKey, IProducer<TIn>> streamAction,
            DeliveryPolicy deliveryPolicy = null,
            Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
        {
            var p = new ParallelSparse<TIn, TKey, TIn>(source.Out.Pipeline, streamAction, branchTerminationPolicy);
            source.PipeTo(p, deliveryPolicy);
            return source;
        }

        /// <summary>
        /// Transforms a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// dictionary stream.
        /// </summary>
        /// <typeparam name="TIn">Type of input dictionary values.</typeparam>
        /// <typeparam name="TKey">Type of input dictionary keys.</typeparam>
        /// <typeparam name="TOut">Type of output dictionary values.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamTransform">Function mapping from an input element stream to an output output element stream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to default.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, dictionary of values and the originating time of the last message containing the key.</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TKey, TOut>> Parallel<TIn, TKey, TOut>(
            this IProducer<Dictionary<TKey, TIn>> source,
            Func<IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy deliveryPolicy = null,
            Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
        {
            return source.Parallel((k, s) => streamTransform(s), outputDefaultIfDropped, defaultValue, deliveryPolicy, branchTerminationPolicy);
        }

        /// <summary>
        /// Processes a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams.
        /// </summary>
        /// <typeparam name="TIn">Type of input dictionary values.</typeparam>
        /// <typeparam name="TKey">Type of input dictionary keys.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamAction">The action to apply to each element stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TKey, TIn>> Parallel<TIn, TKey>(
            this IProducer<Dictionary<TKey, TIn>> source,
            Action<IProducer<TIn>> streamAction,
            DeliveryPolicy deliveryPolicy = null,
            Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
        {
            return source.Parallel((k, s) => streamAction(s), deliveryPolicy, branchTerminationPolicy);
        }

        #endregion
    }
}