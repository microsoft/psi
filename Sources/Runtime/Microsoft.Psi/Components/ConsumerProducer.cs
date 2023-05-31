// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// This is the base class for any component that transforms an input type into an output type.
    /// Derive from thsi class if your component has more than one input or more than one output.
    /// Otherwise, use one of the the <see cref="Operators.Select{TIn, TOut}(IProducer{TIn}, System.Func{TIn, Envelope, TOut}, DeliveryPolicy{TIn}, string)"/>
    /// or <see cref="Operators.Process{TIn, TOut}(IProducer{TIn}, System.Action{TIn, Envelope, Emitter{TOut}}, DeliveryPolicy{TIn}, string)"/> operators.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The output message type.</typeparam>
    public abstract class ConsumerProducer<TIn, TOut> : IConsumerProducer<TIn, TOut>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerProducer{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for this component.</param>
        public ConsumerProducer(Pipeline pipeline, string name = nameof(ConsumerProducer<TIn, TOut>))
        {
            this.name = name;
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.In = pipeline.CreateReceiver<TIn>(this, this.Receive, nameof(this.In));
        }

        /// <summary>
        /// Gets the input to receive messages on.
        /// </summary>
        public Receiver<TIn> In { get; }

        /// <summary>
        /// Gets the stream to write messages to.
        /// </summary>
        public Emitter<TOut> Out { get; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Override this method to process the incomming message and potentially publish one or more output messages.
        /// The input message payload is only valid for the duration of the call.
        /// If the data needs to be stored beyond the scope of this method,
        /// use the extension method <see cref="Serializer.DeepClone{T}(T, ref T)"/> to create a private copy.
        /// </summary>
        /// <param name="data">The input message payload.</param>
        /// <param name="envelope">The input message envelope.</param>
        protected virtual void Receive(TIn data, Envelope envelope)
        {
        }
    }
}