// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Creates and applies a sub-pipeline to each element in the input array. The input array can have variable length.
    /// The sub-pipelines have index affinity, meaning the same sub-pipeline is re-used across multiple messages for the entry with the same index in the array.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The result type.</typeparam>
    public class ParallelVariableLength<TIn, TOut> : Subpipeline, IConsumer<TIn[]>, IProducer<TOut[]>
    {
        private readonly Connector<TIn[]> inConnector;
        private readonly Connector<TOut[]> outConnector;
        private readonly Receiver<TIn[]> splitter;
        private readonly List<Emitter<TIn>> branches = new List<Emitter<TIn>>();
        private readonly Join<int, TOut, TOut[]> join;
        private readonly Emitter<int> activeBranchesEmitter;
        private readonly Func<int, IProducer<TIn>, IProducer<TOut>> parallelTransform;
        private readonly Action<int, IProducer<TIn>> parallelAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelVariableLength{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="action">Function mapping keyed input producers to output producers.</param>
        /// <param name="name">Name for this component (defaults to ParallelVariableLength).</param>
        /// <param name="defaultDeliveryPolicy">Pipeline-level default delivery policy to be used by this component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        public ParallelVariableLength(Pipeline pipeline, Action<int, IProducer<TIn>> action, string name = null, DeliveryPolicy defaultDeliveryPolicy = null)
            : base(pipeline, name ?? nameof(ParallelVariableLength<TIn, TOut>), defaultDeliveryPolicy)
        {
            this.parallelAction = action;
            this.inConnector = this.CreateInputConnectorFrom<TIn[]>(pipeline, nameof(this.inConnector));
            this.splitter = this.CreateReceiver<TIn[]>(this, this.Receive, nameof(this.splitter));
            this.inConnector.PipeTo(this.splitter);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelVariableLength{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="transform">Function mapping keyed input producers to output producers.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="name">Name for this component (defaults to ParallelVariableLength).</param>
        /// <param name="defaultDeliveryPolicy">Pipeline-level default delivery policy to be used by this component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        public ParallelVariableLength(Pipeline pipeline, Func<int, IProducer<TIn>, IProducer<TOut>> transform, bool outputDefaultIfDropped = false, TOut defaultValue = default, string name = null, DeliveryPolicy defaultDeliveryPolicy = null)
            : base(pipeline, name ?? nameof(ParallelVariableLength<TIn, TOut>), defaultDeliveryPolicy)
        {
            this.parallelTransform = transform;
            this.inConnector = this.CreateInputConnectorFrom<TIn[]>(pipeline, nameof(this.inConnector));
            this.splitter = this.CreateReceiver<TIn[]>(this, this.Receive, nameof(this.splitter));
            this.inConnector.PipeTo(this.splitter);
            this.activeBranchesEmitter = this.CreateEmitter<int>(this, nameof(this.activeBranchesEmitter));
            var interpolator = outputDefaultIfDropped ? Reproducible.ExactOrDefault<TOut>(defaultValue) : Reproducible.Exact<TOut>();

            this.join = new Join<int, TOut, TOut[]>(
                this,
                interpolator,
                (count, values) => values,
                0,
                count => Enumerable.Range(0, count));

            this.activeBranchesEmitter.PipeTo(this.join.InPrimary);
            this.outConnector = this.CreateOutputConnectorTo<TOut[]>(pipeline, nameof(this.outConnector));
            this.join.PipeTo(this.outConnector);
        }

        /// <inheritdoc />
        public Receiver<TIn[]> In => this.inConnector.In;

        /// <inheritdoc />
        public Emitter<TOut[]> Out => this.outConnector.Out;

        private void Receive(TIn[] message, Envelope e)
        {
            for (int i = 0; i < message.Length; i++)
            {
                if (this.branches.Count == i)
                {
                    var subpipeline = Subpipeline.Create(this, $"subpipeline{i}");
                    var branch = this.CreateEmitter<TIn>(this, $"branch{i}");
                    var connectorIn = new Connector<TIn>(this, subpipeline, $"connectorIn{i}");
                    branch.PipeTo(connectorIn, true); // allows connections in running pipelines

                    this.branches.Add(branch);

                    if (this.parallelTransform != null)
                    {
                        var branchResult = this.parallelTransform(i, connectorIn.Out);
                        var connectorOut = new Connector<TOut>(subpipeline, this, $"connectorOut{i}");
                        branchResult.PipeTo(connectorOut.In, true);
                        connectorOut.Out.PipeTo(this.join.AddInput(), true);
                    }
                    else
                    {
                        this.parallelAction(i, connectorIn.Out);
                    }

                    // run the subpipeline with a start time based on the message originating time
                    subpipeline.RunAsync(
                        e.OriginatingTime,
                        this.ReplayDescriptor.End,
                        this.ReplayDescriptor.EnforceReplayClock);
                }

                this.branches[i].Post(message[i], e.OriginatingTime);
            }

            this.activeBranchesEmitter?.Post(message.Length, e.OriginatingTime);
        }
    }
}