// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// A stateful transform.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The output message type.</typeparam>
    public class Aggregator<TState, TIn, TOut> : ConsumerProducer<TIn, TOut>, IDisposable
    {
        private readonly Func<TState, TIn, Envelope, Emitter<TOut>, TState> aggregator;
        private TState state;

        /// <summary>
        /// Initializes a new instance of the <see cref="Aggregator{TState, TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="initialState">Initial state.</param>
        /// <param name="aggregator">Aggregation function.</param>
        /// <param name="name">An optional name for this component.</param>
        public Aggregator(Pipeline pipeline, TState initialState, Func<TState, TIn, Envelope, Emitter<TOut>, TState> aggregator, string name = nameof(Aggregator<TState, TIn, TOut>))
            : base(pipeline, name)
        {
            this.state = initialState;
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