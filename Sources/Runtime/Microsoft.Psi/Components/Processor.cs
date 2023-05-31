// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// Component that wraps a transform delegate which processes input messages and optionally publishes results.
    /// There is no assumption regarding the number of input messages needed to produce a result, or the number of output messages
    /// resulting from one input message.
    /// If the transform is stateful, special care needs to be taken when storing input message data.
    /// The input message payload is only valid for the duration of the transform call.
    /// If the data needs to be stored beyond the scope of this method,
    /// use the extension method <see cref="Serializer.DeepClone{T}(T, ref T)"/> to create a private copy.
    /// </summary>
    /// <typeparam name="TIn">The input message type.</typeparam>
    /// <typeparam name="TOut">The result type.</typeparam>
    public class Processor<TIn, TOut> : ConsumerProducer<TIn, TOut>
    {
        private readonly Action<TIn, Envelope, Emitter<TOut>> transform;

        /// <summary>
        /// Initializes a new instance of the <see cref="Processor{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="transform">A delegate that processes the input data and potentially publishes a result on the provided <see cref="Emitter{T}"/>.</param>
        /// <param name="onClose">An optional action to execute when the input stream closes.</param>
        /// <param name="name">An optional name for this component.</param>
        public Processor(Pipeline pipeline, Action<TIn, Envelope, Emitter<TOut>> transform, Action<DateTime, Emitter<TOut>> onClose = null, string name = nameof(Processor<TIn, TOut>))
            : base(pipeline, name)
        {
            this.transform = transform;
            if (onClose != null)
            {
                this.In.Unsubscribed += closingTime => onClose(closingTime, this.Out);
            }
        }

        /// <summary>
        /// Override this method to process the incoming message and potentially publish one or more output messages.
        /// The input message payload is only valid for the duration of the call.
        /// If the data needs to be stored beyond the scope of this method,
        /// use the extension method <see cref="Serializer.DeepClone{T}(T, ref T)"/> to create a private copy.
        /// </summary>
        /// <param name="data">The input message payload.</param>
        /// <param name="envelope">The input message envelope.</param>
        protected override void Receive(TIn data, Envelope envelope)
        {
            this.transform(data, envelope, this.Out);
        }
    }
}