// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Creates and applies a sub-pipeline to each element in the input collection.
    /// The sub-pipelines have index affinity, meaning the same sub-pipeline is re-used across multiple messages for the entry with the same index.
    /// </summary>
    /// <typeparam name="TIn">The input message type</typeparam>
    /// <typeparam name="TOut">The result type</typeparam>
    public class Parallel<TIn, TOut> : IConsumer<TIn[]>, IProducer<TOut[]>
    {
        private readonly Emitter<TIn>[] branches;
        private readonly IProducer<TOut[]> join;
        private readonly Receiver<TIn[]> input;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parallel{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="vectorSize">Vector size.</param>
        /// <param name="transformSelector">Function mapping keyed input producers to output producers.</param>
        /// <param name="joinOrDefault">Whether to do an "...OrDefault" join.</param>
        public Parallel(Pipeline pipeline, int vectorSize, Func<int, IProducer<TIn>, IProducer<TOut>> transformSelector, bool joinOrDefault)
        {
            this.input = pipeline.CreateReceiver<TIn[]>(this, this.Receive, nameof(this.In));
            this.branches = new Emitter<TIn>[vectorSize];
            var branchResults = new IProducer<TOut>[vectorSize];
            for (int i = 0; i < vectorSize; i++)
            {
                this.branches[i] = pipeline.CreateEmitter<TIn>(this, "branch" + i);
                branchResults[i] = transformSelector(i, this.branches[i]);
            }

            var interpolator = joinOrDefault ? Match.ExactOrDefault<TOut>() : Match.Exact<TOut>();
            this.join = Operators.Join(branchResults, interpolator);
        }

        /// <inheritdoc />
        public Receiver<TIn[]> In => this.input;

        /// <inheritdoc />
        public Emitter<TOut[]> Out => this.join.Out;

        private void Receive(TIn[] message, Envelope e)
        {
            for (int i = 0; i < message.Length; i++)
            {
                this.branches[i].Post(message[i], e.OriginatingTime);
            }
        }
    }
}