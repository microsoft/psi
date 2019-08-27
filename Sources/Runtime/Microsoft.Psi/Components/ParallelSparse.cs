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
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TOut">The result type.</typeparam>
    public class ParallelSparse<TIn, TKey, TOut> : Subpipeline, IConsumer<Dictionary<TKey, TIn>>, IProducer<Dictionary<TKey, TOut>>
    {
        private readonly Connector<Dictionary<TKey, TIn>> inConnector;
        private readonly Connector<Dictionary<TKey, TOut>> outConnector;
        private readonly Pipeline pipeline;
        private readonly Join<Dictionary<TKey, int>, TOut, TOut, Dictionary<TKey, TOut>> join;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparse{TIn, TKey, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="action">Action to perform in parallel.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        public ParallelSparse(Pipeline pipeline, Action<TKey, IProducer<TIn>> action, Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.inConnector = this.CreateInputConnectorFrom<Dictionary<TKey, TIn>>(pipeline, nameof(this.inConnector));
            var splitter = new ParallelSplitter(this, action, branchTerminationPolicy);
            this.inConnector.PipeTo(splitter);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparse{TIn, TKey, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="transform">Function mapping keyed input producers to output producers.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key, message payload (dictionary) and originating time.</param>
        public ParallelSparse(Pipeline pipeline, Func<TKey, IProducer<TIn>, IProducer<TOut>> transform, bool outputDefaultIfDropped = false, TOut defaultValue = default, Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy = null)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.inConnector = this.CreateInputConnectorFrom<Dictionary<TKey, TIn>>(pipeline, nameof(this.inConnector));

            var splitter = new ParallelSplitter(this, transform, branchTerminationPolicy, o => o.PipeTo(this.join.AddInput(), true));
            this.inConnector.PipeTo(splitter);
            var interpolator = outputDefaultIfDropped ? Reproducible.ExactOrDefault(defaultValue) : Reproducible.Exact<TOut>();
            this.join = Operators.Join(splitter.ActiveBranches, Enumerable.Empty<IProducer<TOut>>(), interpolator);
            this.outConnector = this.CreateOutputConnectorTo<Dictionary<TKey, TOut>>(pipeline, nameof(this.outConnector));
            this.join.PipeTo(this.outConnector);
        }

        /// <inheritdoc />
        public Receiver<Dictionary<TKey, TIn>> In => this.inConnector.In;

        /// <inheritdoc />
        public Emitter<Dictionary<TKey, TOut>> Out => this.outConnector.Out;

        /// <summary>
        /// Implements the splitter for the parallel operation.
        /// </summary>
        public class ParallelSplitter : IConsumer<Dictionary<TKey, TIn>>
        {
            private readonly Pipeline pipeline;
            private readonly Dictionary<TKey, Emitter<TIn>> branches = new Dictionary<TKey, Emitter<TIn>>();
            private readonly Dictionary<TKey, int> keyToBranchMapping = new Dictionary<TKey, int>();
            private readonly Func<TKey, IProducer<TIn>, IProducer<TOut>> parallelTransform;
            private readonly Action<TKey, IProducer<TIn>> parallelAction;
            private readonly Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy;
            private readonly Action<IProducer<TOut>> connectToJoin;
            private readonly Dictionary<TKey, int> activeBranches = new Dictionary<TKey, int>();
            private int branchKey = 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="ParallelSplitter"/> class.
            /// </summary>
            /// <param name="pipeline">The pipeline to add the component to.</param>
            /// <param name="transform">Function mapping keyed input producers to output producers.</param>
            /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key.</param>
            /// <param name="connectToJoin">Action that connects the results of a parallel branch back to join.</param>
            public ParallelSplitter(
                Pipeline pipeline,
                Func<TKey, IProducer<TIn>, IProducer<TOut>> transform,
                Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy,
                Action<IProducer<TOut>> connectToJoin)
            {
                this.pipeline = pipeline;
                this.parallelTransform = transform;
                this.branchTerminationPolicy = branchTerminationPolicy ?? BranchTerminationPolicy<TKey, TIn>.WhenKeyNotPresent();
                this.connectToJoin = connectToJoin;
                this.In = pipeline.CreateReceiver<Dictionary<TKey, TIn>>(this, this.Receive, nameof(this.In));
                this.ActiveBranches = pipeline.CreateEmitter<Dictionary<TKey, int>>(this, nameof(this.ActiveBranches));
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ParallelSplitter"/> class.
            /// </summary>
            /// <param name="pipeline">The pipeline to add the component to.</param>
            /// <param name="action">Action to perform in parallel.</param>
            /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key.</param>
            public ParallelSplitter(
                Pipeline pipeline,
                Action<TKey, IProducer<TIn>> action,
                Func<TKey, Dictionary<TKey, TIn>, DateTime, (bool, DateTime)> branchTerminationPolicy)
            {
                this.pipeline = pipeline;
                this.parallelAction = action;
                this.branchTerminationPolicy = branchTerminationPolicy ?? BranchTerminationPolicy<TKey, TIn>.WhenKeyNotPresent();
                this.In = pipeline.CreateReceiver<Dictionary<TKey, TIn>>(this, this.Receive, nameof(this.In));
            }

            /// <inheritdoc/>
            public Receiver<Dictionary<TKey, TIn>> In { get; }

            /// <summary>
            /// Gets the active branches emitter.
            /// </summary>
            public Emitter<Dictionary<TKey, int>> ActiveBranches { get; }

            private void Receive(Dictionary<TKey, TIn> message, Envelope e)
            {
                foreach (var pair in message)
                {
                    if (!this.branches.ContainsKey(pair.Key))
                    {
                        this.keyToBranchMapping[pair.Key] = this.branchKey++;
                        var subpipeline = Subpipeline.Create(this.pipeline, $"subpipeline{pair.Key}");
                        var connectorIn = subpipeline.CreateInputConnectorFrom<TIn>(this.pipeline, $"connectorIn{pair.Key}");
                        var branch = this.pipeline.CreateEmitter<TIn>(this, $"branch{pair.Key}-{Guid.NewGuid()}");
                        this.branches[pair.Key] = branch;
                        branch.PipeTo(connectorIn, true); // allows connections in running pipelines
                        connectorIn.In.Unsubscribed += time => subpipeline.Stop(time);
                        if (this.parallelTransform != null)
                        {
                            var branchResult = this.parallelTransform(pair.Key, connectorIn.Out);
                            var connectorOut = subpipeline.CreateOutputConnectorTo<TOut>(this.pipeline, $"connectorOut{pair.Key}");
                            branchResult.PipeTo(connectorOut.In, true);
                            connectorOut.In.Unsubscribed += closeOriginatingTime => connectorOut.Out.Close(closeOriginatingTime);
                            this.connectToJoin.Invoke(connectorOut.Out);
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

                this.ActiveBranches?.Post(this.activeBranches, e.OriginatingTime);
            }
        }
    }
}