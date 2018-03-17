// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// A simple consumer
    /// </summary>
    /// <typeparam name="TIn">The input message type</typeparam>
    public abstract class SimpleConsumer<TIn> : IConsumer<TIn>
    {
        public SimpleConsumer(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<TIn>(this, this.Receive, nameof(this.In));
        }

        /// <inheritdoc />
        public Receiver<TIn> In { get; }

        public abstract void Receive(Message<TIn> message);
    }
}