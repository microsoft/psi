// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Creates and applies a sub-pipeline to each element in the input array. The input array must have the same length across all messages.
    /// The sub-pipelines have index affinity, meaning the same sub-pipeline is re-used across multiple messages for the entry with the same index.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The result type.</typeparam>
    public class ParallelFixedLength<TIn, TOut> : Subpipeline, IConsumer<TIn[]>, IProducer<TOut[]>
    {
        private readonly Connector<TIn[]> inConnector;
        private readonly Connector<TOut[]> outConnector;
        private readonly Receiver<TIn[]> splitter;
        private readonly Emitter<TIn>[] branches;
        private readonly IProducer<TOut[]> join;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelFixedLength{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="vectorSize">Vector size.</param>
        /// <param name="action">Action to apply to output producers.</param>
        /// <param name="name">Name for this component (defaults to ParallelFixedLength).</param>
        /// <param name="defaultDeliveryPolicy">Pipeline-level default delivery policy to be used by this component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        public ParallelFixedLength(Pipeline pipeline, int vectorSize, Action<int, IProducer<TIn>> action, string name = null, DeliveryPolicy defaultDeliveryPolicy = null)
            : base(pipeline, name ?? nameof(ParallelFixedLength<TIn, TOut>), defaultDeliveryPolicy)
        {
            this.inConnector = this.CreateInputConnectorFrom<TIn[]>(pipeline, nameof(this.inConnector));
            this.splitter = this.CreateReceiver<TIn[]>(this, this.Receive, nameof(this.splitter));
            this.inConnector.PipeTo(this.splitter);
            this.branches = new Emitter<TIn>[vectorSize];
            for (int i = 0; i < vectorSize; i++)
            {
                var subpipeline = Subpipeline.Create(pipeline, $"subpipeline{i}");
                var connector = new Connector<TIn>(pipeline, subpipeline, $"connector{i}");
                this.branches[i] = pipeline.CreateEmitter<TIn>(this, $"branch{i}");
                this.branches[i].PipeTo(connector);
                action(i, connector.Out);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelFixedLength{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="vectorSize">Vector size.</param>
        /// <param name="transform">Function mapping keyed input producers to output producers.</param>
        /// <param name="outputDefaultIfDropped">When true, a result is produced even if a message is dropped in processing one of the input elements. In this case the corresponding output element is set to a default value.</param>
        /// <param name="defaultValue">Default value to use when messages are dropped in processing one of the input elements.</param>
        /// <param name="name">Name for this component (defaults to ParallelFixedLength).</param>
        /// <param name="defaultDeliveryPolicy">Pipeline-level default delivery policy to be used by this component (defaults to <see cref="DeliveryPolicy.Unlimited"/> if unspecified).</param>
        public ParallelFixedLength(Pipeline pipeline, int vectorSize, Func<int, IProducer<TIn>, IProducer<TOut>> transform, bool outputDefaultIfDropped, TOut defaultValue = default, string name = null, DeliveryPolicy defaultDeliveryPolicy = null)
            : base(pipeline, name ?? nameof(ParallelFixedLength<TIn, TOut>), defaultDeliveryPolicy)
        {
            this.inConnector = this.CreateInputConnectorFrom<TIn[]>(pipeline, nameof(this.inConnector));
            this.splitter = this.CreateReceiver<TIn[]>(this, this.Receive, nameof(this.splitter));
            this.inConnector.PipeTo(this.splitter);
            this.branches = new Emitter<TIn>[vectorSize];
            var branchResults = new IProducer<TOut>[vectorSize];
            for (int i = 0; i < vectorSize; i++)
            {
                var subpipeline = Subpipeline.Create(this, $"subpipeline{i}");
                var connectorIn = new Connector<TIn>(this, subpipeline, $"connectorIn{i}");
                var connectorOut = new Connector<TOut>(subpipeline, this, $"connectorOut{i}");
                this.branches[i] = this.CreateEmitter<TIn>(this, $"branch{i}");
                this.branches[i].PipeTo(connectorIn);
                transform(i, connectorIn.Out).PipeTo(connectorOut.In);
                branchResults[i] = connectorOut;
            }

            var interpolator = outputDefaultIfDropped ? Reproducible.ExactOrDefault<TOut>(defaultValue) : Reproducible.Exact<TOut>();
            this.join = Operators.Join(branchResults, interpolator);
            this.outConnector = this.CreateOutputConnectorTo<TOut[]>(pipeline, nameof(this.outConnector));
            this.join.PipeTo(this.outConnector);
        }

        /// <inheritdoc />
        public Receiver<TIn[]> In => this.inConnector.In;

        /// <inheritdoc />
        public Emitter<TOut[]> Out => this.outConnector.Out;

        private void Receive(TIn[] message, Envelope e)
        {
            if (message.Length != this.branches.Length)
            {
                throw new InvalidOperationException("The Parallel operator has encountered a stream message that does not match the specified size of the input vector.");
            }

            for (int i = 0; i < message.Length; i++)
            {
                this.branches[i].Post(message[i], e.OriginatingTime);
            }
        }
    }
}