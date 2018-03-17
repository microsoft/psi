// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Deserializer optimized for streaming scenarios, where buffers and instances can be cached.
    /// </summary>
    /// <typeparam name="T">The type of messages to serialize</typeparam>
    public sealed class DeserializerComponent<T> : ConsumerProducer<Message<BufferReader>, T>
    {
        private readonly SerializationContext context;
        private readonly KnownSerializers serializers;
        private readonly SerializationHandler<T> handler;
        private T reusableInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeserializerComponent{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to attach to.</param>
        /// <param name="serializers">A set of known serializers, or KnownSerializers.Default</param>
        /// <param name="reusableInstance">An instance of type T to use as a deserialization buffer, or null / default(T) to let the component allocate one</param>
        public DeserializerComponent(Pipeline pipeline, KnownSerializers serializers, T reusableInstance)
            : base(pipeline)
        {
            this.serializers = serializers;
            this.context = new SerializationContext(this.serializers);
            this.handler = this.serializers.GetHandler<T>();
            this.reusableInstance = reusableInstance;
        }

        /// <summary>
        /// Deserializes and instance from the specified reader.
        /// </summary>
        /// <param name="msg">The byte data to deserialize</param>
        /// <param name="envelope">The envelope of the message.</param>
        protected override void Receive(Message<BufferReader> msg, Envelope envelope)
        {
            var message = msg;

            // don't read unless there are active subscribers
            this.handler.Deserialize(message.Data, ref this.reusableInstance, this.context);
            this.context.Reset();
            this.Out.Deliver(this.reusableInstance, message.Envelope);
        }
    }
}
