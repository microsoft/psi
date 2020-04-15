// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Creates and executes parallel subpipelines based on an input stream and a splitter function.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TBranchKey">The branch key type.</typeparam>
    /// <typeparam name="TBranchIn">The branch input message type.</typeparam>
    /// <remarks>A splitter function is applied to each input message to generate a dictionary, and
    /// a subpipeline is created and executed for every key in the dictionary. A branch termination
    /// policy function governs when branches are terminated.</remarks>
    public class ParallelSparseDo<TIn, TBranchKey, TBranchIn> : Subpipeline, IConsumer<TIn>
    {
        private readonly Connector<TIn> inConnector;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparseDo{TIn, TBranchKey, TBranchIn}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="splitter">A function that generates a dictionary of key-value pairs for each given input message.</param>
        /// <param name="action">Action to perform in parallel.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        /// <param name="name">Name for this component (defaults to ParallelSparse).</param>
        /// <param name="defaultDeliveryPolicy">Pipeline-level default delivery policy to be used by this component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        public ParallelSparseDo(
            Pipeline pipeline,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Action<TBranchKey, IProducer<TBranchIn>> action,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null,
            string name = null,
            DeliveryPolicy defaultDeliveryPolicy = null)
            : base(pipeline, name ?? nameof(ParallelSparseDo<TIn, TBranchKey, TBranchIn>), defaultDeliveryPolicy)
        {
            this.inConnector = this.CreateInputConnectorFrom<TIn>(pipeline, nameof(this.inConnector));
            var parallelSparseSplitter = new ParallelSparseSplitter<TIn, TBranchKey, TBranchIn, TBranchIn>(this, splitter, action, branchTerminationPolicy);
            this.inConnector.PipeTo(parallelSparseSplitter);
        }

        /// <inheritdoc />
        public Receiver<TIn> In => this.inConnector.In;
    }
}