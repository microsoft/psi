// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// Creates and applies a sub-pipeline to each element in the input array. The input array must have the same length across all messages.
    /// The sub-pipelines have index affinity, meaning the same sub-pipeline is re-used across multiple messages for the entry with the same index.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The result type.</typeparam>
    public class ParallelFixedLength<TIn, TOut> : IConsumer<TIn[]>, IProducer<TOut[]>
    {
        private readonly Emitter<TIn>[] branches;
        private readonly IProducer<TOut[]> join;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelFixedLength{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="vectorSize">Vector size.</param>
        /// <param name="action">Action to apply to output producers.</param>
        public ParallelFixedLength(Pipeline pipeline, int vectorSize, Action<int, IProducer<TIn>> action)
        {
            this.In = pipeline.CreateReceiver<TIn[]>(this, this.Receive, nameof(this.In));
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
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="vectorSize">Vector size.</param>
        /// <param name="transform">Function mapping keyed input producers to output producers.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        public ParallelFixedLength(Pipeline pipeline, int vectorSize, Func<int, IProducer<TIn>, IProducer<TOut>> transform, bool joinOrDefault)
        {
            this.In = pipeline.CreateReceiver<TIn[]>(this, this.Receive, nameof(this.In));
            this.branches = new Emitter<TIn>[vectorSize];
            var branchResults = new IProducer<TOut>[vectorSize];
            for (int i = 0; i < vectorSize; i++)
            {
                var subpipeline = Subpipeline.Create(pipeline, $"subpipeline{i}");
                var connectorIn = new Connector<TIn>(pipeline, subpipeline, $"connectorIn{i}");
                var connectorOut = new Connector<TOut>(subpipeline, pipeline, $"connectorOut{i}");
                this.branches[i] = pipeline.CreateEmitter<TIn>(this, $"branch{i}");
                this.branches[i].PipeTo(connectorIn);
                transform(i, connectorIn.Out).PipeTo(connectorOut.In);
                branchResults[i] = connectorOut;
            }

            var interpolator = joinOrDefault ? Match.ExactOrDefault<TOut>() : Match.Exact<TOut>();
            this.join = Operators.Join(branchResults, interpolator, pipeline: pipeline);
        }

        /// <inheritdoc />
        public Receiver<TIn[]> In { get; }

        /// <inheritdoc />
        public Emitter<TOut[]> Out => this.join.Out;

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