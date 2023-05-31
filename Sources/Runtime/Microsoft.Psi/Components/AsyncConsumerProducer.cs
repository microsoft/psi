// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A simple transform component.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The output message type.</typeparam>
    public abstract class AsyncConsumerProducer<TIn, TOut> : IConsumerProducer<TIn, TOut>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncConsumerProducer{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        public AsyncConsumerProducer(Pipeline pipeline, string name = nameof(AsyncConsumerProducer<TIn, TOut>))
        {
            this.name = name;
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.In = pipeline.CreateAsyncReceiver<TIn>(this, this.ReceiveAsync, nameof(this.In));
        }

        /// <inheritdoc />
        public Receiver<TIn> In { get; }

        /// <inheritdoc />
        public Emitter<TOut> Out { get; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Async receiver to be implemented by subclass.
        /// </summary>
        /// <param name="value">Value received.</param>
        /// <param name="envelope">Message envelope.</param>
        /// <returns>Async task.</returns>
        protected virtual async Task ReceiveAsync(TIn value, Envelope envelope)
        {
            await Task.Run(() => throw new NotImplementedException());
        }
    }
}