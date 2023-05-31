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
        /// <param name="name">Name for the parallel composite component (defaults to ParallelFixedLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Func<int, IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = null,
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelFixedLength<TIn, TOut>(source.Out.Pipeline, vectorSize, streamTransform, outputDefaultIfDropped, defaultValue, name, defaultParallelDeliveryPolicy);
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
        /// <param name="name">Name for the parallel composite component (defaults to ParallelFixedLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Action<int, IProducer<TIn>> streamAction,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = null,
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelFixedLength<TIn, TIn>(source.Out.Pipeline, vectorSize, streamAction, name, defaultParallelDeliveryPolicy);
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
        /// <param name="name">Name for the parallel composite component (defaults to ParallelFixedLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Func<IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = null,
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            return source.Parallel(vectorSize, (i, s) => streamTransform(s), outputDefaultIfDropped, defaultValue, deliveryPolicy, name, defaultParallelDeliveryPolicy);
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
        /// <param name="name">Name for the parallel composite component (defaults to ParallelFixedLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            int vectorSize,
            Action<IProducer<TIn>> streamAction,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = null,
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            return source.Parallel(vectorSize, (i, s) => streamAction(s), deliveryPolicy, name, defaultParallelDeliveryPolicy);
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
        /// <param name="name">Name for the parallel composite component (defaults to ParallelVariableLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            Func<int, IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelVariableLength<TIn, TOut>(source.Out.Pipeline, streamTransform, outputDefaultIfDropped, defaultValue, name, defaultParallelDeliveryPolicy);
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
        /// <param name="name">Name for the parallel composite component (defaults to ParallelVariableLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            Action<int, IProducer<TIn>> streamAction,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelVariableLength<TIn, TIn>(source.Out.Pipeline, streamAction, name, defaultParallelDeliveryPolicy);
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
        /// <param name="name">Name for the parallel composite component (defaults to ParallelVariableLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TOut[]> Parallel<TIn, TOut>(
            this IProducer<TIn[]> source,
            Func<IProducer<TIn>, IProducer<TOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TOut defaultValue = default,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = null,
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            return source.Parallel((i, s) => streamTransform(s), outputDefaultIfDropped, defaultValue, deliveryPolicy, name, defaultParallelDeliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of variable-size array messages by creating a stream for each element in the array,
        /// and performing an action on each of these streams.
        /// </summary>
        /// <typeparam name="TIn">Type of input array element.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamAction">Action to apply to the individual element streams.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelVariableLength).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output arrays.</returns>
        public static IProducer<TIn[]> Parallel<TIn>(
            this IProducer<TIn[]> source,
            Action<IProducer<TIn>> streamAction,
            DeliveryPolicy<TIn[]> deliveryPolicy = null,
            string name = null,
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            return source.Parallel((i, s) => streamAction(s), deliveryPolicy, name, defaultParallelDeliveryPolicy);
        }

        #endregion

        #region Parallel extension methods for sparse parallel

        /// <summary>
        /// Transforms a stream of messages by splitting it into a set of sub-streams (indexed by a branch key),
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding
        /// output stream.
        /// </summary>
        /// <typeparam name="TIn">The type of input messages.</typeparam>
        /// <typeparam name="TBranchKey">Type of the substream key.</typeparam>
        /// <typeparam name="TBranchIn">Type of the substream messages.</typeparam>
        /// <typeparam name="TBranchOut">Type of the subpipeline output for each substream.</typeparam>
        /// <typeparam name="TOut">The type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="splitter">A function that splits the input by generating a dictionary of key-value pairs for each given input message.</param>
        /// <param name="streamTransform">Stream transform to be applied to each substream.</param>
        /// <param name="outputCreator">A function that creates the output message based on a dictionary containing the branch outputs.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<TOut> Parallel<TIn, TBranchKey, TBranchIn, TBranchOut, TOut>(
            this IProducer<TIn> source,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Func<TBranchKey, IProducer<TBranchIn>, IProducer<TBranchOut>> streamTransform,
            Func<Dictionary<TBranchKey, TBranchOut>, TOut> outputCreator,
            bool outputDefaultIfDropped = false,
            TBranchOut defaultValue = default,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
         {
            var p = new ParallelSparseSelect<TIn, TBranchKey, TBranchIn, TBranchOut, TOut>(
                source.Out.Pipeline,
                splitter,
                streamTransform,
                outputCreator,
                outputDefaultIfDropped,
                defaultValue,
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            return PipeTo(source, p, deliveryPolicy);
        }

        /// <summary>
        /// Transforms a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// dictionary stream.
        /// </summary>
        /// <typeparam name="TBranchKey">Type of input dictionary keys.</typeparam>
        /// <typeparam name="TBranchIn">Type of input dictionary values.</typeparam>
        /// <typeparam name="TBranchOut">Type of output dictionary values.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamTransform">Function mapping from an input element stream to an output element stream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TBranchKey, TBranchOut>> Parallel<TBranchKey, TBranchIn, TBranchOut>(
            this IProducer<Dictionary<TBranchKey, TBranchIn>> source,
            Func<TBranchKey, IProducer<TBranchIn>, IProducer<TBranchOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TBranchOut defaultValue = default,
            DeliveryPolicy<Dictionary<TBranchKey, TBranchIn>> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseSelect<Dictionary<TBranchKey, TBranchIn>, TBranchKey, TBranchIn, TBranchOut, Dictionary<TBranchKey, TBranchOut>>(
                source.Out.Pipeline,
                _ => _,
                streamTransform,
                _ => _,
                outputDefaultIfDropped,
                defaultValue,
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            return source.PipeTo(p, deliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of messages by splitting it into a set of substreams (indexed by a key),
        /// applying a sub-pipeline to each of these streams.
        /// </summary>
        /// <typeparam name="TIn">The type of input messages.</typeparam>
        /// <typeparam name="TBranchKey">Type of the substream key.</typeparam>
        /// <typeparam name="TBranchIn">Type of the substream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="splitter">A function that splits the input by generating a dictionary of key-value pairs for each given input message.</param>
        /// <param name="streamAction">The action to apply to each element stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<TIn> Parallel<TIn, TBranchKey, TBranchIn>(
            this IProducer<TIn> source,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Action<TBranchKey, IProducer<TBranchIn>> streamAction,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseDo<TIn, TBranchKey, TBranchIn>(
                source.Out.Pipeline,
                splitter,
                streamAction,
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            source.PipeTo(p, deliveryPolicy);
            return source;
        }

        /// <summary>
        /// Processes a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams.
        /// </summary>
        /// <typeparam name="TBranchKey">Type of the substream key.</typeparam>
        /// <typeparam name="TBranchIn">Type of the substream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamAction">The action to apply to each element stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TBranchKey, TBranchIn>> Parallel<TBranchKey, TBranchIn>(
            this IProducer<Dictionary<TBranchKey, TBranchIn>> source,
            Action<TBranchKey, IProducer<TBranchIn>> streamAction,
            DeliveryPolicy<Dictionary<TBranchKey, TBranchIn>> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseDo<Dictionary<TBranchKey, TBranchIn>, TBranchKey, TBranchIn>(
                source.Out.Pipeline,
                _ => _,
                streamAction,
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            source.PipeTo(p, deliveryPolicy);
            return source;
        }

        /// <summary>
        /// Transforms a stream of messages by splitting it into a set of substreams (indexed by a key),
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding
        /// output stream.
        /// </summary>
        /// <typeparam name="TIn">The type of input messages.</typeparam>
        /// <typeparam name="TBranchKey">Type of the substream key.</typeparam>
        /// <typeparam name="TBranchIn">Type of the substream messages.</typeparam>
        /// <typeparam name="TBranchOut">Type of the subpipeline output for each substream.</typeparam>
        /// <typeparam name="TOut">The type of output messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="splitter">A function that splits the input by generating a dictionary of key-value pairs for each given input message.</param>
        /// <param name="streamTransform">Stream transform to be applied to each substream.</param>
        /// <param name="outputCreator">A function that creates the output message based on a dictionary containing the branch outputs.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to default.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, dictionary of values and the originating time of the last message containing the key.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<TOut> Parallel<TIn, TBranchKey, TBranchIn, TBranchOut, TOut>(
            this IProducer<TIn> source,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Func<IProducer<TBranchIn>, IProducer<TBranchOut>> streamTransform,
            Func<Dictionary<TBranchKey, TBranchOut>, TOut> outputCreator,
            bool outputDefaultIfDropped = false,
            TBranchOut defaultValue = default,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseSelect<TIn, TBranchKey, TBranchIn, TBranchOut, TOut>(
                source.Out.Pipeline,
                splitter,
                (k, s) => streamTransform(s),
                outputCreator,
                outputDefaultIfDropped,
                defaultValue,
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            return source.PipeTo(p, deliveryPolicy);
        }

        /// <summary>
        /// Transforms a stream of messages by splitting it into a set of substreams (indexed by a key),
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding
        /// output stream.
        /// </summary>
        /// <typeparam name="TIn">The type of input messages.</typeparam>
        /// <typeparam name="TBranchKey">Type of the substream key.</typeparam>
        /// <typeparam name="TBranchIn">Type of the substream messages.</typeparam>
        /// <typeparam name="TBranchOut">Type of the subpipeline output for each substream.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="splitter">A function that splits the input by generating a dictionary of key-value pairs for each given input message.</param>
        /// <param name="streamTransform">Stream transform to be applied to each substream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to default.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, dictionary of values and the originating time of the last message containing the key.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TBranchKey, TBranchOut>> Parallel<TIn, TBranchKey, TBranchIn, TBranchOut>(
            this IProducer<TIn> source,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Func<IProducer<TBranchIn>, IProducer<TBranchOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TBranchOut defaultValue = default,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseSelect<TIn, TBranchKey, TBranchIn, TBranchOut, Dictionary<TBranchKey, TBranchOut>>(
                source.Out.Pipeline,
                splitter,
                (k, s) => streamTransform(s),
                _ => _,
                outputDefaultIfDropped,
                defaultValue,
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            return source.PipeTo(p, deliveryPolicy);
        }

        /// <summary>
        /// Transforms a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding output
        /// dictionary stream.
        /// </summary>
        /// <typeparam name="TBranchKey">Type of input dictionary keys.</typeparam>
        /// <typeparam name="TBranchIn">Type of input dictionary values.</typeparam>
        /// <typeparam name="TBranchOut">Type of output dictionary values.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamTransform">Function mapping from an input element stream to an output element stream.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to default.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, dictionary of values and the originating time of the last message containing the key.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TBranchKey, TBranchOut>> Parallel<TBranchKey, TBranchIn, TBranchOut>(
            this IProducer<Dictionary<TBranchKey, TBranchIn>> source,
            Func<IProducer<TBranchIn>, IProducer<TBranchOut>> streamTransform,
            bool outputDefaultIfDropped = false,
            TBranchOut defaultValue = default,
            DeliveryPolicy<Dictionary<TBranchKey, TBranchIn>> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseSelect<Dictionary<TBranchKey, TBranchIn>, TBranchKey, TBranchIn, TBranchOut, Dictionary<TBranchKey, TBranchOut>>(
                source.Out.Pipeline,
                _ => _,
                (k, s) => streamTransform(s),
                _ => _,
                outputDefaultIfDropped,
                defaultValue,
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            return source.PipeTo(p, deliveryPolicy);
        }

        /// <summary>
        /// Processes a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams.
        /// </summary>
        /// <typeparam name="TIn">The type of input messages.</typeparam>
        /// <typeparam name="TBranchKey">Type of the substream key.</typeparam>
        /// <typeparam name="TBranchIn">Type of the substream messages.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="splitter">A function that splits the input by generating a dictionary of key-value pairs for each given input message.</param>
        /// <param name="streamAction">The action to apply to each element stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<TIn> Parallel<TIn, TBranchKey, TBranchIn>(
            this IProducer<TIn> source,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Action<IProducer<TBranchIn>> streamAction,
            DeliveryPolicy<TIn> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseDo<TIn, TBranchKey, TBranchIn>(
                source.Out.Pipeline,
                splitter,
                (k, s) => streamAction(s),
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);
            source.PipeTo(p);
            return source;
        }

        /// <summary>
        /// Processes a stream of dictionary messages by creating a stream for each key in the dictionary,
        /// applying a sub-pipeline to each of these streams.
        /// </summary>
        /// <typeparam name="TBranchKey">Type of input dictionary keys.</typeparam>
        /// <typeparam name="TBranchIn">Type of input dictionary values.</typeparam>
        /// <param name="source">Source stream.</param>
        /// <param name="streamAction">The action to apply to each element stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for the parallel composite component (defaults to ParallelSparse).</param>
        /// <param name="defaultParallelDeliveryPolicy">Pipeline-level default delivery policy to be used by the parallel composite component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        /// <returns>Stream of output dictionaries.</returns>
        public static IProducer<Dictionary<TBranchKey, TBranchIn>> Parallel<TBranchKey, TBranchIn>(
            this IProducer<Dictionary<TBranchKey, TBranchIn>> source,
            Action<IProducer<TBranchIn>> streamAction,
            DeliveryPolicy<Dictionary<TBranchKey, TBranchIn>> deliveryPolicy = null,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = nameof(Parallel),
            DeliveryPolicy defaultParallelDeliveryPolicy = null)
        {
            var p = new ParallelSparseDo<Dictionary<TBranchKey, TBranchIn>, TBranchKey, TBranchIn>(
                source.Out.Pipeline,
                _ => _,
                (k, s) => streamAction(s),
                branchTerminationPolicy,
                name,
                defaultParallelDeliveryPolicy);

            source.PipeTo(p, deliveryPolicy);
            return source;
        }

        #endregion
    }
}