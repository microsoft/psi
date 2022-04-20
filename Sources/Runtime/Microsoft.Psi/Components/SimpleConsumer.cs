// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// A simple consumer.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    public abstract class SimpleConsumer<TIn> : IConsumer<TIn>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleConsumer{TIn}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for this component.</param>
        public SimpleConsumer(Pipeline pipeline, string name = nameof(SimpleConsumer<TIn>))
        {
            this.name = name;
            this.In = pipeline.CreateReceiver<TIn>(this, this.Receive, nameof(this.In));
        }

        /// <inheritdoc />
        public Receiver<TIn> In { get; }

        /// <summary>
        /// Message receiver.
        /// </summary>
        /// <param name="message">Message received.</param>
        public abstract void Receive(Message<TIn> message);

        /// <inheritdoc/>
        public override string ToString() => this.name;
    }
}