// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements the splitter for the <see cref="ParallelSparseDo{TIn, TBranchKey, TBranchIn}"/>
    /// and <see cref="ParallelSparseSelect{TIn, TBranchKey, TBranchIn, TBranchOut, TOut}"/> components.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TBranchKey">The key type.</typeparam>
    /// <typeparam name="TBranchIn">The branch input message type.</typeparam>
    /// <typeparam name="TBranchOut">The branch output message type.</typeparam>
    public class ParallelSparseSplitter<TIn, TBranchKey, TBranchIn, TBranchOut> : IConsumer<TIn>
    {
        private readonly Pipeline pipeline;
        private readonly Dictionary<TBranchKey, Emitter<TBranchIn>> branches = new Dictionary<TBranchKey, Emitter<TBranchIn>>();
        private readonly Dictionary<TBranchKey, int> keyToBranchMapping = new Dictionary<TBranchKey, int>();
        private readonly Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitterFunction;
        private readonly Func<TBranchKey, IProducer<TBranchIn>, IProducer<TBranchOut>> parallelTransform;
        private readonly Action<TBranchKey, IProducer<TBranchIn>> parallelAction;
        private readonly Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy;
        private readonly Action<IProducer<TBranchOut>> connectToJoin;
        private readonly Dictionary<TBranchKey, int> activeBranches;
        private int branchKey = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparseSplitter{TIn, TBranchKey, TBranchIn, TBranchOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="splitter">A function that splits the input by generating a dictionary of key-value pairs for each given input message.</param>
        /// <param name="transform">Function mapping keyed input producers to output producers.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key.</param>
        /// <param name="connectToJoin">Action that connects the results of a parallel branch back to join.</param>
        public ParallelSparseSplitter(
            Pipeline pipeline,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Func<TBranchKey, IProducer<TBranchIn>, IProducer<TBranchOut>> transform,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy,
            Action<IProducer<TBranchOut>> connectToJoin)
        {
            this.pipeline = pipeline;
            this.splitterFunction = splitter;
            this.parallelTransform = transform;
            this.branchTerminationPolicy = branchTerminationPolicy ?? BranchTerminationPolicy<TBranchKey, TBranchIn>.WhenKeyNotPresent();
            this.connectToJoin = connectToJoin;
            this.activeBranches = new Dictionary<TBranchKey, int>();
            this.In = pipeline.CreateReceiver<TIn>(this, this.Receive, nameof(this.In));
            this.ActiveBranches = pipeline.CreateEmitter<Dictionary<TBranchKey, int>>(this, nameof(this.ActiveBranches));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelSparseSplitter{TIn, TBranchKey, TBranchIn, TBranchOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="splitter">A function that generates a dictionary of key-value pairs for each given input message.</param>
        /// <param name="action">Action to perform in parallel.</param>
        /// <param name="branchTerminationPolicy">Predicate function determining whether and when (originating time) to terminate branches (defaults to when key no longer present), given the current key.</param>
        public ParallelSparseSplitter(
            Pipeline pipeline,
            Func<TIn, Dictionary<TBranchKey, TBranchIn>> splitter,
            Action<TBranchKey, IProducer<TBranchIn>> action,
            Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> branchTerminationPolicy)
        {
            this.pipeline = pipeline;
            this.splitterFunction = splitter;
            this.parallelAction = action;
            this.branchTerminationPolicy = branchTerminationPolicy ?? BranchTerminationPolicy<TBranchKey, TBranchIn>.WhenKeyNotPresent();
            this.In = pipeline.CreateReceiver<TIn>(this, this.Receive, nameof(this.In));
        }

        /// <inheritdoc/>
        public Receiver<TIn> In { get; }

        /// <summary>
        /// Gets the active branches emitter.
        /// </summary>
        public Emitter<Dictionary<TBranchKey, int>> ActiveBranches { get; }

        private void Receive(TIn input, Envelope e)
        {
            var keyedValues = this.splitterFunction(input);
            foreach (var pair in keyedValues)
            {
                if (!this.branches.ContainsKey(pair.Key))
                {
                    this.keyToBranchMapping[pair.Key] = this.branchKey++;
                    var subpipeline = Subpipeline.Create(this.pipeline, $"subpipeline{pair.Key}");
                    var connectorIn = subpipeline.CreateInputConnectorFrom<TBranchIn>(this.pipeline, $"connectorIn{pair.Key}");
                    var branch = this.pipeline.CreateEmitter<TBranchIn>(this, $"branch{pair.Key}-{Guid.NewGuid()}");
                    this.branches[pair.Key] = branch;
                    branch.PipeTo(connectorIn, true); // allows connections in running pipelines
                    connectorIn.In.Unsubscribed += time => subpipeline.Stop(time);
                    if (this.parallelTransform != null)
                    {
                        var branchResult = this.parallelTransform(pair.Key, connectorIn.Out);
                        var connectorOut = subpipeline.CreateOutputConnectorTo<TBranchOut>(this.pipeline, $"connectorOut{pair.Key}");
                        branchResult.PipeTo(connectorOut.In, true);
                        connectorOut.In.Unsubscribed += closeOriginatingTime => connectorOut.Out.Close(closeOriginatingTime);
                        this.connectToJoin.Invoke(connectorOut.Out);
                    }
                    else
                    {
                        this.parallelAction(pair.Key, connectorIn.Out);
                    }

                    // run the subpipeline with a start time based on the message originating time
                    subpipeline.RunAsync(
                        e.OriginatingTime,
                        this.pipeline.ReplayDescriptor.End,
                        this.pipeline.ReplayDescriptor.EnforceReplayClock);
                }

                this.branches[pair.Key].Post(pair.Value, e.OriginatingTime);
            }

            foreach (var branch in this.branches.ToArray())
            {
                var (terminate, originatingTime) = this.branchTerminationPolicy(branch.Key, keyedValues, e.OriginatingTime);
                if (terminate)
                {
                    branch.Value.Close(originatingTime);
                    this.branches.Remove(branch.Key);
                }
            }

            if (this.ActiveBranches != null)
            {
                this.activeBranches.Clear();
                foreach (var key in this.branches.Keys)
                {
                    this.activeBranches.Add(key, this.keyToBranchMapping[key]);
                }

                this.ActiveBranches.Post(this.activeBranches, e.OriginatingTime);
            }
        }
    }
}
