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
        private readonly Emitter<Dictionary<TKey, int>> activeBranchesEmitter;
        private readonly Func<TKey, IProducer<TIn>, IProducer<TOut>> parallelTransform;
        private readonly Action<TKey, IProducer<TIn>> parallelAction;
        private readonly Pipeline pipeline;
        private readonly Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy;

        // buffers
        private readonly Dictionary<TKey, int> activeBranches = new Dictionary<TKey, int>();

        private int branchKey = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparse{TIn, TKey, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="action">Action to perform in parallel.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        public ParallelSparse(Pipeline pipeline, Action<TKey, IProducer<TIn>> action, Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
        {
            this.pipeline = pipeline;
            this.parallelAction = action;
            this.branchTerminationPolicy = branchTerminationPolicy ?? BranchTerminationPolicy<TKey, TIn>.WhenKeyNotPresent();
            this.In = pipeline.CreateReceiver<Dictionary<TKey, TIn>>(this, this.Receive, nameof(this.In));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparse{TIn, TKey, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="transform">Function mapping keyed input producers to output producers.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        public ParallelSparse(Pipeline pipeline, Func<TKey, IProducer<TIn>, IProducer<TOut>> transform, bool joinOrDefault, Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
        {
            this.pipeline = pipeline;
            this.parallelTransform = transform;
            this.branchTerminationPolicy = branchTerminationPolicy ?? BranchTerminationPolicy<TKey, TIn>.WhenKeyNotPresent();
            this.In = pipeline.CreateReceiver<Dictionary<TKey, TIn>>(this, this.Receive, nameof(this.In));
            this.activeBranchesEmitter = pipeline.CreateEmitter<Dictionary<TKey, int>>(this, nameof(this.activeBranchesEmitter));
            var interpolator = joinOrDefault ? Match.ExactOrDefault<TOut>() : Match.Exact<TOut>();
            this.join = Operators.Join(this.activeBranchesEmitter, Enumerable.Empty<IProducer<TOut>>(), interpolator);
        }

        /// <inheritdoc />
        public Receiver<Dictionary<TKey, TIn>> In { get; }

        /// <inheritdoc />
        public Emitter<Dictionary<TKey, TOut>> Out => this.join.Out;

        private void Receive(Dictionary<TKey, TIn> message, Envelope e)
        {
            foreach (var pair in message)
            {
                if (!this.branches.ContainsKey(pair.Key))
                {
                    this.keyToBranchMapping[pair.Key] = this.branchKey++;
                    var subpipeline = Subpipeline.Create(this.pipeline, $"subpipeline{pair.Key}");
                    var connectorIn = new Connector<TIn>(this.pipeline, subpipeline, $"connectorIn{pair.Key}");
                    var branch = this.pipeline.CreateEmitter<TIn>(this, $"branch{pair.Key}-{Guid.NewGuid()}");
                    this.branches[pair.Key] = branch;
                    branch.PipeTo(connectorIn, true); // allows connections in running pipelines
                    connectorIn.In.Unsubscribed += subpipeline.Stop;
                    if (this.parallelTransform != null)
                    {
                        var branchResult = this.parallelTransform(pair.Key, connectorIn.Out);
                        var connectorOut = new Connector<TOut>(subpipeline, this.pipeline, $"connectorOut{pair.Key}");
                        branchResult.PipeTo(connectorOut.In, true);
                        connectorOut.In.Unsubscribed += closeOriginatingTime => connectorOut.Out.Close(closeOriginatingTime);
                        connectorOut.Out.PipeTo(this.join.AddInput(), true);
                    }
                    else
                    {
                        this.parallelAction(pair.Key, connectorIn.Out);
                    }

                    subpipeline.RunAsync(this.pipeline.ReplayDescriptor);
                }

                this.branches[pair.Key].Post(pair.Value, e.OriginatingTime);
            }

            foreach (var branch in this.branches.ToArray())
            {
                var (terminate, originatingTime) = this.branchTerminationPolicy(branch.Key, message, e.OriginatingTime);
                if (terminate)
                {
                    branch.Value.Close(originatingTime);
                    this.branches.Remove(branch.Key);
                }
            }

            this.activeBranches.Clear();
            foreach (var key in this.branches.Keys)
            {
                this.activeBranches.Add(key, this.keyToBranchMapping[key]);
            }

            this.activeBranchesEmitter?.Post(this.activeBranches, e.OriginatingTime);
        }
    }
}