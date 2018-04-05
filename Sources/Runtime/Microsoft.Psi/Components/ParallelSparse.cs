// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Creates and applies a sub-pipeline to each element in the input collection.
    /// The sub-pipelines have key affinity, meaning the same sub-pipeline is re-used across multiple messages for the entry with the same key.
    /// </summary>
    /// <typeparam name="TIn">The input message type</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TOut">The result type</typeparam>
    public class ParallelSparse<TIn, TKey, TOut> : IConsumer<Dictionary<TKey, TIn>>, IProducer<Dictionary<TKey, TOut>>
    {
        private readonly Dictionary<TKey, Emitter<TIn>> branches = new Dictionary<TKey, Emitter<TIn>>();
        private readonly Dictionary<TKey, int> keyToBranchMapping = new Dictionary<TKey, int>();
        private readonly Join<Dictionary<TKey, int>, TOut, Dictionary<TKey, TOut>> join;
        private readonly Receiver<Dictionary<TKey, TIn>> input;
        private readonly Emitter<Dictionary<TKey, int>> activeBranchesEmitter;
        private readonly Func<TKey, IProducer<TIn>, IProducer<TOut>> transformSelector;
        private readonly Pipeline pipeline;

        // buffers
        private Dictionary<TKey, int> activeBranches = new Dictionary<TKey, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparse{TIn, TKey, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="transformSelector">Function mapping keyed input producers to output producers.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        public ParallelSparse(Pipeline pipeline, Func<TKey, IProducer<TIn>, IProducer<TOut>> transformSelector, bool joinOrDefault)
        {
            this.pipeline = pipeline;
            this.transformSelector = transformSelector;
            this.input = pipeline.CreateReceiver<Dictionary<TKey, TIn>>(this, this.Receive, nameof(this.In));
            this.activeBranchesEmitter = pipeline.CreateEmitter<Dictionary<TKey, int>>(this, nameof(this.activeBranchesEmitter));
            var interpolator = joinOrDefault ?
                Match.BestOrDefault<TOut>(new RelativeTimeInterval(-default(TimeSpan), default(TimeSpan))) :
                Match.Exact<TOut>();
            this.join = Operators.Join(this.activeBranchesEmitter, Enumerable.Empty<IProducer<TOut>>(), interpolator);
        }

        /// <inheritdoc />
        public Receiver<Dictionary<TKey, TIn>> In => this.input;

        /// <inheritdoc />
        public Emitter<Dictionary<TKey, TOut>> Out => this.join.Out;

        private void Receive(Dictionary<TKey, TIn> message, Envelope e)
        {
            this.activeBranches.Clear();
            foreach (var pair in message)
            {
                if (!this.branches.ContainsKey(pair.Key))
                {
                    this.keyToBranchMapping[pair.Key] = this.branches.Count;
                    var branch = this.branches[pair.Key] = this.pipeline.CreateEmitter<TIn>(this, "branch" + pair.Key);
                    var branchResult = this.transformSelector(pair.Key, branch);
                    branchResult.PipeTo(this.join.AddInput());
                }

                this.branches[pair.Key].Post(pair.Value, e.OriginatingTime);
                this.activeBranches[pair.Key] = this.keyToBranchMapping[pair.Key];
            }

            this.activeBranchesEmitter.Post(this.activeBranches, e.OriginatingTime);
        }
    }
}