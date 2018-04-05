// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// A stateful transform.
    /// </summary>
    /// <typeparam name="TState">The state type</typeparam>
    /// <typeparam name="TIn">The input message type</typeparam>
    /// <typeparam name="TOut">The output message type</typeparam>
    public class Aggregator<TState, TIn, TOut> : ConsumerProducer<TIn, TOut>, IDisposable
    {
        private Func<TState, TIn, Envelope, Emitter<TOut>, TState> aggregator;
        private TState state;

        /// <summary>
        /// Initializes a new instance of the <see cref="Aggregator{TState, TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="init">Initial state.</param>
        /// <param name="aggregator">Aggregation function.</param>
        public Aggregator(Pipeline pipeline, TState init, Func<TState, TIn, Envelope, Emitter<TOut>, TState> aggregator)
            : base(pipeline)
        {
            this.state = init;
            this.aggregator = aggregator;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Serializer.Clear(ref this.state, new Serialization.SerializationContext());
        }

        /// <inheritdoc />
        protected override void Receive(TIn value, Envelope envelope)
        {
            var newState = this.aggregator(this.state, value, envelope, this.Out);
            newState.DeepClone(ref this.state);
        }
    }
}