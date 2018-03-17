// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A simple transform component.
    /// </summary>
    /// <typeparam name="TIn">The input message type</typeparam>
    /// <typeparam name="TOut">The output message type</typeparam>
    public abstract class AsyncConsumerProducer<TIn, TOut> : IConsumerProducer<TIn, TOut>
    {
        public AsyncConsumerProducer(Pipeline pipeline)
        {
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.In = pipeline.CreateAsyncReceiver<TIn>(this, this.ReceiveAsync, nameof(this.In));
        }

        /// <inheritdoc />
        public Receiver<TIn> In { get; }

        /// <inheritdoc />
        public Emitter<TOut> Out { get; }

        protected virtual async Task ReceiveAsync(TIn value, Envelope envelope)
        {
            await Task.Run(() => throw new NotImplementedException());
        }
    }
}