// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Serializer optimized for streaming scenarios, where buffers and instances can be cached.
    /// </summary>
    /// <typeparam name="T">The type of messages to serialize</typeparam>
    public sealed class SerializerComponent<T> : ConsumerProducer<T, Message<BufferReader>>
    {
        private readonly SerializationContext context;
        private readonly BufferWriter serializationBuffer = new BufferWriter(16);
        private readonly SerializationHandler<T> handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerComponent{T}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="serializers">Known serializers.</param>
        public SerializerComponent(Pipeline pipeline, KnownSerializers serializers)
            : base(pipeline)
        {
            this.context = new SerializationContext(serializers);
            this.handler = serializers.GetHandler<T>();
        }

        /// <inheritdoc />
        protected override void Receive(T data, Envelope e)
        {
            this.serializationBuffer.Reset();
            this.handler.Serialize(this.serializationBuffer, data, this.context);
            this.context.Reset();
            var outputBuffer = new BufferReader(this.serializationBuffer);

            // preserve the envelope we received
            var resultMsg = Message.Create(outputBuffer, e);
            this.Out.Post(resultMsg, e.OriginatingTime);
        }
    }
}
