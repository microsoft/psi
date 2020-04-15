// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Transforms a stream of messages by splitting it into a set of sub-streams (indexed by a branch key),
    /// applying a sub-pipeline to each of these streams, and assembling the results into a corresponding
    /// output stream.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TBranchKey">The key type.</typeparam>
    /// <typeparam name="TBranchIn">The branch input message type.</typeparam>
    /// <typeparam name="TBranchOut">The branch output message type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    /// <remarks>A splitter function is applied to each input message to generate a dictionary, and
    /// a subpipeline is created and executed for every new key in the dictionary. The results generated
    /// on individual branches are combined to create the output via an output creator function. A branch
    /// termination policy function governs when branches are terminated.</remarks>
    public class ParallelSparseSelect<TIn, TBranchKey, TBranchIn, TBranchOut, TOut> : Subpipeline, IConsumer<TIn>, IProducer<TOut>
    {
        private readonly Pipeline pipeline;
        private readonly Connector<TIn> inConnector;
        private readonly Connector<TOut> outConnector;
        private readonly Join<Dictionary<TBranchKey, int>, TBranchOut, TBranchOut, TOut> join;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparseSelect{TIn, TBranchKey, TBranchIn, TBranchOut, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="splitter">A function that splits the input by generating a dictionary of key-value pairs for each given input message.</param>
        /// <param name="transform">Function mapping keyed input producers to output producers.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="outputCreator">A function that creates the output message based on a dictionary containing the branch outputs.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for this component (defaults to ParallelSparse).</param>
        /// <param name="defaultDeliveryPolicy">Pipeline-level default delivery policy to be used by this component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        public ParallelSparseSelect(
            Pipeline pipeline,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Func<TBranchKey, IProducer<TBranchIn>, IProducer<TBranchOut>> transform,
            Func<Dictionary<TBranchKey, TBranchOut>, TOut> outputCreator,
            bool outputDefaultIfDropped = false,
            TBranchOut defaultValue = default,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = null,
            DeliveryPolicy defaultDeliveryPolicy = null)
            : base(pipeline, name ?? nameof(ParallelSparseSelect<TIn, TOut, TBranchIn, TBranchKey, TBranchOut>), defaultDeliveryPolicy)
        {
            this.pipeline = pipeline;
            this.inConnector = this.CreateInputConnectorFrom<TIn>(pipeline, nameof(this.inConnector));

            var parallelSparseSplitter = new ParallelSparseSplitter<TIn, TBranchKey, TBranchIn, TBranchOut>(this, splitter, transform, branchTerminationPolicy, o => o.PipeTo(this.join.AddInput(), true));
            this.inConnector.PipeTo(parallelSparseSplitter);
            var interpolator = outputDefaultIfDropped ? Reproducible.ExactOrDefault(defaultValue) : Reproducible.Exact<TBranchOut>();

            var buffer = new Dictionary<TBranchKey, TBranchOut>();
            this.join = Operators.Join(
                parallelSparseSplitter.ActiveBranches,
                Enumerable.Empty<IProducer<TBranchOut>>(),
                interpolator,
                (keys, values) =>
                {
                    buffer.Clear();
                    foreach (var keyPair in keys)
                    {
                        buffer[keyPair.Key] = values[keyPair.Value];
                    }

                    return outputCreator(buffer);
                });

            this.outConnector = this.CreateOutputConnectorTo<TOut>(pipeline, nameof(this.outConnector));
            this.join.PipeTo(this.outConnector);
        }

        /// <inheritdoc />
        public Receiver<TIn> In => this.inConnector.In;

        /// <inheritdoc />
        public Emitter<TOut> Out => this.outConnector.Out;
    }
}