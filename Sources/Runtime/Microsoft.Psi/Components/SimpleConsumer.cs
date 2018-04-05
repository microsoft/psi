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
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConsumer{TIn}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        public SimpleConsumer(Pipeline pipeline)
        {
            this.In = pipeline.CreateReceiver<TIn>(this, this.Receive, nameof(this.In));
        }

        /// <inheritdoc />
        public Receiver<TIn> In { get; }

        /// <summary>
        /// Message receiver.
        /// </summary>
        /// <param name="message">Message received.</param>
        public abstract void Receive(Message<TIn> message);
    }
}